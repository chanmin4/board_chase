public static class RunResult
{
    public static int Stage { get; private set; } = 1;
    public static string StageDisplayName { get; private set; } = "1 Stage";

    public static void SetStage(int stage, string displayName)
    {
        Stage = stage < 1 ? 1 : stage;
        StageDisplayName = string.IsNullOrWhiteSpace(displayName)
            ? $"{Stage} Stage"
            : displayName;
    }

    public static void Clear()
    {
        Stage = 1;
        StageDisplayName = "1 Stage";
    }
}
