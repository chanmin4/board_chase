/*
 * Role hierarchy: Gameplay > World > Sectors > StageMap
 * Primary responsibility: Manages StageGoalRoomType stage, sector, navigation, spawn, or spatial progression state.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

public enum StageGoalRoomType
{
    Named = 3,
    Boss = 6,
    BigMonsterWave = 7
}

public static class StageGoalRoomTypeExtensions
{
    // Purpose: Performs the to stage room type workflow for this component.
    public static StageRoomType ToStageRoomType(this StageGoalRoomType goalRoomType)
    {
        return goalRoomType switch
        {
            StageGoalRoomType.Named => StageRoomType.Named,
            StageGoalRoomType.Boss => StageRoomType.Boss,
            StageGoalRoomType.BigMonsterWave => StageRoomType.BigMonsterWave,
            _ => StageRoomType.Named
        };
    }
}
