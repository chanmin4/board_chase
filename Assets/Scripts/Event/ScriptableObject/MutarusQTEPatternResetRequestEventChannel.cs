using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "MutarusQTEPatternResetRequestEventChannel",
    menuName = "Events/Named Enemy/Mutarus QTE Pattern Reset Request Event Channel")]
public class MutarusQTEPatternResetRequestEventChannelSO : ScriptableObject
{
    public event Action OnEventRaised;

    public void RaiseEvent()
    {
        OnEventRaised?.Invoke();
    }
}
