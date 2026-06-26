using System;

public enum StageRoomType
{
    Empty = 0,
    Start = 1,
    NormalBattle = 2,
    Named = 3,
    Treasure = 4,
    Shop = 5,
    Boss = 6,
    BigMonsterWave = 7
}

[Flags]
public enum StageRoomConnectionFlags
{
    None = 0,
    Left = 1 << 0,
    Right = 1 << 1,
    Down = 1 << 2,
    Up = 1 << 3
}