using UnityEngine;

[CreateAssetMenu(
    fileName = "NamedSectorTiming",
    menuName = "Game/Sector/Named Sector Timing")]
public class NamedSectorTimingSO : ScriptableObject
{
    [Header("Boot")]
    [Tooltip("Start named sector cycle when SectorStateManager is ready.")]
    [SerializeField] private bool _startOnReady = true;

    [Tooltip("If true, first named sector is reserved immediately. If false, First Reservation Delay is used.")]
    [SerializeField] private bool _reserveFirstSectorImmediately = true;

    [Tooltip("Only used when Reserve First Sector Immediately is false.")]
    [SerializeField, Min(0f)] private float _firstReservationDelay = 0f;

    [Header("Cycle")]
    [Tooltip("How long a selected sector stays reserved before named becomes present.")]
    [SerializeField, Min(0f)] private float _reservationDuration = 30f;

    [Tooltip("Delay after named is killed before the next random sector is reserved.")]
    [SerializeField, Min(0f)] private float _respawnCooldownAfterKill = 120f;

    [Tooltip("Retry delay when no valid opened sector can be selected.")]
    [SerializeField, Min(0f)] private float _retryDelayWhenNoCandidate = 5f;

    [Header("Publish")]
    [Tooltip("How often timer snapshot is sent to UI.")]
    [SerializeField, Min(0.01f)] private float _timerPublishInterval = 0.1f;

    [Header("Debug")]
    [SerializeField, Min(0.1f)] private float _debugLogInterval = 1f;

    public bool StartOnReady => _startOnReady;
    public bool ReserveFirstSectorImmediately => _reserveFirstSectorImmediately;
    public float FirstReservationDelay => _firstReservationDelay;
    public float ReservationDuration => _reservationDuration;
    public float RespawnCooldownAfterKill => _respawnCooldownAfterKill;
    public float RetryDelayWhenNoCandidate => _retryDelayWhenNoCandidate;
    public float TimerPublishInterval => _timerPublishInterval;
    public float DebugLogInterval => _debugLogInterval;
}
