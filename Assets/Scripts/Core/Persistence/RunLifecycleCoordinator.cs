/*
 * Role hierarchy: Core > Persistence
 * Primary responsibility: Owns run lifecycle transitions and the persistent-profile save boundary.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

public enum RunLifecyclePhase
{
    Booting,
    Home,
    RunActive,
    RunSuspended,
    RunCompleted,
    GameOver
}

/// <summary>
/// PersistentManager-owned coordinator for boundaries that affect an entire run.
/// It intentionally owns policy and ordering only: systems such as inventory,
/// shop and stage flow remain the owners of their individual runtime data.
/// </summary>
[DefaultExecutionOrder(-10000)]
[DisallowMultipleComponent]
public sealed class RunLifecycleCoordinator : MonoBehaviour
{
    [Header("Persistent Profile")]
    [Tooltip("The single persistent profile. It is loaded once at startup and flushed at safe lifecycle boundaries.")]
    [SerializeField] private SaveSystem _saveSystem;

    [Header("Resumable Player Run")]
    [Tooltip("Build-safe lookup used to restore ItemSO and status-effect references from the ES3 player-run snapshot.")]
    [SerializeField] private RuntimeContentCatalogSO _runtimeContentCatalog;

    [Tooltip("Runtime bridge that captures an alive player at save boundaries and restores it after the Player prefab is ready.")]
    [SerializeField] private PlayerRunStatePersistence _playerRunStatePersistence;

    [Header("Scene Dependency Validation")]
    [Tooltip("Persistent runtime guard for the Home/RUN additive scene contract. It reports missing or duplicate services but never creates gameplay objects.")]
    [SerializeField] private RuntimeSceneDependencyValidator _sceneDependencyValidator;

    [SerializeField] private bool _savePersistentProfileOnApplicationPause = true;
    [SerializeField] private bool _savePersistentProfileOnApplicationQuit = true;

    [Header("Run Loss Policy")]
    [Tooltip("Run inventory, quickslots and virtual magazines are owned by PlayerInventoryRuntime and are cleared on death.")]
    [SerializeField] private bool _clearPlayerInventoryOnRunLoss = true;

    [Tooltip("The Home shop's run-scoped purchase escalation is reset on death.")]
    [SerializeField] private bool _resetShopRunRuntimeOnRunLoss = true;

    [Header("Runtime State (Read Only)")]
    [SerializeField] private RunLifecyclePhase _phase = RunLifecyclePhase.Booting;
    [SerializeField] private RunLifecyclePhase _phaseBeforeSuspend = RunLifecyclePhase.Booting;
    [SerializeField] private int _activeStageIndex = -1;
    [SerializeField] private string _lastTransitionReason = "Not initialized";

    private bool _suppressExternalRunStateCapture;

    public static RunLifecycleCoordinator Instance { get; private set; }
    public RunLifecyclePhase Phase => _phase;
    public int ActiveStageIndex => _activeStageIndex;
    public string LastTransitionReason => _lastTransitionReason;
    public RuntimeSceneDependencyValidator SceneDependencyValidator => _sceneDependencyValidator;
    public event Action<RunLifecyclePhase, RunLifecyclePhase, string> Transitioned;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("[RunLifecycle] Multiple coordinators were created. Keeping the first PersistentManager instance.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureSceneDependencyValidator();
        EnsurePersistentProfileLoaded();
        EnsurePlayerRunStatePersistence();
        _playerRunStatePersistence.Initialize(ResolveSaveSystem(), _runtimeContentCatalog);
    }

    // Purpose: Gives the run-state bridge a chance to restore only after the scene-created player is ready.
    private void Update()
    {
        _playerRunStatePersistence?.TryRestorePendingRunState();
        ResolveSaveSystem()?.FlushPendingWrites();
    }

    // Purpose: Releases owned resources before this component is destroyed.
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // Purpose: Responds to the application pause callback.
    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            _phaseBeforeSuspend = _phase;
            TransitionTo(
                RunLifecyclePolicy.ResolveApplicationPausePhase(_phase, true, _phaseBeforeSuspend),
                "Application paused");

            if (_savePersistentProfileOnApplicationPause)
                SavePersistentProfile("Application paused");

            return;
        }

        TransitionTo(
            RunLifecyclePolicy.ResolveApplicationPausePhase(_phase, false, _phaseBeforeSuspend),
            "Application resumed");
    }

    // Purpose: Responds to the application quit callback.
    private void OnApplicationQuit()
    {
        if (_savePersistentProfileOnApplicationQuit)
            SavePersistentProfile("Application quit");
    }

    // Purpose: Publishes the home entered notification.
    public static void NotifyHomeEntered()
    {
        Instance?.HandleHomeEntered();
    }

    // Purpose: Publishes the run started notification.
    public static void NotifyRunStarted(int stageIndex)
    {
        Instance?.HandleRunStarted(stageIndex);
    }

    // Purpose: Publishes the run completed notification.
    public static void NotifyRunCompleted(int stageIndex)
    {
        Instance?.HandleRunCompleted(stageIndex);
    }

    /// <summary>
    /// Returns false only when PersistentManager is absent, so isolated test
    /// scenes can keep their former local death fallback intact.
    /// </summary>
    public static bool TryHandlePlayerRunLoss(PlayerInventoryRuntime playerInventory)
    {
        if (Instance == null)
            return false;

        Instance.HandlePlayerRunLoss(playerInventory);
        return true;
    }

    /// <summary>
    /// SaveSystem calls this before any direct ES3 profile write initiated by
    /// quests, shop unlocks, options, or meta progression. The coordinator is
    /// the sole policy owner, so death saves can suppress this capture after
    /// deliberately clearing the run snapshot.
    /// </summary>
    public static void CaptureRunStateForExternalSave(SaveSystem saveSystem)
    {
        if (Instance == null || saveSystem == null || Instance._suppressExternalRunStateCapture)
            return;

        if (Instance.ResolveSaveSystem() != saveSystem)
            return;

        Instance._playerRunStatePersistence?.CaptureCurrentPlayerState();
    }

    // Purpose: Handles the incoming home entered request or event.
    private void HandleHomeEntered()
    {
        _activeStageIndex = -1;
        TransitionTo(RunLifecyclePhase.Home, "Entered Home");
        SavePersistentProfile("Entered Home");
    }

    // Purpose: Handles the incoming run started request or event.
    private void HandleRunStarted(int stageIndex)
    {
        GameProgressionRuntime.BeginRun();
        _activeStageIndex = RunLifecyclePolicy.NormalizeActiveStageIndex(stageIndex);
        TransitionTo(RunLifecyclePhase.RunActive, $"Started Stage {_activeStageIndex}");
    }

    // Purpose: Handles the incoming run completed request or event.
    private void HandleRunCompleted(int stageIndex)
    {
        _activeStageIndex = RunLifecyclePolicy.NormalizeActiveStageIndex(stageIndex);
        TransitionTo(RunLifecyclePhase.RunCompleted, $"Completed Stage {_activeStageIndex}");
        SavePersistentProfile("Run completed");
    }

    // Purpose: Handles the incoming player run loss request or event.
    private void HandlePlayerRunLoss(PlayerInventoryRuntime playerInventory)
    {
        if (!RunLifecyclePolicy.CanProcessRunLoss(_phase))
            return;

        GameProgressionRuntime.Settle(false);

        if (_clearPlayerInventoryOnRunLoss)
        {
            if (playerInventory != null)
            {
                playerInventory.ClearForRunLoss();
            }
            else
            {
                Debug.LogError("[RunLifecycle] PlayerInventoryRuntime is missing, so run possessions could not be cleared.", this);
            }
        }

        if (_resetShopRunRuntimeOnRunLoss)
            ShopRunRuntime.EnsureRuntime().ResetForRunLoss();

        _playerRunStatePersistence?.ClearRunState();

        _activeStageIndex = -1;
        TransitionTo(RunLifecyclePhase.GameOver, "Player died");
        SavePersistentProfile("Player died", capturePlayerRunState: false);
    }

    // Purpose: Ensures persistent profile loaded is ready before the workflow continues.
    private void EnsurePersistentProfileLoaded()
    {
        SaveSystem saveSystem = ResolveSaveSystem();
        if (saveSystem == null)
        {
            Debug.LogError("[RunLifecycle] SaveSystem is missing on PersistentManager.", this);
            return;
        }

        saveSystem.LoadSaveDataFromDisk();
    }

    // Purpose: Saves the current persistent profile state.
    private void SavePersistentProfile(string reason, bool capturePlayerRunState = true)
    {
        SaveSystem saveSystem = ResolveSaveSystem();
        if (saveSystem == null)
        {
            Debug.LogError($"[RunLifecycle] SaveSystem is missing. Persistent profile was not saved. reason={reason}", this);
            return;
        }

        bool previousSuppressState = _suppressExternalRunStateCapture;
        _suppressExternalRunStateCapture = true;

        try
        {
            if (capturePlayerRunState)
                _playerRunStatePersistence?.CaptureCurrentPlayerState();

            saveSystem.SaveDataToDisk();
        }
        finally
        {
            _suppressExternalRunStateCapture = previousSuppressState;
        }
    }

    // Purpose: Resolves the save system needed by this component.
    private SaveSystem ResolveSaveSystem()
    {
        return _saveSystem != null ? _saveSystem : SaveSystem.Active;
    }

    // Purpose: Ensures scene dependency validator is ready before the workflow continues.
    private void EnsureSceneDependencyValidator()
    {
        if (_sceneDependencyValidator == null)
            _sceneDependencyValidator = GetComponent<RuntimeSceneDependencyValidator>();

        if (_sceneDependencyValidator == null)
            _sceneDependencyValidator = gameObject.AddComponent<RuntimeSceneDependencyValidator>();
    }

    // Purpose: Ensures resumable player-run persistence is attached to PersistentManager.
    private void EnsurePlayerRunStatePersistence()
    {
        if (_playerRunStatePersistence == null)
            _playerRunStatePersistence = GetComponent<PlayerRunStatePersistence>();

        if (_playerRunStatePersistence == null)
            _playerRunStatePersistence = gameObject.AddComponent<PlayerRunStatePersistence>();
    }

    // Purpose: Performs the transition to workflow for this component.
    private void TransitionTo(RunLifecyclePhase nextPhase, string reason)
    {
        RunLifecyclePhase previousPhase = _phase;
        _phase = nextPhase;
        _lastTransitionReason = string.IsNullOrWhiteSpace(reason) ? "Unspecified" : reason;

        if (previousPhase == nextPhase)
            return;

        Debug.Log($"[RunLifecycle] {previousPhase} -> {nextPhase}. reason={_lastTransitionReason}", this);
        Transitioned?.Invoke(previousPhase, nextPhase, _lastTransitionReason);
    }
}
