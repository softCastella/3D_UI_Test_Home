using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

/// <summary>
/// Creates and validates the teleport-only XRI locomotion stack used by 3_PPE_Room.
/// This intentionally adds no continuous move or turn providers.
/// </summary>
public static class PPERoomTeleportationSetup
{
    const string ScenePath = "Assets/Scenes/3_PPE_Room.unity";
    const string RootName = "PPE Teleport-Only Locomotion";
    const string ProviderName = "Teleportation Provider";

    static readonly string[] MarkerNames =
    {
        "XR Location Marker_big_PPE_1",
        "XR Location Marker_big_PPE_2",
    };

    static readonly string[] ArrivalAnchorNames =
    {
        "PPE_1 Arrival Anchor",
        "PPE_2 Arrival Anchor",
    };

    [MenuItem("Tools/PPE/Setup Teleport-Only Locomotion")]
    public static void Setup()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("PPE teleport-only locomotion setup was cancelled.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        XROrigin xrOrigin = FindSingle<XROrigin>(scene);
        if (xrOrigin == null)
            throw new InvalidOperationException($"'{ScenePath}' has no XROrigin.");

        Transform stackRoot = FindDirectChild(xrOrigin.transform, RootName);
        if (stackRoot == null)
        {
            GameObject rootObject = new(RootName);
            Undo.RegisterCreatedObjectUndo(rootObject, "Create PPE teleport-only locomotion");
            stackRoot = rootObject.transform;
            Undo.SetTransformParent(stackRoot, xrOrigin.transform, "Parent PPE teleport-only locomotion");
            stackRoot.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            stackRoot.localScale = Vector3.one;
        }

        XRBodyTransformer bodyTransformer = GetOrAdd<XRBodyTransformer>(stackRoot.gameObject);
        LocomotionMediator mediator = GetOrAdd<LocomotionMediator>(stackRoot.gameObject);

        SerializedObject serializedBody = new(bodyTransformer);
        SerializedProperty xrOriginProperty = serializedBody.FindProperty("m_XROrigin");
        if (xrOriginProperty == null)
            throw new MissingFieldException(nameof(XRBodyTransformer), "m_XROrigin");
        xrOriginProperty.objectReferenceValue = xrOrigin;
        serializedBody.ApplyModifiedProperties();

        Transform providerTransform = FindDirectChild(stackRoot, ProviderName);
        if (providerTransform == null)
        {
            GameObject providerObject = new(ProviderName);
            Undo.RegisterCreatedObjectUndo(providerObject, "Create PPE teleportation provider");
            providerTransform = providerObject.transform;
            Undo.SetTransformParent(providerTransform, stackRoot, "Parent PPE teleportation provider");
            providerTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            providerTransform.localScale = Vector3.one;
        }

        TeleportationProvider provider = GetOrAdd<TeleportationProvider>(providerTransform.gameObject);
        Undo.RecordObject(provider, "Connect PPE teleportation provider");
        provider.mediator = mediator;
        EditorUtility.SetDirty(provider);

        for (int markerIndex = 0; markerIndex < MarkerNames.Length; markerIndex++)
        {
            string markerName = MarkerNames[markerIndex];
            GameObject marker = FindSceneObject(scene, markerName);
            XRLocationTeleportTarget target = marker != null
                ? marker.GetComponent<XRLocationTeleportTarget>()
                : null;
            if (target == null)
                throw new InvalidOperationException($"Missing XRLocationTeleportTarget on '{markerName}'.");

            Transform markerParent = marker.transform.parent;
            if (markerParent == null)
                throw new InvalidOperationException($"'{markerName}' must have a parent for its arrival anchor.");

            string anchorName = ArrivalAnchorNames[markerIndex];
            Transform arrivalAnchor = FindDirectChild(markerParent, anchorName);
            if (arrivalAnchor == null)
            {
                GameObject anchorObject = new(anchorName);
                Undo.RegisterCreatedObjectUndo(anchorObject, $"Create {anchorName}");
                arrivalAnchor = anchorObject.transform;
                Undo.SetTransformParent(arrivalAnchor, markerParent, $"Parent {anchorName}");
                arrivalAnchor.SetLocalPositionAndRotation(marker.transform.localPosition, Quaternion.Euler(0f, 180f, 0f));
                arrivalAnchor.localScale = Vector3.one;
            }

            SerializedObject serializedTarget = new(target);
            SerializedProperty providerProperty = serializedTarget.FindProperty("teleportationProvider");
            SerializedProperty destinationProperty = serializedTarget.FindProperty("destination");
            if (providerProperty == null)
                throw new MissingFieldException(nameof(XRLocationTeleportTarget), "teleportationProvider");
            if (destinationProperty == null)
                throw new MissingFieldException(nameof(XRLocationTeleportTarget), "destination");
            providerProperty.objectReferenceValue = provider;
            destinationProperty.objectReferenceValue = arrivalAnchor;
            serializedTarget.ApplyModifiedProperties();
        }

        int configuredInteractors = 0;
        foreach (NearFarInteractor interactor in FindComponents<NearFarInteractor>(scene))
        {
            if (!interactor.gameObject.activeInHierarchy
                || (interactor.name != "Left_NearFarInteractor" && interactor.name != "Right_NearFarInteractor"))
            {
                continue;
            }

            Undo.RecordObject(interactor, "Allow hovered trigger activation");
            interactor.allowHoveredActivate = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(interactor);
            EditorUtility.SetDirty(interactor);
            configuredInteractors++;
        }

        if (configuredInteractors < 2)
            throw new InvalidOperationException("Expected active left and right NearFarInteractor components.");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        ValidateScene(scene);
        Debug.Log("PPE teleport-only locomotion setup complete: provider stack, two markers, and both hovered-trigger interactors are connected.");
    }

    [MenuItem("Tools/PPE/Validate Teleport-Only Locomotion")]
    public static void Validate()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("PPE teleport-only locomotion validation was cancelled.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ValidateScene(scene);
    }

    static void ValidateScene(Scene scene)
    {
        List<string> failures = new();
        XROrigin xrOrigin = FindSingle<XROrigin>(scene);
        XRBodyTransformer bodyTransformer = FindSingle<XRBodyTransformer>(scene);
        LocomotionMediator mediator = FindSingle<LocomotionMediator>(scene);
        TeleportationProvider provider = FindSingle<TeleportationProvider>(scene);

        if (xrOrigin == null)
            failures.Add("Missing XROrigin.");
        if (bodyTransformer == null)
            failures.Add("Missing XRBodyTransformer.");
        else if (bodyTransformer.xrOrigin != xrOrigin)
            failures.Add("XRBodyTransformer does not reference the scene XROrigin.");
        if (mediator == null)
            failures.Add("Missing LocomotionMediator.");
        if (provider == null)
            failures.Add("Missing TeleportationProvider.");
        else if (provider.mediator != mediator)
            failures.Add("TeleportationProvider does not reference the LocomotionMediator.");

        foreach (LocomotionProvider locomotionProvider in FindComponents<LocomotionProvider>(scene))
        {
            if (locomotionProvider.gameObject.activeInHierarchy && locomotionProvider is not TeleportationProvider)
                failures.Add($"Unexpected non-teleport locomotion provider: '{GetPath(locomotionProvider.transform)}'.");
        }

        for (int markerIndex = 0; markerIndex < MarkerNames.Length; markerIndex++)
        {
            string markerName = MarkerNames[markerIndex];
            GameObject marker = FindSceneObject(scene, markerName);
            XRLocationTeleportTarget target = marker != null
                ? marker.GetComponent<XRLocationTeleportTarget>()
                : null;
            if (target == null)
            {
                failures.Add($"Missing XRLocationTeleportTarget on '{markerName}'.");
                continue;
            }

            SerializedObject serializedTarget = new(target);
            UnityEngine.Object assignedProvider = serializedTarget.FindProperty("teleportationProvider")?.objectReferenceValue;
            Transform assignedDestination = serializedTarget.FindProperty("destination")?.objectReferenceValue as Transform;
            float arrivalOffset = serializedTarget.FindProperty("arrivalForwardOffset")?.floatValue ?? 0f;
            if (assignedProvider != provider)
                failures.Add($"'{markerName}' does not reference the scene TeleportationProvider.");

            GameObject anchorObject = FindSceneObject(scene, ArrivalAnchorNames[markerIndex]);
            Transform expectedAnchor = anchorObject != null ? anchorObject.transform : null;
            if (expectedAnchor == null)
            {
                failures.Add($"Missing arrival anchor '{ArrivalAnchorNames[markerIndex]}'.");
                continue;
            }

            if (assignedDestination != expectedAnchor)
                failures.Add($"'{markerName}' does not reference '{ArrivalAnchorNames[markerIndex]}' as its destination.");
            if (Vector3.Distance(expectedAnchor.position, marker.transform.position) > 0.001f)
                failures.Add($"'{ArrivalAnchorNames[markerIndex]}' is not centered on '{markerName}'.");

            Vector3 planarForward = Vector3.ProjectOnPlane(expectedAnchor.forward, Vector3.up).normalized;
            if (planarForward.sqrMagnitude < 0.5f || Vector3.Dot(planarForward, Vector3.back) < 0.99f)
                failures.Add($"'{ArrivalAnchorNames[markerIndex]}' must face the PPE stands (-Z).");
            if (arrivalOffset <= 0f)
                failures.Add($"'{markerName}' needs a positive Arrival Forward Offset.");
        }

        int activeControllerInteractors = 0;
        UnityEngine.Object leftActivateAction = null;
        UnityEngine.Object rightActivateAction = null;
        foreach (NearFarInteractor interactor in FindComponents<NearFarInteractor>(scene))
        {
            if (!interactor.gameObject.activeInHierarchy
                || (interactor.name != "Left_NearFarInteractor" && interactor.name != "Right_NearFarInteractor"))
            {
                continue;
            }

            activeControllerInteractors++;
            if (!interactor.allowHoveredActivate)
                failures.Add($"'{GetPath(interactor.transform)}' has Allow Hovered Activate disabled.");

            bool isLeft = interactor.name == "Left_NearFarInteractor";
            InteractorHandedness expectedHandedness = isLeft
                ? InteractorHandedness.Left
                : InteractorHandedness.Right;
            if (interactor.handedness != expectedHandedness)
                failures.Add($"'{GetPath(interactor.transform)}' has the wrong handedness.");

            SerializedObject serializedInteractor = new(interactor);
            UnityEngine.Object activateAction = serializedInteractor
                .FindProperty("m_ActivateInput.m_InputActionReferencePerformed")
                ?.objectReferenceValue;
            UnityEngine.Object activateValueAction = serializedInteractor
                .FindProperty("m_ActivateInput.m_InputActionReferenceValue")
                ?.objectReferenceValue;
            if (activateAction == null || activateValueAction == null)
                failures.Add($"'{GetPath(interactor.transform)}' has no Trigger Activate action reference.");

            if (isLeft)
                leftActivateAction = activateAction;
            else
                rightActivateAction = activateAction;
        }

        if (activeControllerInteractors != 2)
            failures.Add("Expected exactly one active left and one active right NearFarInteractor component.");
        if (leftActivateAction != null && leftActivateAction == rightActivateAction)
            failures.Add("Left and right NearFarInteractors reference the same Trigger Activate action.");

        if (failures.Count > 0)
        {
            string message = "PPE teleport-only locomotion validation failed:\n- " + string.Join("\n- ", failures);
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        Debug.Log("PPE teleport-only locomotion validation passed: standard XRI provider stack, marker references, and hovered Trigger activation are valid.");
    }

    static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(gameObject);
    }

    static Transform FindDirectChild(Transform parent, string childName)
    {
        for (int index = 0; index < parent.childCount; index++)
        {
            Transform child = parent.GetChild(index);
            if (child.name == childName)
                return child;
        }

        return null;
    }

    static GameObject FindSceneObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name == objectName)
                    return transform.gameObject;
            }
        }

        return null;
    }

    static T FindSingle<T>(Scene scene) where T : Component
    {
        T[] components = FindComponents<T>(scene);
        return components.Length == 1 ? components[0] : null;
    }

    static T[] FindComponents<T>(Scene scene) where T : Component
    {
        List<T> matches = new();
        foreach (GameObject root in scene.GetRootGameObjects())
            matches.AddRange(root.GetComponentsInChildren<T>(true));
        return matches.ToArray();
    }

    static string GetPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = $"{transform.name}/{path}";
        }

        return path;
    }
}
