using System;
using UnityEngine;

public enum NamedSectorPhase
{
    None,
    WaitingForReservation,
    Reserved,
    Present,
    EnteringBattle,
    Battle,
    RewardPending,
    EndingBattle,
    DefeatedCooldown
}

public readonly struct NamedSectorPhaseChange
{
    public readonly NamedSectorPhase Phase;
    public readonly SectorRuntime Sector;

    public NamedSectorPhaseChange(NamedSectorPhase phase, SectorRuntime sector)
    {
        Phase = phase;
        Sector = sector;
    }
}

[CreateAssetMenu(
    fileName = "NamedSectorPhaseEventChannel",
    menuName = "Events/Named Sector Phase Event Channel")]
public class NamedSectorPhaseEventChannelSO : ScriptableObject
{
    public event Action<NamedSectorPhaseChange> OnEventRaised;

    public void RaiseEvent(NamedSectorPhase phase, SectorRuntime sector)
    {
        OnEventRaised?.Invoke(new NamedSectorPhaseChange(phase, sector));
    }
}
