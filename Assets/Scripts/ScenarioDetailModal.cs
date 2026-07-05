using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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

    private int selectedScenario = -1;
    private readonly List<(Button button, UnityAction action)> selectionListeners = new();

    private void OnEnable()
    {
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
        if (numberText != null) numberText.text = detail.Number;
        if (titleText != null) titleText.text = detail.Title;
        if (descriptionText != null) descriptionText.text = detail.Description;
        if (trainingButtonText != null) trainingButtonText.text = detail.TrainingButtonLabel;

        modalRoot.SetActive(true);
        modalRoot.transform.SetAsLastSibling();
        trainingButton?.Select();
    }

    public void Hide()
    {
        selectedScenario = -1;
        if (modalRoot != null)
            modalRoot.SetActive(false);
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

        if (titleText != null) titleText.text = trainingChoiceTitle;
        if (descriptionText != null) descriptionText.text = trainingChoiceDescription;
        trainingButton?.gameObject.SetActive(false);
        backButton?.gameObject.SetActive(false);
        incompletePpeButton?.gameObject.SetActive(true);
        standardTrainingButton?.gameObject.SetActive(true);
        trainingChoiceBackButton?.gameObject.SetActive(true);
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
        if (selectedScenario >= 0 && selectedScenario < scenarios.Length)
            scenarios[selectedScenario]?.OnStandardTrainingScenarioSelected?.Invoke();
    }

    private void ReturnToScenarioDetails()
    {
        if (scenarios == null || selectedScenario < 0 || selectedScenario >= scenarios.Length)
            return;

        ScenarioDetail detail = scenarios[selectedScenario];
        if (detail == null)
            return;

        if (titleText != null) titleText.text = detail.Title;
        if (descriptionText != null) descriptionText.text = detail.Description;
        ShowPrimaryActions(detail);
        trainingButton?.Select();
    }

    private void ShowPrimaryActions(ScenarioDetail detail)
    {
        incompletePpeButton?.gameObject.SetActive(false);
        standardTrainingButton?.gameObject.SetActive(false);
        trainingChoiceBackButton?.gameObject.SetActive(false);
        trainingButton?.gameObject.SetActive(true);
        backButton?.gameObject.SetActive(true);
        if (trainingButtonText != null)
            trainingButtonText.text = detail.TrainingButtonLabel;
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
}
