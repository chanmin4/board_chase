using System;
using UnityEngine;

[Serializable]
public struct SectorMapCellSnapshot
{
    public Vector2Int coord;
    public bool isOpened;
    public bool isLocked;

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

    public void RaiseEvent(SectorMapSnapshot snapshot)
    {
        OnEventRaised?.Invoke(snapshot);
    }
}
