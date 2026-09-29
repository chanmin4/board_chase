/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Handles UIAudioCueType audio playback and sound-stimulus delivery.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

public enum UIAudioCueType
{
    PanelOpen = 0,
    PanelClose = 1,
    ButtonClick = 2,
    Denied = 3,
    InventoryMove = 4,
    Equip = 5,
    Unequip = 6,
    Split = 7,
    Buy = 8,
    Sell = 9,
    Repair = 10,
    Navigate = 11,
    Cancel = 12,
    QuestAccepted = 13,
    QuestCompleted = 14
}

[CreateAssetMenu(
    fileName = "UIAudioEventChannel",
    menuName = "Events/Audio/UI Audio Event Channel")]
public class UIAudioEventChannelSO : DescriptionBaseSO
{
    public event Action<UIAudioCueType> OnEventRaised;

    // Purpose: Publishes the event notification.
    public void RaiseEvent(UIAudioCueType cueType)
    {
        OnEventRaised?.Invoke(cueType);
    }
}
