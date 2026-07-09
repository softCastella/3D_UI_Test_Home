using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class ScenarioDetailModal : MonoBehaviour
{
    [Serializable]
    public sealed class ScenarioDetail
    {
        [SerializeField] private Button[] selectionButtons;
        [SerializeField] private string number;
        [SerializeField, TextArea] private string title;
        [SerializeField, TextArea(3, 10)] private string description;
        [SerializeField] private string trainingButtonLabel;
        [SerializeField] private UnityEvent onTrainingSelected;
        [SerializeField] private UnityEvent onIncompletePpeScenarioSelected;
        [SerializeField] private UnityEvent onStandardTrainingScenarioSelected;

        public Button[] SelectionButtons => selectionButtons;
        public string Number => number;
        public string Title => title;
        public string Description => description;
        public string TrainingButtonLabel => trainingButtonLabel;
        public UnityEvent OnTrainingSelected => onTrainingSelected;
        public UnityEvent OnIncompletePpeScenarioSelected => onIncompletePpeScenarioSelected;
        public UnityEvent OnStandardTrainingScenarioSelected => onStandardTrainingScenarioSelected;
    }

    [Header("Scene References")]
    [SerializeField] private GameObject modalRoot;
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private GameObject[] scenarioTitleObjects;
    [SerializeField] private GameObject[] scenarioDescriptionObjects;
    [SerializeField] private TMP_Text trainingButtonText;
    [SerializeField] private Button trainingButton;
    [SerializeField] private Button backButton;

    [Header("Training Choice View")]
    [SerializeField] private string trainingChoiceTitle;
    [SerializeField, TextArea] private string trainingChoiceDescription;
    [SerializeField] private Button incompletePpeButton;
    [SerializeField] private Button standardTrainingButton;
    [SerializeField] private Button trainingChoiceBackButton;

    [Header("Inspector-authored Scenario Data")]
    [SerializeField] private ScenarioDetail[] scenarios;
    [SerializeField] private bool enableMousePhysicsFallback = true;
    [SerializeField] private Camera mouseRaycastCamera;
    [SerializeField] private LayerMask mouseRaycastMask = ~0;

    private int selectedScenario = -1;
    private readonly List<(Button button, UnityAction action)> selectionListeners = new();
    private Canvas ownerCanvas;

    private void OnEnable()
    {
        ownerCanvas = GetComponent<Canvas>();
        SetSelectionListeners(true);
        if (trainingButton != null)
            trainingButton.onClick.AddListener(SelectTraining);
        if (backButton != null)
            backButton.onClick.AddListener(Hide);
        if (incompletePpeButton != null)
            incompletePpeButton.onClick.AddListener(SelectIncompletePpeScenario);
        if (standardTrainingButton != null)
            standardTrainingButton.onClick.AddListener(SelectStandardTrainingScenario);
        if (trainingChoiceBackButton != null)
            trainingChoiceBackButton.onClick.AddListener(ReturnToScenarioDetails);
    }

    private void Update()
    {
        if (!enableMousePhysicsFallback || !WasMousePressedThisFrame())
            return;

        if (modalRoot != null && modalRoot.activeInHierarchy)
            return;

        if (TryShowFromMouseRaycast())
            return;

        LogPointerRaycastDiagnostics();
    }

    private void OnDisable()
    {
        SetSelectionListeners(false);
        if (trainingButton != null)
        {
            trainingButton.onClick.RemoveListener(SelectTraining);
        }
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(Hide);
        }
        if (incompletePpeButton != null)
            incompletePpeButton.onClick.RemoveListener(SelectIncompletePpeScenario);
        if (standardTrainingButton != null)
            standardTrainingButton.onClick.RemoveListener(SelectStandardTrainingScenario);
        if (trainingChoiceBackButton != null)
            trainingChoiceBackButton.onClick.RemoveListener(ReturnToScenarioDetails);
    }

    public void Show(int scenarioIndex)
    {
        if (scenarios == null || scenarioIndex < 0 || scenarioIndex >= scenarios.Length)
            return;

        ScenarioDetail detail = scenarios[scenarioIndex];
        if (detail == null || modalRoot == null)
            return;

        selectedScenario = scenarioIndex;
        ShowPrimaryActions(detail);
        ShowScenarioContent(scenarioIndex);
        if (numberText != null) numberText.text = detail.Number;
        if (!HasScenarioContentObjects())
        {
            if (titleText != null) titleText.text = detail.Title;
            if (descriptionText != null) descriptionText.text = detail.Description;
        }
        if (trainingButtonText != null) trainingButtonText.text = detail.TrainingButtonLabel;

        SetModalHierarchyActive(true);
        modalRoot.SetActive(true);
        modalRoot.transform.SetAsLastSibling();
        Debug.Log($"ScenarioDetailModal showing scenario {scenarioIndex + 1} on {modalRoot.name}.", modalRoot);
        LogModalTransformDiagnostics();
        trainingButton?.Select();
    }

    public void Hide()
    {
        selectedScenario = -1;
        SetModalHierarchyActive(false);
    }

    private void SelectTraining()
    {
        if (scenarios == null || selectedScenario < 0 || selectedScenario >= scenarios.Length)
            return;

        if (!HasTrainingChoiceConfiguration())
        {
            Debug.LogWarning("ScenarioDetailModal training choice references or serialized text are missing.", this);
            return;
        }

        ScenarioDetail detail = scenarios[selectedScenario];
        detail?.OnTrainingSelected?.Invoke();
        if (detail == null)
            return;

        if (!HasScenarioContentObjects())
        {
            if (titleText != null) titleText.text = trainingChoiceTitle;
            if (descriptionText != null) descriptionText.text = trainingChoiceDescription;
        }
        SetButtonVisible(trainingButton, false);
        SetButtonVisible(backButton, false);
        SetButtonVisible(incompletePpeButton, true);
        SetButtonVisible(standardTrainingButton, true);
        SetButtonVisible(trainingChoiceBackButton, true);
        incompletePpeButton?.Select();
    }

    private bool HasTrainingChoiceConfiguration()
    {
        return !string.IsNullOrWhiteSpace(trainingChoiceTitle)
            && !string.IsNullOrWhiteSpace(trainingChoiceDescription)
            && incompletePpeButton != null
            && standardTrainingButton != null
            && trainingChoiceBackButton != null;
    }

    private void SelectIncompletePpeScenario()
    {
        if (selectedScenario >= 0 && selectedScenario < scenarios.Length)
            scenarios[selectedScenario]?.OnIncompletePpeScenarioSelected?.Invoke();
    }

    private void SelectStandardTrainingScenario()
    {
        Debug.Log("ScenarioDetailModal standard training scenario button selected.", this);
        if (selectedScenario >= 0 && selectedScenario < scenarios.Length)
            scenarios[selectedScenario]?.OnStandardTrainingScenarioSelected?.Invoke();
    }

    private void ReturnToScenarioDetails()
    {
        Debug.Log("ScenarioDetailModal training choice back button selected.", this);
        if (scenarios == null || selectedScenario < 0 || selectedScenario >= scenarios.Length)
            return;

        ScenarioDetail detail = scenarios[selectedScenario];
        if (detail == null)
            return;

        ShowScenarioContent(selectedScenario);
        if (!HasScenarioContentObjects())
        {
            if (titleText != null) titleText.text = detail.Title;
            if (descriptionText != null) descriptionText.text = detail.Description;
        }
        ShowPrimaryActions(detail);
        trainingButton?.Select();
    }

    private void ShowScenarioContent(int scenarioIndex)
    {
        SetOnlyActive(scenarioTitleObjects, scenarioIndex);
        SetOnlyActive(scenarioDescriptionObjects, scenarioIndex);
    }

    private bool HasScenarioContentObjects()
    {
        return HasAnyAssigned(scenarioTitleObjects) || HasAnyAssigned(scenarioDescriptionObjects);
    }

    private static bool HasAnyAssigned(GameObject[] objects)
    {
        if (objects == null)
            return false;

        foreach (GameObject target in objects)
            if (target != null)
                return true;

        return false;
    }

    private static void SetOnlyActive(GameObject[] objects, int activeIndex)
    {
        if (objects == null)
            return;

        for (int i = 0; i < objects.Length; i++)
            if (objects[i] != null)
                objects[i].SetActive(i == activeIndex);
    }

    private void ShowPrimaryActions(ScenarioDetail detail)
    {
        SetButtonVisible(incompletePpeButton, false);
        SetButtonVisible(standardTrainingButton, false);
        SetButtonVisible(trainingChoiceBackButton, false);
        SetButtonVisible(trainingButton, true);
        SetButtonVisible(backButton, true);
        if (trainingButtonText != null)
            trainingButtonText.text = detail.TrainingButtonLabel;
    }

    private static void SetButtonVisible(Button button, bool visible)
    {
        if (button == null)
            return;

        button.gameObject.SetActive(visible);
        if (!visible)
            return;

        foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
            label.gameObject.SetActive(true);
    }

    private void SetSelectionListeners(bool add)
    {
        if (!add)
        {
            foreach ((Button button, UnityAction action) in selectionListeners)
                if (button != null)
                    button.onClick.RemoveListener(action);
            selectionListeners.Clear();
            return;
        }

        if (scenarios == null)
            return;

        for (int scenarioIndex = 0; scenarioIndex < scenarios.Length; scenarioIndex++)
        {
            ScenarioDetail detail = scenarios[scenarioIndex];
            if (detail?.SelectionButtons == null)
                continue;

            int capturedIndex = scenarioIndex;
            foreach (Button button in detail.SelectionButtons)
            {
                if (button == null)
                    continue;

                UnityAction listener = () => Show(capturedIndex);
                button.onClick.AddListener(listener);
                selectionListeners.Add((button, listener));
            }
        }
    }

    private bool TryShowFromMouseRaycast()
    {
        Camera raycastCamera = ResolveMouseRaycastCamera();
        if (raycastCamera == null || scenarios == null)
            return false;

        Ray ray = raycastCamera.ScreenPointToRay(GetMousePosition());
        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, mouseRaycastMask, QueryTriggerInteraction.Collide))
            return false;

        Transform hitTransform = hit.transform;
        for (int scenarioIndex = 0; scenarioIndex < scenarios.Length; scenarioIndex++)
        {
            ScenarioDetail detail = scenarios[scenarioIndex];
            if (detail?.SelectionButtons == null)
                continue;

            foreach (Button button in detail.SelectionButtons)
            {
                if (button == null)
                    continue;

                if (MatchesSelectionTarget(hitTransform, button.transform))
                {
                    Show(scenarioIndex);
                    Debug.Log($"ScenarioDetailModal mouse physics selected scenario {scenarioIndex + 1} via {hitTransform.name}.", hit.collider);
                    return true;
                }
            }
        }

        Debug.Log($"ScenarioDetailModal mouse physics hit {hitTransform.name}, but it is not registered as a scenario selection button.", hit.collider);
        return false;
    }

    private static bool MatchesSelectionTarget(Transform hitTransform, Transform selectionTransform)
    {
        if (hitTransform == selectionTransform || hitTransform.IsChildOf(selectionTransform) || selectionTransform.IsChildOf(hitTransform))
            return true;

        if (hitTransform.parent != selectionTransform.parent)
            return false;

        if (hitTransform is not RectTransform hitRect || selectionTransform is not RectTransform selectionRect)
            return false;

        return Vector2.SqrMagnitude(hitRect.anchoredPosition - selectionRect.anchoredPosition) < 0.01f;
    }

    private Camera ResolveMouseRaycastCamera()
    {
        if (mouseRaycastCamera != null)
            return mouseRaycastCamera;

        if (ownerCanvas != null && ownerCanvas.worldCamera != null)
            return ownerCanvas.worldCamera;

        return Camera.main;
    }

    private void LogModalTransformDiagnostics()
    {
        if (modalRoot == null)
            return;

        RectTransform rectTransform = modalRoot.transform as RectTransform;
        Canvas parentCanvas = modalRoot.GetComponentInParent<Canvas>(true);
        string rectInfo = rectTransform == null
            ? "no RectTransform"
            : $"anchored={rectTransform.anchoredPosition}, local={rectTransform.localPosition}, world={rectTransform.position}, scale={rectTransform.lossyScale}, size={rectTransform.rect.size}";

        string canvasInfo = parentCanvas == null
            ? "no parent Canvas"
            : $"canvas={parentCanvas.name}, sortingOrder={parentCanvas.sortingOrder}, overrideSorting={parentCanvas.overrideSorting}, worldCamera={parentCanvas.worldCamera?.name ?? "null"}";

        Debug.Log($"ScenarioDetailModal modal diagnostics: active={modalRoot.activeInHierarchy}, {rectInfo}, {canvasInfo}", modalRoot);
    }

    private void SetModalHierarchyActive(bool visible)
    {
        Canvas parentCanvas = modalRoot != null ? modalRoot.GetComponentInParent<Canvas>(true) : null;
        if (parentCanvas != null)
            parentCanvas.gameObject.SetActive(visible);

        if (modalRoot != null)
            modalRoot.SetActive(visible);
    }

    private void LogPointerRaycastDiagnostics()
    {
        if (EventSystem.current == null)
        {
            Debug.LogWarning("ScenarioDetailModal pointer diagnostics: no active EventSystem.", this);
            return;
        }

        PointerEventData pointerEventData = new(EventSystem.current)
        {
            position = GetMousePosition()
        };
        List<RaycastResult> results = new();
        EventSystem.current.RaycastAll(pointerEventData, results);

        if (results.Count == 0)
        {
            Debug.Log("ScenarioDetailModal pointer diagnostics: no UI raycast hits.", this);
            return;
        }

        int count = Mathf.Min(results.Count, 5);
        for (int i = 0; i < count; i++)
        {
            RaycastResult result = results[i];
            Debug.Log($"ScenarioDetailModal pointer diagnostics hit {i}: {result.gameObject.name}", result.gameObject);
        }
    }

    private static bool WasMousePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(0);
#else
        return false;
#endif
    }

    private static Vector2 GetMousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
#else
        return Vector2.zero;
#endif
    }
}
