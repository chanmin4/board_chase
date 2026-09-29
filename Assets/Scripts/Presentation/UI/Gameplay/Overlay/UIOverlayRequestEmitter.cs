/*
 * Role hierarchy: Presentation > UI > Gameplay > Overlay
 * Primary responsibility: Implements the focused UIOverlayRequestEmitter responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public class UIOverlayRequestEmitter : MonoBehaviour
{
    [SerializeField] private UIOverlayRequestEventChannelSO _requestChannel;
    [SerializeField] private UIOverlayId _overlayId;
    [SerializeField] private UIOverlayRequestType _requestType = UIOverlayRequestType.Toggle;

    // Purpose: Publishes the raise notification.
    public void Raise()
    {
        if (_requestChannel == null)
            return;

        _requestChannel.RaiseEvent(new UIOverlayRequest(_overlayId, _requestType));
    }

    // Purpose: Opens the open UI or workflow.
    public void Open()
    {
        if (_requestChannel != null)
            _requestChannel.Open(_overlayId);
    }

    // Purpose: Closes the active close UI or workflow.
    public void Close()
    {
        if (_requestChannel != null)
            _requestChannel.Close(_overlayId);
    }

    // Purpose: Toggles the toggle state.
    public void Toggle()
    {
        if (_requestChannel != null)
            _requestChannel.Toggle(_overlayId);
    }

    // Purpose: Closes the active all UI or workflow.
    public void CloseAll()
    {
        if (_requestChannel != null)
            _requestChannel.CloseAll();
    }
}
