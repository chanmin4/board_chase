/*
 * Role hierarchy: Gameplay > World > Sectors > StageMap
 * Primary responsibility: Manages StageRoomNode stage, sector, navigation, spawn, or spatial progression state.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[Serializable]
public class StageRoomNode
{
    public Vector2Int coord;
    public StageRoomType roomType;
    public RaidRoomRole raidRole;
    public bool isCleared;
    public bool isRevealed;
    public bool virusRoom;
    public int extraEnemyCount;
    public bool isOpened;
    // Per-room outgoing portal power, independent of reveal/clear. Old saves default to off.
    public bool portalsActivated;
    public bool isLocked;
    public StageRoomConnectionFlags connections;
    public bool hasTraitSnapshot;
    public SectorRoomTraitType savedTrait;
    public int gasLeakStacks;
    public bool vaultEntryUnlocked;
    // Independent from isCleared: passing the perimeter never completes this encounter.
    public bool hasInternalCombatLock;
    public bool internalCombatUnlocked;
    public LootKeyTier internalCombatKeyTier;
    public string specialSectorPoolId;
    public string specialSectorVariantId;
    // Empty on old saves. Assignment is generated once and is never rerolled on resume.
    public string combatPoolId;
    // Overlay on a common combat room, not a cleared Merchant-role replacement room.
    public bool hasMerchant;

    public StageRoomNode(
        Vector2Int coord,
        StageRoomType roomType,
        StageRoomConnectionFlags connections)
    {
        this.coord = coord;
        this.roomType = roomType;
        this.connections = connections;
    }

    // Purpose: Checks whether this component has connection.
    public bool HasConnection(StageRoomConnectionFlags flag)
    {
        return (connections & flag) != 0;
    }
}
