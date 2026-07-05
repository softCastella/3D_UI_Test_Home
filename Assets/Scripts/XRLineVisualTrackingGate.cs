using UnityEngine;
using UnityEngine.XR;

[DisallowMultipleComponent]
[RequireComponent(typeof(LineRenderer))]
public sealed class XRLineVisualTrackingGate : MonoBehaviour
{
    [SerializeField] private XRNode xrNode;
    [SerializeField] private LineRenderer lineRenderer;

    private void LateUpdate()
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(xrNode);
        bool isTracked = device.isValid
            && device.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked)
            && tracked;

        if (!isTracked && lineRenderer != null)
            lineRenderer.enabled = false;
    }
}
