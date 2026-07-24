using System.Collections;
using UnityEngine;

public sealed class HandwrittenSignatureSequence : MonoBehaviour
{
    [System.Serializable]
    public sealed class SignatureStep
    {
        [SerializeField] Renderer targetRenderer;
        [SerializeField, Min(0f)] float delayBefore;
        [SerializeField, Min(0.05f)] float duration = 1.4f;
        [SerializeField] AnimationCurve revealCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public Renderer TargetRenderer => targetRenderer;
        public float DelayBefore => delayBefore;
        public float Duration => duration;
        public AnimationCurve RevealCurve => revealCurve;
    }

    [SerializeField] SignatureStep[] signatures;
    [SerializeField] bool playOnEnable = true;
    [SerializeField] bool useUnscaledTime;
    [SerializeField, Min(0f)] float initialDelay = 0.8f;

    static readonly int RevealProperty = Shader.PropertyToID("_Reveal");
    readonly MaterialPropertyBlock propertyBlock = new();
    Coroutine playback;

    void Awake()
    {
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

        target.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(RevealProperty, Mathf.Clamp01(reveal));
        target.SetPropertyBlock(propertyBlock);
    }

    IEnumerator PlaySequence()
    {
        if (initialDelay > 0f)
            yield return Wait(initialDelay);

        if (signatures != null)
        {
            for (int i = 0; i < signatures.Length; i++)
            {
                SignatureStep step = signatures[i];
                if (step == null || step.TargetRenderer == null)
                    continue;

                if (step.DelayBefore > 0f)
                    yield return Wait(step.DelayBefore);

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

        playback = null;
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
