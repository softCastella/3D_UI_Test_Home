using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class ScenarioCardGroupBuilder
{
    private const string TargetSceneName = "3_PPE_Room";
    private const string HudName = "Scenario Selection HUD (1)";
    private const string LayoutMarkerName = "__HorizontalCardGroups_v3";
    private const string Card3GroupName = "ScenarioCard3_Group";

    [MenuItem("Tools/Scenario HUD/Create Overlapping Card Groups")]
    private static void BuildGroupsOnce()
    {
        BuildInScene(SceneManager.GetActiveScene());
    }

    private static void BuildInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || scene.name != TargetSceneName)
            return;

        Transform hud = FindSceneTransform(scene, HudName);
        if (hud == null)
            return;

        bool changed = false;
        changed |= CreateGroup(hud, "LeftColor_Group", "SciFiCard_CornerMarkerCard",
            "LeftColor");
        changed |= CreateGroup(hud, "RightColor_Group", "SciFiCard_FloatingShadow",
            "RightColor");
        changed |= CreateScenarioCard3Group(hud);
        changed |= AdjustScenarioCard3DuplicateTransparency(hud);

        if (hud.Find(LayoutMarkerName) == null)
        {
            Transform leftGroup = hud.Find("LeftColor_Group");
            Transform rightGroup = hud.Find("RightColor_Group");
            if (leftGroup != null && rightGroup != null)
            {
                ApplyHorizontalLayout(leftGroup);
                ApplyHorizontalLayout(rightGroup);
                leftGroup.gameObject.SetActive(true);
                rightGroup.gameObject.SetActive(false);

                GameObject marker = new(LayoutMarkerName) { hideFlags = HideFlags.HideInHierarchy };
                marker.transform.SetParent(hud, false);
                Undo.RegisterCreatedObjectUndo(marker, "Mark horizontal card group layout");
                changed = true;
            }
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Created LeftColor_Group and RightColor_Group in Scenario Selection HUD (1). Save the scene.");
        }
    }

    private static bool CreateGroup(Transform hud, string groupName, string sourceName,
        string cardPrefix)
    {
        // A completed group is entirely scene-authored from this point onward.
        // This tool never updates or resets it again.
        if (hud.Find(groupName) != null)
            return false;

        Transform source = FindDescendant(hud, sourceName);
        if (source == null)
            return false;

        Transform oldGeneratedCard1 = hud.Find($"{cardPrefix}_Card_1");
        if (oldGeneratedCard1 != null && oldGeneratedCard1 != source)
            Undo.DestroyObjectImmediate(oldGeneratedCard1.gameObject);

        GameObject groupObject = new(groupName);
        Undo.RegisterCreatedObjectUndo(groupObject, "Create overlapping card group");
        Transform group = groupObject.transform;
        group.SetParent(hud, false);
        group.localPosition = Vector3.zero;
        group.localRotation = Quaternion.identity;
        group.localScale = Vector3.one;

        source.name = $"{cardPrefix}_Card_1";
        Undo.SetTransformParent(source, group, "Move card into group");
        SetCardTransform(source, -440f);
        source.gameObject.SetActive(true);

        for (int index = 2; index <= 3; index++)
        {
            string cardName = $"{cardPrefix}_Card_{index}";
            Transform card = hud.Find(cardName);
            if (card == null)
            {
                GameObject clone = Object.Instantiate(source.gameObject, group, false);
                clone.name = cardName;
                card = clone.transform;
                Undo.RegisterCreatedObjectUndo(clone, "Create overlapping scenario card");
            }
            else
            {
                Undo.SetTransformParent(card, group, "Move card into group");
            }

            SetCardTransform(card, (index - 2) * 440f);
            card.gameObject.SetActive(true);
        }

        return true;
    }

    private static bool CreateScenarioCard3Group(Transform hud)
    {
        if (hud.Find(Card3GroupName) != null)
            return false;

        Transform source = hud.Find("Scenario Card 3");
        if (source == null || source is not RectTransform sourceRect)
            return false;

        GameObject groupObject = new(Card3GroupName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(groupObject, "Create Scenario Card 3 group");
        RectTransform group = groupObject.GetComponent<RectTransform>();
        group.SetParent(hud, false);
        group.anchorMin = new Vector2(0.5f, 0.5f);
        group.anchorMax = new Vector2(0.5f, 0.5f);
        group.pivot = new Vector2(0.5f, 0.5f);
        group.anchoredPosition = Vector2.zero;
        group.localRotation = Quaternion.identity;
        group.localScale = Vector3.one;

        Undo.SetTransformParent(sourceRect, group, "Move Scenario Card 3 into group");
        sourceRect.name = "ScenarioCard3_Card_1";
        SetUiCardTransform(sourceRect, -440f);
        sourceRect.gameObject.SetActive(true);

        for (int index = 2; index <= 3; index++)
        {
            GameObject clone = Object.Instantiate(sourceRect.gameObject, group, false);
            clone.name = $"ScenarioCard3_Card_{index}";
            Undo.RegisterCreatedObjectUndo(clone, "Create Scenario Card 3 variant");
            SetUiCardTransform(clone.GetComponent<RectTransform>(), (index - 2) * 440f);
            clone.SetActive(true);
        }

        groupObject.SetActive(false);
        return true;
    }

    private static bool AdjustScenarioCard3DuplicateTransparency(Transform hud)
    {
        Transform duplicateGroup = FindDescendant(hud, "ScenarioCard3_Group (1)");
        if (duplicateGroup == null)
            return false;

        foreach (RoundedRectangleGraphic graphic in duplicateGroup.GetComponentsInChildren<RoundedRectangleGraphic>(true))
        {
            string shaderName = graphic.material != null && graphic.material.shader != null
                ? graphic.material.shader.name
                : "UI Default";
            Debug.Log($"[CardTransparency] {graphic.name}: active={graphic.gameObject.activeInHierarchy}, "
                + $"color={graphic.color}, shader={shaderName}", graphic);
        }

        if (duplicateGroup.Find("__TransparencyAdjusted_v2") != null)
            return false;

        bool adjusted = false;
        for (int index = 0; index < duplicateGroup.childCount; index++)
        {
            Transform card = duplicateGroup.GetChild(index);
            if (!card.TryGetComponent(out RoundedRectangleGraphic panel))
                continue;

            Undo.RecordObject(panel, "Make Scenario Card 3 duplicate translucent");
            Color color = panel.color;
            color.a = 0.52f;
            panel.color = color;
            EditorUtility.SetDirty(panel);
            adjusted = true;
        }

        if (!adjusted)
            return false;

        GameObject marker = new("__TransparencyAdjusted_v2")
        {
            hideFlags = HideFlags.HideInHierarchy
        };
        marker.transform.SetParent(duplicateGroup, false);
        Undo.RegisterCreatedObjectUndo(marker, "Mark Scenario Card 3 transparency adjustment");
        return true;
    }

    private static void SetUiCardTransform(RectTransform card, float x)
    {
        card.anchorMin = new Vector2(0.5f, 0.5f);
        card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.anchoredPosition = new Vector2(x, 0f);
        card.sizeDelta = new Vector2(360f, 490f);
        card.localRotation = Quaternion.identity;
        card.localScale = Vector3.one;
    }

    private static void ApplyHorizontalLayout(Transform group)
    {
        group.localPosition = Vector3.zero;
        group.localRotation = Quaternion.identity;
        group.localScale = Vector3.one;

        for (int index = 0; index < group.childCount && index < 3; index++)
        {
            Transform card = group.GetChild(index);
            SetCardTransform(card, (index - 1) * 440f);
            card.gameObject.SetActive(true);
        }
    }

    private static void SetCardTransform(Transform card, float x)
    {
        card.localPosition = new Vector3(x, 0f, 0f);
        card.localRotation = Quaternion.identity;
        card.localScale = Vector3.one * 500f;
    }

    private static Transform FindSceneTransform(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == objectName)
                    return candidate;
            }
        }

        return null;
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
        {
            if (candidate.name == objectName)
                return candidate;
        }

        return null;
    }
}
