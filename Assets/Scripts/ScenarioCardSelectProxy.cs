using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[DisallowMultipleComponent]
public sealed class ScenarioCardSelectProxy : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private ScenarioDetailModal modal;
    [SerializeField, Min(0)] private int scenarioIndex;

    private XRSimpleInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        if (interactable == null)
            interactable = gameObject.AddComponent<XRSimpleInteractable>();

        interactable.selectEntered.AddListener(OnSelectEntered);
    }

    private void OnDestroy()
    {
        if (interactable != null)
            interactable.selectEntered.RemoveListener(OnSelectEntered);
    }

    public void Trigger()
    {
        if (modal == null)
            return;

        modal.Show(scenarioIndex);
    }

    private void OnMouseDown()
    {
        Trigger();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Trigger();
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        Trigger();
    }
}
