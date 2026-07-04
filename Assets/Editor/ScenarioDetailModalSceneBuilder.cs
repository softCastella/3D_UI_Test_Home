using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

[InitializeOnLoad]
internal static class ScenarioDetailModalSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/3_PPE_Room.unity";
    private const string ModalName = "Scenario Detail Modal";

    static ScenarioDetailModalSceneBuilder()
    {
        EditorApplication.delayCall += BuildLoadedTargetSceneOnce;
    }

    [MenuItem("Tools/Scenario HUD/Create Scenario Detail Modal")]
    private static void BuildLoadedTargetSceneOnce()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        ScenarioSelectionHud hud = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<ScenarioSelectionHud>(true))
            .FirstOrDefault();
        if (hud == null)
            return;

        bool controllersAdded = AddQuestControllerInteractors(scene);
        if (hud.GetComponent<ScenarioDetailModal>() != null)
        {
            if (controllersAdded)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            return;
        }

        Canvas canvas = hud.GetComponent<Canvas>();
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Font/Pretendard-Medium SDF.asset");

        GameObject modalRoot = UiObject(ModalName, canvas.transform);
        SetRect(modalRoot, Vector2.zero, new Vector2(4000f, 3000f));
        Image overlay = modalRoot.AddComponent<Image>();
        overlay.color = new Color(0.02f, 0.035f, 0.05f, 0.68f);
        overlay.raycastTarget = true;

        GameObject panel = UiObject("Modal Panel", modalRoot.transform);
        SetRect(panel, Vector2.zero, new Vector2(1050f, 520f));
        RoundedRectangleGraphic panelGraphic = panel.AddComponent<RoundedRectangleGraphic>();
        panelGraphic.color = new Color(0.86f, 0.9f, 0.92f, 0.94f);
        panelGraphic.CornerRadius = 42f;
        Shadow shadow = panel.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
        shadow.effectDistance = new Vector2(0f, -12f);

        GameObject numberPlateObject = UiObject("Scenario Number Plate", panel.transform);
        SetRect(numberPlateObject, new Vector2(0f, 205f), new Vector2(110f, 80f));
        Image numberPlate = numberPlateObject.AddComponent<Image>();
        numberPlate.color = new Color(0.48f, 0.52f, 0.55f, 1f);
        numberPlate.raycastTarget = false;
        TMP_Text number = Text("Scenario Number", numberPlateObject.transform, font, "1", 36f,
            FontStyles.Bold, TextAlignmentOptions.Center, Vector2.zero, new Vector2(110f, 80f));

        TMP_Text title = Text("Scenario Title", panel.transform, font,
            "화학물질 화재 발생 시\n소화 약제 선택 및 진압 훈련", 27f, FontStyles.Bold,
            TextAlignmentOptions.Center, new Vector2(0f, 110f), new Vector2(760f, 90f));
        TMP_Text description = Text("Scenario Description", panel.transform, font,
            "시나리오 카드를 선택하면 상세 설명이 표시됩니다.", 22f, FontStyles.Normal,
            TextAlignmentOptions.TopLeft, new Vector2(0f, -5f), new Vector2(760f, 130f));

        Button trainingButton = Button("Training Select Button", panel.transform, font,
            "훈련 선택", new Vector2(-205f, -190f), new Vector2(330f, 72f));
        Button backButton = Button("Back Button", panel.transform, font,
            "돌아가기", new Vector2(205f, -190f), new Vector2(330f, 72f));
        TMP_Text trainingLabel = trainingButton.GetComponentInChildren<TMP_Text>();

        ScenarioDetailModal controller = hud.gameObject.AddComponent<ScenarioDetailModal>();
        SerializedObject serialized = new(controller);
        serialized.FindProperty("modalRoot").objectReferenceValue = modalRoot;
        serialized.FindProperty("numberText").objectReferenceValue = number;
        serialized.FindProperty("titleText").objectReferenceValue = title;
        serialized.FindProperty("descriptionText").objectReferenceValue = description;
        serialized.FindProperty("trainingButtonText").objectReferenceValue = trainingLabel;
        serialized.FindProperty("trainingButton").objectReferenceValue = trainingButton;
        serialized.FindProperty("backButton").objectReferenceValue = backButton;

        string[] titles =
        {
            "화학물질 화재 발생 시\n소화 약제 선택 및 진압 훈련",
            "밀폐공간 작업 전\n개인보호구 선택 및 착용 훈련",
            "비상 상황 발생 시\n대피 절차 및 보고 훈련"
        };
        string[] descriptions =
        {
            "화학물질 화재 발생 시 소화 약제 선택 및 진압 훈련\n특정 유기용제나 금속성(물과 반응하는) 화학물질 보관소에서 화재가 발생한 상황.\n불이 났을 때 무조건 물을 뿌리는 것이 아니라, 화학물질 종류에 맞는 소화기(CO2, 분말, 팽창질석 등)를 정확히 선택하여 초기 진압을 체험합니다.",
            "작업 환경의 유해 요인을 확인하고 필요한 개인보호구를 선택합니다.\n보호구의 손상 여부와 밀착 상태를 점검한 뒤 올바른 순서로 착용합니다.",
            "경보와 현장 위험 요소를 확인하고 지정된 대피 경로로 이동합니다.\n안전 구역 도착 후 인원을 확인하고 비상 연락 체계에 따라 상황을 보고합니다."
        };

        SerializedProperty scenarios = serialized.FindProperty("scenarios");
        scenarios.arraySize = 3;
        Button[] allButtons = canvas.GetComponentsInChildren<Button>(true)
            .Where(button => !button.transform.IsChildOf(modalRoot.transform)).ToArray();
        for (int i = 0; i < scenarios.arraySize; i++)
        {
            SerializedProperty item = scenarios.GetArrayElementAtIndex(i);
            List<Button> matches = allButtons.Where(button => MatchesScenario(button.name, i + 1)).ToList();
            SerializedProperty buttons = item.FindPropertyRelative("selectionButtons");
            buttons.arraySize = matches.Count;
            for (int j = 0; j < matches.Count; j++)
                buttons.GetArrayElementAtIndex(j).objectReferenceValue = matches[j];
            item.FindPropertyRelative("number").stringValue = (i + 1).ToString();
            item.FindPropertyRelative("title").stringValue = titles[i];
            item.FindPropertyRelative("description").stringValue = descriptions[i];
            item.FindPropertyRelative("trainingButtonLabel").stringValue = "훈련 선택";
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();

        modalRoot.SetActive(false);
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Created and serialized Scenario Detail Modal. Inspector values are now authoritative.", controller);
    }

    private static bool AddQuestControllerInteractors(Scene scene)
    {
        Transform xrOrigin = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(candidate => candidate.name == "XR Origin (VR)");
        Transform cameraOffset = xrOrigin != null ? xrOrigin.Find("Camera Offset") : null;
        if (cameraOffset == null)
            return false;

        bool changed = false;
        if (xrOrigin.GetComponent<XRInteractionManager>() == null)
        {
            xrOrigin.gameObject.AddComponent<XRInteractionManager>();
            changed = true;
        }

        InputActionManager actionManager = xrOrigin.GetComponent<InputActionManager>();
        if (actionManager == null)
        {
            actionManager = xrOrigin.gameObject.AddComponent<InputActionManager>();
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/XRI Default Input Actions.inputactions");
            SerializedObject serializedManager = new(actionManager);
            SerializedProperty actionAssets = serializedManager.FindProperty("m_ActionAssets");
            actionAssets.arraySize = actions != null ? 1 : 0;
            if (actions != null)
                actionAssets.GetArrayElementAtIndex(0).objectReferenceValue = actions;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();
            changed = true;
        }

        changed |= AddInteractor(cameraOffset, "Left_NearFarInteractor",
            "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/Prefabs/Interactors/Left_NearFarInteractor.prefab");
        changed |= AddInteractor(cameraOffset, "Right_NearFarInteractor",
            "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/Prefabs/Interactors/Right_NearFarInteractor.prefab");
        return changed;
    }

    private static bool AddInteractor(Transform parent, string objectName, string prefabPath)
    {
        if (parent.Find(objectName) != null)
            return false;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"Quest controller interactor prefab was not found: {prefabPath}");
            return false;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = objectName;
        instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        instance.transform.localScale = Vector3.one;
        return true;
    }

    private static bool MatchesScenario(string objectName, int number)
    {
        return objectName == $"Scenario Card {number}"
            || objectName.EndsWith($"_Card_{number}");
    }

    private static GameObject UiObject(string name, Transform parent)
    {
        GameObject gameObject = new(name, typeof(RectTransform));
        gameObject.layer = 5;
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void SetRect(GameObject gameObject, Vector2 position, Vector2 size)
    {
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static TMP_Text Text(string name, Transform parent, TMP_FontAsset font, string value,
        float size, FontStyles style, TextAlignmentOptions alignment, Vector2 position, Vector2 dimensions)
    {
        GameObject gameObject = UiObject(name, parent);
        SetRect(gameObject, position, dimensions);
        TextMeshProUGUI text = gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = new Color(0.04f, 0.055f, 0.065f, 1f);
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        return text;
    }

    private static Button Button(string name, Transform parent, TMP_FontAsset font, string label,
        Vector2 position, Vector2 dimensions)
    {
        GameObject gameObject = UiObject(name, parent);
        SetRect(gameObject, position, dimensions);
        Image image = gameObject.AddComponent<Image>();
        image.color = new Color(0.43f, 0.46f, 0.48f, 1f);
        Button button = gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        Text(name + " Label", gameObject.transform, font, label, 24f, FontStyles.Bold,
            TextAlignmentOptions.Center, Vector2.zero, dimensions);
        return button;
    }
}
