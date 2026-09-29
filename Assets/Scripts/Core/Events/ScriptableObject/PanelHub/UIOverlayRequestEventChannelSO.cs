/*
 * Role hierarchy: Core > Events > ScriptableObject > PanelHub
 * Primary responsibility: Implements the focused UIOverlayRequestType responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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

    // Purpose: Opens the open UI or workflow.
    public void Open(UIOverlayId id)
    {
        RaiseEvent(new UIOverlayRequest(id, UIOverlayRequestType.Open));
    }

    // Purpose: Closes the active close UI or workflow.
    public void Close(UIOverlayId id)
    {
        RaiseEvent(new UIOverlayRequest(id, UIOverlayRequestType.Close));
    }

    // Purpose: Toggles the toggle state.
    public void Toggle(UIOverlayId id)
    {
        RaiseEvent(new UIOverlayRequest(id, UIOverlayRequestType.Toggle));
    }

    // Purpose: Closes the active all UI or workflow.
    public void CloseAll()
    {
        RaiseEvent(new UIOverlayRequest(UIOverlayId.None, UIOverlayRequestType.CloseAll));
    }

    // Purpose: Publishes the event notification.
    public void RaiseEvent(UIOverlayRequest request)
    {
        OnEventRaised?.Invoke(request);
    }
}
