using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Drives a controller-held hand model between its open and gripped poses from an analog controller
/// axis. The action is built in code rather than referenced from an input asset so the component
/// works as soon as it is dropped onto a hand, with nothing to wire up in the inspector.
/// </summary>
[RequireComponent(typeof(Animator))]
public class HandGripAnimator : MonoBehaviour
{
    public enum HandSide
    {
        Left,
        Right,
    }

    /// <summary>Float parameter of the controller built by HandAnimatorBuilder.</summary>
    const string k_Parameter = "Grip";

    /// <summary>
    /// Seconds to reach the target value. The raw axis jitters around its extremes, which reads as a
    /// twitching hand, so the value is always eased rather than left to the inspector.
    /// </summary>
    const float k_SmoothTime = 0.05f;

    [SerializeField] HandSide m_Side = HandSide.Left;

    Animator m_Animator;
    InputAction m_Action;
    int m_ParameterHash;
    float m_Value;
    float m_Velocity;

    void Awake()
    {
        m_Animator = GetComponent<Animator>();
        m_ParameterHash = Animator.StringToHash(k_Parameter);
    }

    void OnEnable()
    {
        var usage = m_Side == HandSide.Left ? "LeftHand" : "RightHand";

        m_Action = new InputAction(
            name: $"HandGrip_{usage}",
            type: InputActionType.Value,
            binding: $"<XRController>{{{usage}}}/grip",
            expectedControlType: "Axis");

        m_Action.Enable();
    }

    void OnDisable()
    {
        m_Action?.Disable();
        m_Action?.Dispose();
        m_Action = null;
    }

    void Update()
    {
        if (m_Action == null)
            return;

        var target = m_Action.ReadValue<float>();
        m_Value = Mathf.SmoothDamp(m_Value, target, ref m_Velocity, k_SmoothTime);

        m_Animator.SetFloat(m_ParameterHash, m_Value);
    }
}
