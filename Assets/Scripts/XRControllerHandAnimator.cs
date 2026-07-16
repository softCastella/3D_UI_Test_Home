using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

[DisallowMultipleComponent]
public sealed class XRControllerHandAnimator : MonoBehaviour
{
    [Header("Hand Source")]
    [SerializeField] XRNode controllerNode = XRNode.LeftHand;
    [SerializeField] GameObject handModelPrefab;
    [SerializeField] GameObject controllerVisual;

    [Header("Authored Hand Alignment")]
    [SerializeField] Vector3 localPosition;
    [SerializeField] Vector3 localEulerAngles;
    [SerializeField] Vector3 localScale = Vector3.one;

    [Header("Finger Motion")]
    [SerializeField, Range(0f, 120f)] float proximalCurl = 65f;
    [SerializeField, Range(0f, 120f)] float intermediateCurl = 80f;
    [SerializeField, Range(0f, 120f)] float distalCurl = 55f;
    [SerializeField, Range(-1f, 1f)] float curlDirection = -1f;
    [SerializeField, Min(0.01f)] float smoothing = 14f;

    readonly List<FingerBone> triggerBones = new();
    readonly List<FingerBone> gripBones = new();
    readonly List<FingerBone> thumbBones = new();

    InputDevice controller;
    float smoothedTrigger;
    float smoothedGrip;

    void Awake()
    {
        if (controllerVisual != null)
            controllerVisual.SetActive(false);

        if (handModelPrefab == null)
            return;

        var hand = Instantiate(handModelPrefab, transform);
        hand.name = controllerNode == XRNode.LeftHand ? "Left Controller Hand Visual" : "Right Controller Hand Visual";
        hand.transform.SetLocalPositionAndRotation(localPosition, Quaternion.Euler(localEulerAngles));
        hand.transform.localScale = localScale;

        CacheFinger(hand.transform, "Index", triggerBones);
        CacheFinger(hand.transform, "Middle", gripBones);
        CacheFinger(hand.transform, "Ring", gripBones);
        CacheFinger(hand.transform, "Little", gripBones);
        CacheFinger(hand.transform, "Thumb", thumbBones);
    }

    void Update()
    {
        if (!controller.isValid)
            controller = InputDevices.GetDeviceAtXRNode(controllerNode);

        var trigger = ReadAxis(CommonUsages.trigger);
        var grip = ReadAxis(CommonUsages.grip);
        var blend = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        smoothedTrigger = Mathf.Lerp(smoothedTrigger, trigger, blend);
        smoothedGrip = Mathf.Lerp(smoothedGrip, grip, blend);
    }

    void LateUpdate()
    {
        ApplyCurl(triggerBones, smoothedTrigger);
        ApplyCurl(gripBones, smoothedGrip);
        ApplyCurl(thumbBones, smoothedGrip * 0.55f);
    }

    float ReadAxis(InputFeatureUsage<float> usage)
    {
        return controller.TryGetFeatureValue(usage, out var value) ? Mathf.Clamp01(value) : 0f;
    }

    void CacheFinger(Transform root, string fingerName, List<FingerBone> destination)
    {
        var palm = FindChild(root, "Palm");
        CacheBone(root, palm, fingerName + "Metacarpal", destination, proximalCurl * 0.35f);
        CacheBone(root, palm, fingerName + "Proximal", destination, proximalCurl);
        CacheBone(root, palm, fingerName + "Intermediate", destination, intermediateCurl);
        CacheBone(root, palm, fingerName + "Distal", destination, distalCurl);
    }

    void CacheBone(Transform root, Transform palm, string boneName, List<FingerBone> destination, float angle)
    {
        var bone = FindChild(root, boneName);
        if (bone == null || bone.childCount == 0 || palm == null)
            return;

        var segmentDirection = (bone.GetChild(0).position - bone.position).normalized;
        var towardPalm = Vector3.ProjectOnPlane(palm.position - bone.position, segmentDirection).normalized;
        var bendAxisWorld = Vector3.Cross(segmentDirection, towardPalm).normalized;
        if (bendAxisWorld.sqrMagnitude < 0.5f)
            return;

        var bendAxisLocal = bone.InverseTransformDirection(bendAxisWorld).normalized;
        destination.Add(new FingerBone(bone, bone.localRotation, bendAxisLocal, angle));
    }

    void ApplyCurl(List<FingerBone> bones, float amount)
    {
        foreach (var bone in bones)
            bone.Transform.localRotation = bone.AuthoredRotation * Quaternion.AngleAxis(bone.Angle * curlDirection * amount, bone.BendAxis);
    }

    static Transform FindChild(Transform root, string objectName)
    {
        if (root.name == objectName)
            return root;

        foreach (Transform child in root)
        {
            var match = FindChild(child, objectName);
            if (match != null)
                return match;
        }

        return null;
    }

    readonly struct FingerBone
    {
        public readonly Transform Transform;
        public readonly Quaternion AuthoredRotation;
        public readonly Vector3 BendAxis;
        public readonly float Angle;

        public FingerBone(Transform transform, Quaternion authoredRotation, Vector3 bendAxis, float angle)
        {
            Transform = transform;
            AuthoredRotation = authoredRotation;
            BendAxis = bendAxis;
            Angle = angle;
        }
    }
}
