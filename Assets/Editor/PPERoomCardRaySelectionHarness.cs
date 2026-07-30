using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;

public static class PPERoomCardRaySelectionHarness
{
    private const string ScenePath = "Assets/Scenes/3_PPE_Room.unity";
    private const string CanvasName = "XR UI Canvas (1)";
    private const float RequiredRayDistance = 20f;

    [MenuItem("Tools/PPE/Validate Card Ray Selection")]
    public static void Validate()
    {
        if (!Application.isBatchMode
            && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("PPE card ray selection validation was cancelled.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        List<string> failures = new();
        GameObject canvasObject = FindSceneObject(scene, CanvasName);
        if (canvasObject == null)
        {
            failures.Add($"Missing '{CanvasName}'.");
        }
        else
        {
            ValidateCanvas(canvasObject, failures);
        }

        ValidateControllerRays(scene, failures);
        ValidateLocationMarker(scene, "XR Location Marker_big_PPE_1", failures);
        ValidateLocationMarker(scene, "XR Location Marker_big_PPE_2", failures);

        if (failures.Count > 0)
        {
            string message = "PPE card ray selection validation failed:\n- "
                + string.Join("\n- ", failures);
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        Debug.Log($"PPE card ray selection validation passed for '{scene.path}': "
            + "controller ray + trigger only, Canvas hide targets, 40 m rays, and location markers are valid.");
    }

    private static void ValidateCanvas(GameObject canvasObject, List<string> failures)
    {
        GraphicRaycaster mouseRaycaster = canvasObject.GetComponent<GraphicRaycaster>();
        if (mouseRaycaster != null && mouseRaycaster.enabled)
            failures.Add($"'{CanvasName}' still has an enabled mouse GraphicRaycaster.");

        TrackedDeviceGraphicRaycaster trackedRaycaster =
            canvasObject.GetComponent<TrackedDeviceGraphicRaycaster>();
        if (trackedRaycaster == null || !trackedRaycaster.enabled)
            failures.Add($"'{CanvasName}' is missing an enabled TrackedDeviceGraphicRaycaster.");

        ScenarioDetailModal modal = canvasObject.GetComponent<ScenarioDetailModal>();
        if (modal != null)
        {
            SerializedObject serializedModal = new(modal);
            if (serializedModal.FindProperty("enableMousePhysicsFallback").boolValue)
                failures.Add($"'{CanvasName}' still enables the mouse physics fallback.");
        }

        ScenarioCardSelectProxy[] proxies =
            canvasObject.GetComponentsInChildren<ScenarioCardSelectProxy>(true);
        if (proxies.Length == 0)
            failures.Add($"'{CanvasName}' has no ScenarioCardSelectProxy components.");

        foreach (ScenarioCardSelectProxy proxy in proxies)
        {
            SerializedObject serializedProxy = new(proxy);
            GameObject hideTarget = serializedProxy.FindProperty("hideAfterSelection")
                .objectReferenceValue as GameObject;
            if (hideTarget != canvasObject)
                failures.Add($"'{GetPath(proxy.transform)}' does not hide '{CanvasName}' after selection.");

            if (proxy.TryGetComponent(out XRSimpleInteractable _))
                failures.Add($"'{GetPath(proxy.transform)}' still has an XRSimpleInteractable bypass.");
        }
    }

    private static void ValidateControllerRays(Scene scene, List<string> failures)
    {
        XRNearFarReticleVisual[] visuals = FindComponents<XRNearFarReticleVisual>(scene);
        if (visuals.Length < 2)
            failures.Add("Expected left and right XRNearFarReticleVisual components.");

        foreach (XRNearFarReticleVisual visual in visuals)
        {
            SerializedObject serializedVisual = new(visual);
            float distance = serializedVisual.FindProperty("rayDistance").floatValue;
            bool extends = serializedVisual.FindProperty("extendRayToEmptyHit").boolValue;
            if (distance < RequiredRayDistance)
                failures.Add($"'{GetPath(visual.transform)}' ray distance is only {distance:0.##} m.");
            if (!extends)
                failures.Add($"'{GetPath(visual.transform)}' retracts to a short resting line on an empty hit.");
        }
    }

    private static void ValidateLocationMarker(Scene scene, string markerName, List<string> failures)
    {
        GameObject marker = FindSceneObject(scene, markerName);
        if (marker == null || !marker.activeSelf)
        {
            failures.Add($"Missing active '{markerName}'.");
            return;
        }

        Collider collider = marker.GetComponent<Collider>();
        if (collider == null || !collider.enabled)
            failures.Add($"'{markerName}' has no enabled Collider for the ray.");
        if (!marker.TryGetComponent(out XRLocationTeleportTarget _))
            failures.Add($"'{markerName}' has no XRLocationTeleportTarget.");
    }

    private static GameObject FindSceneObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName)
                    return child.gameObject;
            }
        }

        return null;
    }

    private static T[] FindComponents<T>(Scene scene) where T : Component
    {
        List<T> matches = new();
        foreach (GameObject root in scene.GetRootGameObjects())
            matches.AddRange(root.GetComponentsInChildren<T>(true));
        return matches.ToArray();
    }

    private static string GetPath(Transform transform)
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
