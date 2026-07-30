using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
internal static class XRUiCanvasPlayModeValidator
{
    const string CanvasName = "XR UI Canvas";
    const string SnapshotKey = "XRUiCanvasPlayModeValidator.Snapshot";

    [Serializable]
    struct TransformSnapshot
    {
        public string scenePath;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
        public Vector3 anchoredPosition3D;
    }

    static XRUiCanvasPlayModeValidator()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    [MenuItem("Tools/UI/Validate XR UI Canvas Play Mode Transform")]
    static void ValidateCurrentTransform()
    {
        if (!TryFindCanvas(out var canvas))
        {
            Debug.LogWarning($"Could not find {CanvasName} in the active scene.");
            return;
        }

        Debug.Log($"{CanvasName} authored transform is valid: local={canvas.transform.localPosition}, "
            + $"rotation={canvas.transform.localEulerAngles}, scale={canvas.transform.localScale}.", canvas);
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            if (TryFindCanvas(out var canvas) && canvas.transform is RectTransform rectTransform)
            {
                var snapshot = new TransformSnapshot
                {
                    scenePath = canvas.gameObject.scene.path,
                    localPosition = rectTransform.localPosition,
                    localRotation = rectTransform.localRotation,
                    localScale = rectTransform.localScale,
                    anchoredPosition3D = rectTransform.anchoredPosition3D,
                };
                SessionState.SetString(SnapshotKey, JsonUtility.ToJson(snapshot));
            }
        }
        else if (state == PlayModeStateChange.EnteredPlayMode)
        {
            CompareWithEditModeSnapshot();
        }
    }

    static void CompareWithEditModeSnapshot()
    {
        var json = SessionState.GetString(SnapshotKey, string.Empty);
        if (string.IsNullOrEmpty(json) || !TryFindCanvas(out var canvas) || canvas.transform is not RectTransform rectTransform)
            return;

        var snapshot = JsonUtility.FromJson<TransformSnapshot>(json);
        const float positionTolerance = 0.0001f;
        const float rotationTolerance = 0.01f;

        var changed = snapshot.scenePath != canvas.gameObject.scene.path
            || Vector3.Distance(snapshot.localPosition, rectTransform.localPosition) > positionTolerance
            || Quaternion.Angle(snapshot.localRotation, rectTransform.localRotation) > rotationTolerance
            || Vector3.Distance(snapshot.localScale, rectTransform.localScale) > positionTolerance
            || Vector3.Distance(snapshot.anchoredPosition3D, rectTransform.anchoredPosition3D) > positionTolerance;

        if (changed)
        {
            Debug.LogError($"{CanvasName} transform changed while entering Play Mode. "
                + $"Edit local={snapshot.localPosition}, Play local={rectTransform.localPosition}, "
                + $"Edit anchored={snapshot.anchoredPosition3D}, Play anchored={rectTransform.anchoredPosition3D}.", canvas);
        }
        else
        {
            Debug.Log($"{CanvasName} transform remained unchanged after entering Play Mode.", canvas);
        }
    }

    static bool TryFindCanvas(out Canvas targetCanvas)
    {
        targetCanvas = null;
        var activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid())
            return false;

        foreach (var root in activeScene.GetRootGameObjects())
        {
            foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                if (canvas.name != CanvasName)
                    continue;

                targetCanvas = canvas;
                return true;
            }
        }

        return false;
    }
}
