/*
 * Role hierarchy: Core > SceneFlow
 * Primary responsibility: Validates the critical persistent, Home, and RUN scene runtime dependencies after scene changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum RuntimeDependencySeverity
{
    Info = 0,
    Warning = 1,
    Error = 2
}

public readonly struct RuntimeDependencyIssue
{
    public RuntimeDependencySeverity Severity { get; }
    public string Message { get; }

    public RuntimeDependencyIssue(RuntimeDependencySeverity severity, string message)
    {
        Severity = severity;
        Message = message ?? string.Empty;
    }
}

/// <summary>
/// A lightweight runtime guard for the additive Home/RUN architecture. It does
/// not create or repair gameplay objects: it reports only the missing or
/// duplicated dependencies that would make a scene transition unsafe.
/// </summary>
[DefaultExecutionOrder(-9900)]
[DisallowMultipleComponent]
public sealed class RuntimeSceneDependencyValidator : MonoBehaviour
{
    [Header("Expected Scene Names")]
    [SerializeField] private string _homeSceneName = "HomeScene";
    [SerializeField] private string _runSceneName = "RUNScene";

    [Header("Runtime Report (Read Only)")]
    [ReadOnly] [SerializeField] private int _lastErrorCount;
    [ReadOnly] [SerializeField] private int _lastWarningCount;
    [ReadOnly] [SerializeField] private string _lastValidationSummary = "Not validated yet.";
    [ReadOnly] [TextArea(5, 18)] [SerializeField] private string _lastValidationReport = "";

    private readonly List<RuntimeDependencyIssue> _issues = new();
    private Coroutine _queuedValidation;
    private string _lastLoggedSignature;

    public IReadOnlyList<RuntimeDependencyIssue> Issues => _issues;
    public int LastErrorCount => _lastErrorCount;
    public int LastWarningCount => _lastWarningCount;
    public string LastValidationSummary => _lastValidationSummary;
    public string LastValidationReport => _lastValidationReport;
    public bool HasErrors => _lastErrorCount > 0;

    public event Action<RuntimeSceneDependencyValidator> Validated;

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        SceneManager.sceneUnloaded += HandleSceneUnloaded;
        QueueValidation();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneUnloaded -= HandleSceneUnloaded;

        if (_queuedValidation != null)
        {
            StopCoroutine(_queuedValidation);
            _queuedValidation = null;
        }
    }

    [ContextMenu("Validate Runtime Scene Dependencies")]
    // Purpose: Validates now and reports invalid authored data.
    public void ValidateNow()
    {
        _issues.Clear();

        ValidatePersistentDependencies();

        bool homeLoaded = IsSceneLoaded(_homeSceneName);
        bool runLoaded = IsSceneLoaded(_runSceneName);

        if (homeLoaded)
            ValidateHomeDependencies();

        if (runLoaded)
            ValidateRunDependencies(homeLoaded);

        PublishReport(homeLoaded, runLoaded);
    }

    // Purpose: Handles the incoming scene loaded request or event.
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        QueueValidation();
    }

    // Purpose: Handles the incoming scene unloaded request or event.
    private void HandleSceneUnloaded(Scene scene)
    {
        QueueValidation();
    }

    // Purpose: Queues validation for deferred processing.
    private void QueueValidation()
    {
        if (!isActiveAndEnabled)
            return;

        if (_queuedValidation != null)
            StopCoroutine(_queuedValidation);

        _queuedValidation = StartCoroutine(ValidateAfterSceneSettle());
    }

    // Purpose: Validates after scene settle and reports invalid authored data.
    private IEnumerator ValidateAfterSceneSettle()
    {
        // Addressable additive loads and Awake/OnEnable chains finish over the
        // next frames. Waiting avoids false errors during normal Home -> RUN.
        yield return null;
        yield return null;

        _queuedValidation = null;
        ValidateNow();
    }

    // Purpose: Validates persistent dependencies and reports invalid authored data.
    private void ValidatePersistentDependencies()
    {
        ValidateComponentCount<RunLifecycleCoordinator>(
            "RunLifecycleCoordinator",
            required: true);

        ValidateComponentCount<SceneLoader>(
            "SceneLoader",
            required: false);

        if (SaveSystem.Active == null)
        {
            AddIssue(
                RuntimeDependencySeverity.Error,
                "SaveSystem.Active is missing. Persistent profile load/save cannot be guaranteed.");
        }
    }

    // Purpose: Validates home dependencies and reports invalid authored data.
    private void ValidateHomeDependencies()
    {
        HomeSector[] allHomeSectors =
            FindObjectsByType<HomeSector>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        int countInHomeScene = 0;
        HomeSector resolvedHomeSector = null;

        for (int i = 0; i < allHomeSectors.Length; i++)
        {
            HomeSector candidate = allHomeSectors[i];
            if (candidate == null || candidate.gameObject.scene.name != _homeSceneName)
                continue;

            countInHomeScene++;
            resolvedHomeSector = candidate;
        }

        if (countInHomeScene != 1)
        {
            AddIssue(
                RuntimeDependencySeverity.Error,
                $"HomeScene requires exactly one HomeSector, found {countInHomeScene}.");
            return;
        }

        if (resolvedHomeSector.SectorRuntime == null &&
            resolvedHomeSector.GetComponent<SectorRuntime>() == null)
        {
            AddIssue(
                RuntimeDependencySeverity.Error,
                "HomeSector is missing its SectorRuntime reference/component.");
        }
    }

    // Purpose: Validates run dependencies and reports invalid authored data.
    private void ValidateRunDependencies(bool homeLoaded)
    {
        ValidateComponentCount<StageProgressionManager>(
            "StageProgressionManager",
            required: true);
        ValidateComponentCount<SectorStateManager>(
            "SectorStateManager",
            required: true);
        ValidateComponentCount<StageSectorInstantiator>(
            "StageSectorInstantiator",
            required: true);
        ValidateComponentCount<InfectionControlManager>(
            "InfectionControlManager",
            required: true);

        if (!homeLoaded)
        {
            AddIssue(
                RuntimeDependencySeverity.Warning,
                "RUNScene is loaded before HomeScene. Direct editor launch may still be loading HomeScene; StageProgressionManager will report a timeout if it never arrives.");
        }
    }

    private void ValidateComponentCount<T>(string displayName, bool required)
        where T : Component
    {
        T[] instances = FindObjectsByType<T>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        if (instances.Length == 1)
            return;

        if (instances.Length == 0)
        {
            AddIssue(
                required ? RuntimeDependencySeverity.Error : RuntimeDependencySeverity.Warning,
                required
                    ? $"Required runtime service is missing: {displayName}."
                    : $"Optional direct-launch service is missing: {displayName}.");
            return;
        }

        AddIssue(
            RuntimeDependencySeverity.Error,
            $"Duplicate runtime service detected: {displayName} x{instances.Length}. Keep exactly one persistent instance.");
    }

    // Purpose: Adds the requested issue to this component.
    private void AddIssue(RuntimeDependencySeverity severity, string message)
    {
        _issues.Add(new RuntimeDependencyIssue(severity, message));
    }

    // Purpose: Publishes the report notification.
    private void PublishReport(bool homeLoaded, bool runLoaded)
    {
        _lastErrorCount = 0;
        _lastWarningCount = 0;

        StringBuilder report = new StringBuilder();
        report.Append("Scenes: Home=")
            .Append(homeLoaded ? "loaded" : "not loaded")
            .Append(", RUN=")
            .Append(runLoaded ? "loaded" : "not loaded");

        for (int i = 0; i < _issues.Count; i++)
        {
            RuntimeDependencyIssue issue = _issues[i];
            if (issue.Severity == RuntimeDependencySeverity.Error)
                _lastErrorCount++;
            else if (issue.Severity == RuntimeDependencySeverity.Warning)
                _lastWarningCount++;

            report.Append('\n')
                .Append('[')
                .Append(issue.Severity)
                .Append("] ")
                .Append(issue.Message);
        }

        if (_issues.Count == 0)
            report.Append("\n[Info] Critical runtime dependencies are valid.");

        _lastValidationReport = report.ToString();
        _lastValidationSummary = _lastErrorCount > 0
            ? $"FAILED: {_lastErrorCount} error(s), {_lastWarningCount} warning(s)"
            : _lastWarningCount > 0
                ? $"VALID WITH WARNINGS: {_lastWarningCount} warning(s)"
                : "VALID";

        string signature = _lastValidationSummary + "\n" + _lastValidationReport;
        if (!string.Equals(_lastLoggedSignature, signature, StringComparison.Ordinal))
        {
            _lastLoggedSignature = signature;

            if (_lastErrorCount > 0)
                Debug.LogError($"[RuntimeDependencies] {_lastValidationSummary}\n{_lastValidationReport}", this);
            else if (_lastWarningCount > 0)
                Debug.LogWarning($"[RuntimeDependencies] {_lastValidationSummary}\n{_lastValidationReport}", this);
            else
                Debug.Log($"[RuntimeDependencies] {_lastValidationSummary}\n{_lastValidationReport}", this);
        }

        Validated?.Invoke(this);
    }

    // Purpose: Checks whether scene loaded is currently true.
    private static bool IsSceneLoaded(string sceneName)
    {
        return !string.IsNullOrWhiteSpace(sceneName) &&
               SceneManager.GetSceneByName(sceneName).isLoaded;
    }
}
