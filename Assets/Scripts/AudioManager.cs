using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class AudioManager : MonoBehaviour
{
    private const string SettingsResourcePath = "Audio/AudioManagerSettings";
    private const string SceneSettingsResourcePath = "Audio/Scenes/";

    public static AudioManager Instance { get; private set; }
    public float BgmVolume => bgmSource != null ? bgmSource.volume : 0f;

    private AudioManagerSettings settings;
    private AudioSource bgmSource;
    private AudioSource ambienceSource;
    private AudioSource sfxSource;
    private Coroutine sceneAudioRoutine;
    private Coroutine bgmFadeRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        AudioManager manager = Instance != null
            ? Instance
            : FindAnyObjectByType<AudioManager>();
        if (manager == null)
            manager = new GameObject("AudioManager").AddComponent<AudioManager>();
        if (Instance == null)
        {
            Instance = manager;
            DontDestroyOnLoad(manager.gameObject);
        }

        manager.QueueSceneAudio(SceneManager.GetActiveScene());
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        settings = Resources.Load<AudioManagerSettings>(SettingsResourcePath);
        CreateSources();
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (settings == null)
        {
            Debug.LogError($"Audio settings not found at Resources/{SettingsResourcePath}.", this);
            return;
        }

        if (settings.playBgmOnStartup && !string.IsNullOrWhiteSpace(settings.startupBgmId))
            PlayBgm(settings.startupBgmId);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        QueueSceneAudio(scene);
    }

    private void QueueSceneAudio(Scene scene)
    {
        SceneAudioSettings sceneSettings = Resources.Load<SceneAudioSettings>(
            SceneSettingsResourcePath + scene.name);
        if (sceneSettings == null)
            return;

        if (sceneAudioRoutine != null)
            StopCoroutine(sceneAudioRoutine);
        sceneAudioRoutine = StartCoroutine(PlaySceneAudioWhenReady(sceneSettings));
    }

    private IEnumerator PlaySceneAudioWhenReady(SceneAudioSettings sceneSettings)
    {
        // Let the XR camera and the first rendered frames become ready before audio starts.
        yield return null;
        yield return new WaitForEndOfFrame();
        if (sceneSettings.startDelay > 0f)
            yield return new WaitForSecondsRealtime(sceneSettings.startDelay);

        yield return LoadClip(sceneSettings.bgm);
        yield return LoadClip(sceneSettings.ambience);

        if (sceneSettings.bgm != null)
            PlaySceneBgm(sceneSettings.bgm, sceneSettings.bgmVolume,
                sceneSettings.bgmFadeInDuration);
        else if (sceneSettings.stopPreviousBgmWhenEmpty)
            StopBgm();

        if (sceneSettings.ambience != null)
            PlayLooping(sceneSettings.ambience, sceneSettings.ambienceVolume, ambienceSource);
        else if (sceneSettings.stopPreviousAmbienceWhenEmpty)
            StopAmbience();

        sceneAudioRoutine = null;
    }

    private static IEnumerator LoadClip(AudioClip clip)
    {
        if (clip == null || clip.loadState == AudioDataLoadState.Loaded)
            yield break;

        clip.LoadAudioData();
        while (clip.loadState == AudioDataLoadState.Loading)
            yield return null;

        if (clip.loadState == AudioDataLoadState.Failed)
            Debug.LogError($"Failed to load audio clip '{clip.name}'.");
    }

    public void PlayBgm(string id)
    {
        PlayLooping(Find(settings == null ? null : settings.bgm, id), bgmSource);
    }

    public void StopBgm()
    {
        if (bgmFadeRoutine != null)
        {
            StopCoroutine(bgmFadeRoutine);
            bgmFadeRoutine = null;
        }
        bgmSource.Stop();
    }

    public void FadeOutBgm(float duration)
    {
        if (bgmFadeRoutine != null)
            StopCoroutine(bgmFadeRoutine);
        bgmFadeRoutine = StartCoroutine(FadeOutBgmRoutine(duration));
    }

    private void PlaySceneBgm(AudioClip clip, float volume, float fadeInDuration)
    {
        if (bgmFadeRoutine != null)
            StopCoroutine(bgmFadeRoutine);

        float targetVolume = Mathf.Clamp01(volume);
        PlayLooping(clip, targetVolume, bgmSource);
        if (fadeInDuration <= 0f)
        {
            bgmFadeRoutine = null;
            return;
        }

        bgmSource.volume = 0f;
        bgmFadeRoutine = StartCoroutine(FadeInBgmRoutine(targetVolume, fadeInDuration));
    }

    public void PlayAmbience(string id)
    {
        PlayLooping(Find(settings == null ? null : settings.ambience, id), ambienceSource);
    }

    public void StopAmbience()
    {
        ambienceSource.Stop();
    }

    public void PlaySfx(string id)
    {
        AudioManagerSettings.Sound sound = Find(settings == null ? null : settings.sfx, id);
        if (sound?.clip != null)
            sfxSource.PlayOneShot(sound.clip, sound.volume);
    }

    public void SetBgmVolume(float volume)
    {
        if (bgmFadeRoutine != null)
        {
            StopCoroutine(bgmFadeRoutine);
            bgmFadeRoutine = null;
        }

        bgmSource.volume = Mathf.Clamp01(volume);
    }

    public void SetAmbienceVolume(float volume)
    {
        ambienceSource.volume = Mathf.Clamp01(volume);
    }

    public void SetSfxVolume(float volume)
    {
        sfxSource.volume = Mathf.Clamp01(volume);
    }

    private void CreateSources()
    {
        bgmSource = CreateSource("BGM", true);
        ambienceSource = CreateSource("Ambience", true);
        sfxSource = CreateSource("SFX", false);

        if (settings == null)
            return;

        bgmSource.outputAudioMixerGroup = settings.bgmOutput;
        ambienceSource.outputAudioMixerGroup = settings.ambienceOutput;
        sfxSource.outputAudioMixerGroup = settings.sfxOutput;
    }

    private AudioSource CreateSource(string sourceName, bool loop)
    {
        GameObject child = new(sourceName);
        child.transform.SetParent(transform, false);
        AudioSource source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        return source;
    }

    private static void PlayLooping(AudioManagerSettings.Sound sound, AudioSource source)
    {
        if (sound?.clip == null)
            return;

        PlayLooping(sound.clip, sound.volume, source);
    }

    private static void PlayLooping(AudioClip clip, float volume, AudioSource source)
    {
        if (clip == null)
            return;

        if (source.isPlaying && source.clip == clip)
            return;

        source.clip = clip;
        source.volume = Mathf.Clamp01(volume);
        source.Play();
    }

    private IEnumerator FadeOutBgmRoutine(float duration)
    {
        if (!bgmSource.isPlaying || duration <= 0f)
        {
            bgmSource.Stop();
            bgmFadeRoutine = null;
            yield break;
        }

        float startVolume = bgmSource.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.volume = startVolume;
        bgmFadeRoutine = null;
    }

    private IEnumerator FadeInBgmRoutine(float targetVolume, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(0f, targetVolume,
                Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        bgmSource.volume = targetVolume;
        bgmFadeRoutine = null;
    }

    private static AudioManagerSettings.Sound Find(
        List<AudioManagerSettings.Sound> sounds, string id)
    {
        if (sounds == null || string.IsNullOrWhiteSpace(id))
            return null;

        foreach (AudioManagerSettings.Sound sound in sounds)
        {
            if (sound != null && sound.id == id)
                return sound;
        }

        Debug.LogWarning($"Audio id '{id}' is not registered in AudioManagerSettings.");
        return null;
    }
}
