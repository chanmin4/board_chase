using System;
using UnityEngine;

public readonly struct InteractionPromptSnapshot
{
    public readonly bool Visible;
    public readonly InteractionType InteractionType;
    public readonly Transform Anchor;
    public readonly string KeyLabel;
    public readonly string ActionLabel;

    public InteractionPromptSnapshot(
        bool visible,
        InteractionType interactionType,
        Transform anchor,
        string keyLabel,
        string actionLabel)
    {
        Visible = visible;
        InteractionType = interactionType;
        Anchor = anchor;
        KeyLabel = keyLabel;
        ActionLabel = actionLabel;
    }

    public static InteractionPromptSnapshot Hidden =>
        new InteractionPromptSnapshot(false, InteractionType.None, null, string.Empty, string.Empty);
}

[CreateAssetMenu(
    fileName = "InteractionPromptEventChannel",
    menuName = "Events/UI/Interaction Prompt Event Channel")]
public class InteractionPromptEventChannelSO : ScriptableObject
{
    public event Action<InteractionPromptSnapshot> OnEventRaised;

    public void RaiseEvent(InteractionPromptSnapshot snapshot)
    {
        OnEventRaised?.Invoke(snapshot);
    }

    public void Clear()
    {
        RaiseEvent(InteractionPromptSnapshot.Hidden);
    }
}
