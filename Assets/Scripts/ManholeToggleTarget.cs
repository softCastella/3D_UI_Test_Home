using UnityEngine;

[DisallowMultipleComponent]
public sealed class ManholeToggleTarget : MonoBehaviour
{
    public MixerManholeController controller;

    void OnMouseDown()
    {
        controller?.Toggle();
    }

    public void Toggle()
    {
        controller?.Toggle();
    }
}
