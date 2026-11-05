/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Manages SectorMapCellSnapshot stage, sector, navigation, spawn, or spatial progression state.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[Serializable]
public struct SectorMapCellSnapshot
{
    public Vector2Int coord;
    public bool isOpened;
    public bool isRevealed;
    public bool isLocked;
    public bool isStartSector;
    public bool isCleared;
    public bool isExtraction;
    public RaidRoomRole raidRole;
    public bool hasPublicRaidMarker;
    public StageRoomType roomType;
    public SectorMapIconKind mapIconKind;
    // Retired per-preset image field. Kept only for old consumers; no active renderer uses it.
    public Sprite mapIcon;

    public SectorOwner owner;
    public SectorOwner dominantOwner;
    public SectorContestState contestState;
    public SectorSpecialState specialState;

    public float playerRatio;
    public float virusRatio;
    public float contestElapsed;
    public float contestRequired;
}

[Serializable]
public struct SectorMapSnapshot
{
    public Vector2Int currentSectorCoord;
    public SectorMapCellSnapshot[] cells;
}

[CreateAssetMenu(fileName = "SectorMapSnapshotChanged", menuName = "Events/Sector Map Snapshot Changed")]
public class SectorMapSnapshotEventChannelSO : ScriptableObject
{
    public event Action<SectorMapSnapshot> OnEventRaised;

    // Purpose: Publishes the event notification.
    public void RaiseEvent(SectorMapSnapshot snapshot)
    {
        OnEventRaised?.Invoke(snapshot);
    }
}
