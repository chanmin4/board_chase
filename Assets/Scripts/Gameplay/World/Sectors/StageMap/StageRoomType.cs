/*
 * Role hierarchy: Gameplay > World > Sectors > StageMap
 * Primary responsibility: Manages StageRoomType stage, sector, navigation, spawn, or spatial progression state.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;

public enum StageRoomType
{
    Empty = 0,
    Start = 1,
    // Keep the serialized value at 2 so every existing StageDefinitionSO and
    // generated-run snapshot remains compatible. Combat is configured by a
    // separate room policy in a later migration, not by this room identity.
    Normal = 2,
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
