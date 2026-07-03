using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public sealed class ScenarioSelectionHud : MonoBehaviour
{
    [Serializable]
    public sealed class ScenarioOption
    {
        public string title = "Scenario";
        public UnityEvent onSelected = new();
    }

    [SerializeField] private ScenarioOption[] options =
    {
        new() { title = "Scenario 1" },
        new() { title = "Scenario 2" },
        new() { title = "Scenario 3" }
    };

    private const string HudRootName = "Scenario Selection HUD";

    private void OnEnable()
    {
        BindButtons();
    }

    private void BindButtons()
    {
        Transform root = transform.Find(HudRootName);
        if (root == null)
            return;

        for (int i = 0; i < 3; i++)
        {
            Transform card = root.Find($"Scenario Card {i + 1}");
            if (card == null || !card.TryGetComponent(out Button button))
                continue;

            int capturedIndex = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectScenario(capturedIndex));
        }
    }

    private void SelectScenario(int index)
    {
        if (options == null || index < 0 || index >= options.Length || options[index] == null)
            return;

        Debug.Log($"Selected {options[index].title}", this);
        options[index].onSelected?.Invoke();
    }
}
