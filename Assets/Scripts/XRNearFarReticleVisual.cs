using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

[DisallowMultipleComponent]
public sealed class XRNearFarReticleVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] NearFarInteractor nearFarInteractor;
    [SerializeField] CurveVisualController curveVisualController;
    [SerializeField] GameObject reticlePrefab;

    [Header("Ray Appearance")]
    [SerializeField] bool overrideRayAppearance = true;
    [SerializeField] Material rayMaterial;
    [SerializeField] Gradient rayGradient;
    [SerializeField, Min(0.1f)] float rayDistance = 40f;
    [SerializeField] bool extendRayToEmptyHit = true;

    [Header("Reticle Behavior")]
    [SerializeField] bool showOnEmptyGeometry = true;
    [SerializeField] bool showWhileSelecting;
    [SerializeField] bool scaleWithDistance = true;
    [SerializeField, Min(0.01f)] float minimumDistanceScale = 0.5f;
    [SerializeField, Min(0.01f)] float maximumDistanceScale = 4f;
    [SerializeField, Min(0f)] float surfaceOffset = 0.003f;

    GameObject reticleInstance;
    Vector3 authoredReticleScale;
    CurveVisualController appearanceTarget;

    void Awake()
    {
        ResolveActiveInteractor();
        CreateReticleInstance();
        ApplyRayAppearance();
    }

    void OnEnable()
    {
        Application.onBeforeRender += UpdateReticle;
    }

    void Start()
    {
        // Re-apply after every component in the imported interactor prefab has completed Awake.
        ResolveActiveInteractor();
        ApplyRayAppearance();
        UpdateReticle();
    }

    void LateUpdate()
    {
        if (nearFarInteractor == null || !nearFarInteractor.isActiveAndEnabled)
        {
            ResolveActiveInteractor();
            ApplyRayAppearance();
        }

        UpdateReticle();
    }

    void OnDisable()
    {
        Application.onBeforeRender -= UpdateReticle;
        SetReticleActive(false);
    }

    void OnDestroy()
    {
        Application.onBeforeRender -= UpdateReticle;
        if (reticleInstance != null)
            Destroy(reticleInstance);
    }

    void ResolveActiveInteractor()
    {
        if (nearFarInteractor == null || !nearFarInteractor.isActiveAndEnabled)
            nearFarInteractor = GetComponentInChildren<NearFarInteractor>();

        if (nearFarInteractor == null)
            return;

        if (curveVisualController == null || !curveVisualController.gameObject.activeInHierarchy)
            curveVisualController = nearFarInteractor.GetComponentInChildren<CurveVisualController>();
    }

    void CreateReticleInstance()
    {
        if (reticleInstance != null || reticlePrefab == null)
            return;

        reticleInstance = Instantiate(reticlePrefab);
        reticleInstance.name = $"{gameObject.name} Ray Reticle";
        authoredReticleScale = reticleInstance.transform.localScale;
        reticleInstance.SetActive(false);
    }

    void ApplyRayAppearance()
    {
        if (curveVisualController == null)
            return;

        curveVisualController.maxVisualCurveDistance = rayDistance;
        curveVisualController.extendLineToEmptyHit = extendRayToEmptyHit;

        if (nearFarInteractor != null
            && nearFarInteractor.farInteractionCaster is CurveInteractionCaster curveInteractionCaster)
        {
            curveInteractionCaster.castDistance = rayDistance;
        }

        if (appearanceTarget == curveVisualController)
            return;

        if (!overrideRayAppearance)
        {
            appearanceTarget = curveVisualController;
            return;
        }

        LineRenderer lineRenderer = curveVisualController.lineRenderer;
        if (lineRenderer != null)
        {
            if (rayMaterial != null)
                lineRenderer.sharedMaterial = rayMaterial;
            if (rayGradient != null)
                lineRenderer.colorGradient = rayGradient;
        }

        if (rayGradient != null)
        {
            ApplyGradient(curveVisualController.noValidHitProperties);
            ApplyGradient(curveVisualController.uiHitProperties);
            ApplyGradient(curveVisualController.uiPressHitProperties);
            ApplyGradient(curveVisualController.selectHitProperties);
            ApplyGradient(curveVisualController.hoverHitProperties);
        }

        appearanceTarget = curveVisualController;
    }

    void ApplyGradient(LineProperties properties)
    {
        if (properties == null)
            return;

        properties.adjustGradient = true;
        properties.gradient = rayGradient;
    }

    void UpdateReticle()
    {
        if (reticleInstance == null)
            CreateReticleInstance();

        if (reticleInstance == null || nearFarInteractor == null || !nearFarInteractor.isActiveAndEnabled)
        {
            SetReticleActive(false);
            return;
        }

        var curveProvider = (ICurveInteractionDataProvider)nearFarInteractor;
        if (!curveProvider.isActive || (!showWhileSelecting && nearFarInteractor.hasSelection))
        {
            SetReticleActive(false);
            return;
        }

        EndPointType endPointType = nearFarInteractor.TryGetCurveEndPoint(
            out Vector3 endPoint, snapToSelectedAttachIfAvailable: true, snapToSnapVolumeIfAvailable: true);
        if (endPointType == EndPointType.None || (endPointType == EndPointType.EmptyCastHit && !showOnEmptyGeometry))
        {
            SetReticleActive(false);
            return;
        }

        Vector3 origin = curveProvider.curveOrigin != null
            ? curveProvider.curveOrigin.position
            : nearFarInteractor.transform.position;
        Vector3 towardOrigin = origin - endPoint;
        if (towardOrigin.sqrMagnitude < 0.000001f)
            towardOrigin = -nearFarInteractor.transform.forward;
        towardOrigin.Normalize();

        EndPointType normalType = nearFarInteractor.TryGetCurveEndNormal(
            out Vector3 endNormal, snapToSelectedAttachIfAvailable: true);
        if (normalType == EndPointType.None || endNormal.sqrMagnitude < 0.000001f)
            endNormal = towardOrigin;
        else
            endNormal.Normalize();

        if (Vector3.Dot(endNormal, towardOrigin) < 0f)
            endNormal = -endNormal;

        Vector3 reticleUp = Vector3.ProjectOnPlane(Vector3.up, endNormal).normalized;
        if (reticleUp.sqrMagnitude < 0.000001f)
            reticleUp = Vector3.ProjectOnPlane(nearFarInteractor.transform.up, endNormal).normalized;
        if (reticleUp.sqrMagnitude < 0.000001f)
            reticleUp = Vector3.up;

        Transform reticleTransform = reticleInstance.transform;
        reticleTransform.SetPositionAndRotation(
            endPoint + endNormal * surfaceOffset,
            Quaternion.LookRotation(endNormal, reticleUp));

        float distanceScale = 1f;
        if (scaleWithDistance)
        {
            float distance = Vector3.Distance(origin, endPoint);
            float min = Mathf.Min(minimumDistanceScale, maximumDistanceScale);
            float max = Mathf.Max(minimumDistanceScale, maximumDistanceScale);
            distanceScale = Mathf.Clamp(distance, min, max);
        }

        reticleTransform.localScale = authoredReticleScale * distanceScale;
        SetReticleActive(true);
    }

    void SetReticleActive(bool active)
    {
        if (reticleInstance != null && reticleInstance.activeSelf != active)
            reticleInstance.SetActive(active);
    }
}
