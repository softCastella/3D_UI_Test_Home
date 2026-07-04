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

        public Button[] SelectionButtons => selectionButtons;
        public string Number => number;
        public string Title => title;
        public string Description => description;
        public string TrainingButtonLabel => trainingButtonLabel;
        public UnityEvent OnTrainingSelected => onTrainingSelected;
    }

    [Header("Scene References")]
    [SerializeField] private GameObject modalRoot;
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text trainingButtonText;
    [SerializeField] private Button trainingButton;
    [SerializeField] private Button backButton;

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
    }

    private void OnDisable()
    {
        SetSelectionListeners(false);
        if (trainingButton != null)
            trainingButton.onClick.RemoveListener(SelectTraining);
        if (backButton != null)
            backButton.onClick.RemoveListener(Hide);
    }

    public void Show(int scenarioIndex)
    {
        if (scenarios == null || scenarioIndex < 0 || scenarioIndex >= scenarios.Length)
            return;

        ScenarioDetail detail = scenarios[scenarioIndex];
        if (detail == null || modalRoot == null)
            return;

        selectedScenario = scenarioIndex;
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

        scenarios[selectedScenario]?.OnTrainingSelected?.Invoke();
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
