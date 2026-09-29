/*
 * Role hierarchy: Core > SceneFlow
 * Primary responsibility: Implements the focused RunResult responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

public static class RunResult
{
    public static int Stage { get; private set; } = 1;
    public static string StageDisplayName { get; private set; } = "1 Stage";

    // Purpose: Updates the stage state.
    public static void SetStage(int stage, string displayName)
    {
        Stage = stage < 1 ? 1 : stage;
        StageDisplayName = string.IsNullOrWhiteSpace(displayName)
            ? $"{Stage} Stage"
            : displayName;
    }

    // Purpose: Clears the current clear state.
    public static void Clear()
    {
        Stage = 1;
        StageDisplayName = "1 Stage";
    }
}
