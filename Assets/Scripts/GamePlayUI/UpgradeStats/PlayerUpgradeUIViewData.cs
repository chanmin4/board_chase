using System;
using UnityEngine;

public enum PlayerUpgradeNodeState
{
    Purchased,
    Available,
    LockedByPreviousLevel,
    LockedByPoints,
    LockedInBeta
}

[Serializable]
public struct PlayerUpgradeUISnapshot
{
    public int unspentPoints;
    public PlayerUpgradeTrackViewData removal;
    public PlayerUpgradeTrackViewData occupation;
    public PlayerUpgradeTrackViewData control;
}

[Serializable]
public struct PlayerUpgradeTrackViewData
{
    public PlayerUpgradeTrack track;
    public string title;
    public int currentLevel;
    public int nextLevel;
    public int maxLevel;
    public int scrollFocusLevel;
    public PlayerUpgradeNodeViewData[] nodes;
}

[Serializable]
public struct PlayerUpgradeNodeViewData
{
    public PlayerUpgradeTrack track;
    public int level;
    public int cost;
    public Sprite icon;
    public string displayName;
    public string description;
    public PlayerUpgradeNodeState state;
    public bool canPurchase;
    public bool isPurchased;
}
