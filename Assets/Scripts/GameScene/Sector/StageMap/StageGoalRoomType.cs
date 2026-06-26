public enum StageGoalRoomType
{
    Named = 3,
    Boss = 6,
    BigMonsterWave = 7
}

public static class StageGoalRoomTypeExtensions
{
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
