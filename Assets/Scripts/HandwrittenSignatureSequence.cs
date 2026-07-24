using System.Collections;
using UnityEngine;

public sealed class HandwrittenSignatureSequence : MonoBehaviour
{
    [System.Serializable]
    public sealed class SignatureStep
    {
        [SerializeField] Renderer targetRenderer;
        [SerializeField] AudioClip sound;
        [SerializeField, Range(0f, 1f)] float soundVolume = 1f;
        [SerializeField, Min(0f)] float delayBefore;
        [SerializeField, Min(0.05f)] float duration = 1.4f;
        [SerializeField] AnimationCurve revealCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public Renderer TargetRenderer => targetRenderer;
        public AudioClip Sound => sound;
        public float SoundVolume => soundVolume;
        public float DelayBefore => delayBefore;
        public float Duration => duration;
        public AnimationCurve RevealCurve => revealCurve;
    }

    [SerializeField] SignatureStep[] signatures;
    [SerializeField] AudioSource audioSource;
    [SerializeField] bool playOnEnable = true;
    [SerializeField] bool useUnscaledTime;
    [SerializeField, Min(0f)] float initialDelay = 0.8f;
    [SerializeField] bool duckBgmDuringPlayback = true;
    [SerializeField, Range(0f, 1f)] float duckedBgmVolume = 0.25f;

    static readonly int RevealProperty = Shader.PropertyToID("_Reveal");
    MaterialPropertyBlock propertyBlock;
    Coroutine playback;
    float previousBgmVolume;
    bool bgmDucked;

    void Awake()
    {
        EnsurePropertyBlock();
        ResetSignatures();
    }

    void OnEnable()
    {
        if (playOnEnable)
            Replay();
    }

    void OnDisable()
    {
        if (playback != null)
        {
            StopCoroutine(playback);
            playback = null;
        }

        if (audioSource != null)
            audioSource.Stop();

        RestoreBgmVolume();
    }

    [ContextMenu("Replay Signatures")]
    public void Replay()
    {
        if (!isActiveAndEnabled)
            return;

        if (playback != null)
            StopCoroutine(playback);

        ResetSignatures();
        playback = StartCoroutine(PlaySequence());
    }

    [ContextMenu("Reset Signatures")]
    public void ResetSignatures()
    {
        if (signatures == null)
            return;

        for (int i = 0; i < signatures.Length; i++)
            SetStepReveal(i, 0f);
    }

    public void SetStepReveal(int index, float reveal)
    {
        if (signatures == null || index < 0 || index >= signatures.Length)
            return;

        Renderer target = signatures[index]?.TargetRenderer;
        if (target == null)
            return;

        EnsurePropertyBlock();
        target.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(RevealProperty, Mathf.Clamp01(reveal));
        target.SetPropertyBlock(propertyBlock);
    }

    void EnsurePropertyBlock()
    {
        propertyBlock ??= new MaterialPropertyBlock();
    }

    IEnumerator PlaySequence()
    {
        if (initialDelay > 0f)
            yield return Wait(initialDelay);

        DuckBgm();

        if (signatures != null)
        {
            for (int i = 0; i < signatures.Length; i++)
            {
                SignatureStep step = signatures[i];
                if (step == null || step.TargetRenderer == null)
                    continue;

                if (step.DelayBefore > 0f)
                    yield return Wait(step.DelayBefore);

                if (audioSource != null && step.Sound != null)
                    audioSource.PlayOneShot(step.Sound, step.SoundVolume);

                float elapsed = 0f;
                while (elapsed < step.Duration)
                {
                    elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    float normalizedTime = Mathf.Clamp01(elapsed / step.Duration);
                    SetStepReveal(i, step.RevealCurve.Evaluate(normalizedTime));
                    yield return null;
                }

                SetStepReveal(i, 1f);
            }
        }

        RestoreBgmVolume();
        playback = null;
    }

    void DuckBgm()
    {
        if (!duckBgmDuringPlayback || bgmDucked || AudioManager.Instance == null)
            return;

        previousBgmVolume = AudioManager.Instance.BgmVolume;
        AudioManager.Instance.SetBgmVolume(Mathf.Min(previousBgmVolume, duckedBgmVolume));
        bgmDucked = true;
    }

    void RestoreBgmVolume()
    {
        if (!bgmDucked)
            return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.SetBgmVolume(previousBgmVolume);
        bgmDucked = false;
    }

    IEnumerator Wait(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
    }
}
