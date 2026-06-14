using System;
using UnityEngine;

public enum UIOverlayRequestType
{
    Open = 0,
    Close = 1,
    Toggle = 2,
    CloseAll = 3
}

[Serializable]
public struct UIOverlayRequest
{
    public UIOverlayId overlayId;
    public UIOverlayRequestType requestType;

    public UIOverlayRequest(UIOverlayId overlayId, UIOverlayRequestType requestType)
    {
        this.overlayId = overlayId;
        this.requestType = requestType;
    }
}

[CreateAssetMenu(
    fileName = "UIOverlayRequestEventChannel",
    menuName = "Events/UI/UI Overlay Request Event Channel")]
public class UIOverlayRequestEventChannelSO : ScriptableObject
{
    public event Action<UIOverlayRequest> OnEventRaised;

    public void Open(UIOverlayId id)
    {
        RaiseEvent(new UIOverlayRequest(id, UIOverlayRequestType.Open));
    }

    public void Close(UIOverlayId id)
    {
        RaiseEvent(new UIOverlayRequest(id, UIOverlayRequestType.Close));
    }

    public void Toggle(UIOverlayId id)
    {
        RaiseEvent(new UIOverlayRequest(id, UIOverlayRequestType.Toggle));
    }

    public void CloseAll()
    {
        RaiseEvent(new UIOverlayRequest(UIOverlayId.None, UIOverlayRequestType.CloseAll));
    }

    public void RaiseEvent(UIOverlayRequest request)
    {
        OnEventRaised?.Invoke(request);
    }
}
