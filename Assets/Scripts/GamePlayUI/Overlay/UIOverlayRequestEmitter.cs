using UnityEngine;

[DisallowMultipleComponent]
public class UIOverlayRequestEmitter : MonoBehaviour
{
    [SerializeField] private UIOverlayRequestEventChannelSO _requestChannel;
    [SerializeField] private UIOverlayId _overlayId;
    [SerializeField] private UIOverlayRequestType _requestType = UIOverlayRequestType.Toggle;

    public void Raise()
    {
        if (_requestChannel == null)
            return;

        _requestChannel.RaiseEvent(new UIOverlayRequest(_overlayId, _requestType));
    }

    public void Open()
    {
        if (_requestChannel != null)
            _requestChannel.Open(_overlayId);
    }

    public void Close()
    {
        if (_requestChannel != null)
            _requestChannel.Close(_overlayId);
    }

    public void Toggle()
    {
        if (_requestChannel != null)
            _requestChannel.Toggle(_overlayId);
    }

    public void CloseAll()
    {
        if (_requestChannel != null)
            _requestChannel.CloseAll();
    }
}
