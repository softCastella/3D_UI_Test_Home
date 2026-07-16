using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class XRLocationTeleportTarget : UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable
{
    [Header("Teleport Destination")]
    [SerializeField] XROrigin xrOrigin;
    [SerializeField] Transform destination;
    [SerializeField, Min(0f)] float floorOffset = 0.03f;
    [SerializeField] bool preserveCurrentHeight = true;
    [SerializeField] float arrivalForwardOffset = 0.65f;
    [SerializeField] bool alignViewToDestination = true;

    protected override void Awake()
    {
        base.Awake();

        if (destination == null)
            destination = transform;
        if (xrOrigin == null)
            xrOrigin = FindFirstObjectByType<XROrigin>();
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        Teleport();
    }

    public void Teleport()
    {
        if (xrOrigin == null || xrOrigin.Camera == null || destination == null)
            return;

        var originTransform = xrOrigin.transform;
        var up = originTransform.up;
        var cameraPosition = xrOrigin.Camera.transform.position;
        var cameraHeight = Vector3.Dot(cameraPosition - originTransform.position, up);
        var currentFeetPosition = cameraPosition - up * cameraHeight;
        var destinationForward = Vector3.ProjectOnPlane(destination.forward, up).normalized;
        var desiredFeetPosition = destination.position + up * floorOffset;
        if (destinationForward.sqrMagnitude > 0.5f)
            desiredFeetPosition += destinationForward * arrivalForwardOffset;

        var movement = desiredFeetPosition - currentFeetPosition;

        if (preserveCurrentHeight)
            movement = Vector3.ProjectOnPlane(movement, up);

        originTransform.position += movement;

        if (!alignViewToDestination || destinationForward.sqrMagnitude <= 0.5f)
            return;

        var currentViewForward = Vector3.ProjectOnPlane(xrOrigin.Camera.transform.forward, up).normalized;
        if (currentViewForward.sqrMagnitude <= 0.5f)
            return;

        var yaw = Vector3.SignedAngle(currentViewForward, destinationForward, up);
        originTransform.RotateAround(xrOrigin.Camera.transform.position, up, yaw);
    }
}
