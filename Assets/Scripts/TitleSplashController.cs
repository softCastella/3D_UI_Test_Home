using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class TitleSplashController : MonoBehaviour
{
    private const float MaximumFadeDeltaPerFrame = 1f / 30f;

    [SerializeField] private GameObject primaryLogo;
    [SerializeField] private GameObject partnerLogos;

    [Header("Primary Logo (logo2d)")]
    [SerializeField, Min(1)] private int prewarmFrames = 3;
    [SerializeField, Min(0f)] private float initialDelay = 1f;
    [SerializeField, Min(0f)] private float primaryFadeInDuration = 0.6f;
    [SerializeField, Min(0f)] private float partnerLogoDelay = 0.3f;

    [Header("Partner Logos (Logos)")]
    [SerializeField, Min(0f)] private float partnerFadeInDuration = 0.6f;

    [Header("Scene Transition")]
    [SerializeField, Min(0f)] private float visibleDuration = 1.5f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.6f;
    [SerializeField] private string nextSceneName = "2_Intro";

    private CanvasGroup primaryGroup;
    private CanvasGroup partnerGroup;

    private void Awake()
    {
        primaryGroup = GetOrAddCanvasGroup(primaryLogo);
        partnerGroup = GetOrAddCanvasGroup(partnerLogos);

        SetAlpha(primaryGroup, 0f);
        SetAlpha(partnerGroup, 0f);
    }

    private IEnumerator Start()
    {
        Canvas.ForceUpdateCanvases();
        for (int frame = 0; frame < prewarmFrames; frame++)
            yield return new WaitForEndOfFrame();

        yield return Wait(initialDelay);
        yield return Fade(primaryGroup, 0f, 1f, primaryFadeInDuration);
        yield return Wait(partnerLogoDelay);

        yield return Fade(partnerGroup, 0f, 1f, partnerFadeInDuration);
        yield return Wait(visibleDuration);
        yield return FadeOutLogos();

        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError($"TitleSplashController: Build Settings에 '{nextSceneName}' 씬이 없습니다.", this);
            yield break;
        }

        yield return SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Single);
    }

    private IEnumerator FadeOutLogos()
    {
        float elapsed = 0f;
        if (fadeOutDuration <= 0f)
        {
            SetAlpha(primaryGroup, 0f);
            SetAlpha(partnerGroup, 0f);
            yield break;
        }

        while (elapsed < fadeOutDuration)
        {
            elapsed += Mathf.Min(Time.unscaledDeltaTime, MaximumFadeDeltaPerFrame);
            float progress = Mathf.Clamp01(elapsed / fadeOutDuration);
            float alpha = 1f - Mathf.SmoothStep(0f, 1f, progress);
            SetAlpha(primaryGroup, alpha);
            SetAlpha(partnerGroup, alpha);
            yield return null;
        }

        SetAlpha(primaryGroup, 0f);
        SetAlpha(partnerGroup, 0f);
    }

    private static CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        if (target == null)
            return null;

        CanvasGroup group = target.GetComponent<CanvasGroup>();
        return group != null ? group : target.AddComponent<CanvasGroup>();
    }

    private static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null)
            yield break;

        group.alpha = from;
        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            // XR 초기화나 셰이더 준비로 첫 프레임이 길어져도 페이드가
            // 한 프레임 만에 끝나지 않도록 진행량을 제한한다.
            elapsed += Mathf.Min(Time.unscaledDeltaTime, MaximumFadeDeltaPerFrame);
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            group.alpha = Mathf.Lerp(from, to, easedProgress);
            yield return null;
        }

        group.alpha = to;
    }

    private static IEnumerator Wait(float duration)
    {
        if (duration > 0f)
            yield return new WaitForSecondsRealtime(duration);
    }

    private static void SetAlpha(CanvasGroup group, float alpha)
    {
        if (group != null)
            group.alpha = alpha;
    }
}
