using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

[DisallowMultipleComponent]
public sealed class ScenarioCardSelectProxy : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private ScenarioDetailModal modal;
    [SerializeField, Min(0)] private int scenarioIndex;
    [SerializeField] private GameObject hideAfterSelection;

    public void Trigger()
    {
        if (hideAfterSelection != null)
        {
            hideAfterSelection.SetActive(false);
            return;
        }

        if (modal == null)
            return;

        modal.Show(scenarioIndex);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData is not TrackedDeviceEventData trackedEventData
            || trackedEventData.interactor is not NearFarInteractor
            || trackedEventData.rayHitIndex <= 0
            || trackedEventData.rayPoints == null
            || trackedEventData.rayPoints.Count < 2)
        {
            return;
        }

        Trigger();
    }
}
