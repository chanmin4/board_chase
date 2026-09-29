/*
 * Role hierarchy: Core > Persistence
 * Primary responsibility: Keeps the pure, testable lifecycle rules used by PersistentManager.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

/// <summary>
/// Stateless lifecycle rules. Keeping these decisions outside the scene component
/// lets EditMode tests protect run boundaries without creating a PersistentManager
/// or touching the real save file.
/// </summary>
public static class RunLifecyclePolicy
{
    public static RunLifecyclePhase ResolveApplicationPausePhase(
        RunLifecyclePhase currentPhase,
        bool paused,
        RunLifecyclePhase phaseBeforeSuspend)
    {
        if (paused)
            return currentPhase == RunLifecyclePhase.RunActive
                ? RunLifecyclePhase.RunSuspended
                : currentPhase;

        return currentPhase == RunLifecyclePhase.RunSuspended
            ? phaseBeforeSuspend
            : currentPhase;
    }

    // Purpose: Normalizes active stage index to a safe authored value.
    public static int NormalizeActiveStageIndex(int stageIndex)
    {
        return Mathf.Max(0, stageIndex);
    }

    // Purpose: Checks whether process run loss is currently permitted.
    public static bool CanProcessRunLoss(RunLifecyclePhase currentPhase)
    {
        return currentPhase != RunLifecyclePhase.GameOver;
    }
}
