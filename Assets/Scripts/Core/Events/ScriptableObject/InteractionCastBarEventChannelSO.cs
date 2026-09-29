/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Handles InteractionCastBarSnapshot interaction, pickup, trade, loot, or storage flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

public readonly struct InteractionCastBarSnapshot
{
    public readonly bool Visible;
    public readonly InteractionType InteractionType;
    public readonly Transform Actor;
    public readonly Transform Anchor;
    public readonly float Progress01;
    public readonly float DurationSeconds;

    public InteractionCastBarSnapshot(
        bool visible,
        InteractionType interactionType,
        Transform actor,
        Transform anchor,
        float progress01,
        float durationSeconds)
    {
        Visible = visible;
        InteractionType = interactionType;
        Actor = actor;
        Anchor = anchor;
        Progress01 = Mathf.Clamp01(progress01);
        DurationSeconds = Mathf.Max(0f, durationSeconds);
    }

    public static InteractionCastBarSnapshot Hidden =>
        new InteractionCastBarSnapshot(
            false,
            InteractionType.None,
            null,
            null,
            0f,
            0f);
}

[CreateAssetMenu(
    fileName = "InteractionCastBarEventChannel",
    menuName = "Events/UI/Interaction Cast Bar Event Channel")]
public class InteractionCastBarEventChannelSO : ScriptableObject
{
    public event Action<InteractionCastBarSnapshot> OnEventRaised;

    // Purpose: Publishes the event notification.
    public void RaiseEvent(InteractionCastBarSnapshot snapshot)
    {
        OnEventRaised?.Invoke(snapshot);
    }

    // Purpose: Clears the current clear state.
    public void Clear()
    {
        RaiseEvent(InteractionCastBarSnapshot.Hidden);
    }
}
