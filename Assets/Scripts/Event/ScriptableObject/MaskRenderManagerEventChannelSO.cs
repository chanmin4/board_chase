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

    public void RaiseEvent(MaskRenderManager manager)
    {
        _current = manager;
        OnEventRaised?.Invoke(manager);
    }

    public void Clear(MaskRenderManager manager)
    {
        if (_current != manager)
            return;

        _current = null;
        OnEventRaised?.Invoke(null);
    }
}
