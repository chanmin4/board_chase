/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Defines the MaskRenderManagerEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "MaskRenderManagerEventChannel",
    menuName = "Events/Mask/Mask Render Manager Event Channel")]
public class MaskRenderManagerEventChannelSO : ScriptableObject
{
    public event Action<MaskRenderManager> OnEventRaised;

    [NonSerialized] private MaskRenderManager _current;

    public MaskRenderManager Current => _current;

    // Purpose: Publishes the event notification.
    public void RaiseEvent(MaskRenderManager manager)
    {
        _current = manager;
        OnEventRaised?.Invoke(manager);
    }

    // Purpose: Clears the current clear state.
    public void Clear(MaskRenderManager manager)
    {
        if (_current != manager)
            return;

        _current = null;
        OnEventRaised?.Invoke(null);
    }
}
