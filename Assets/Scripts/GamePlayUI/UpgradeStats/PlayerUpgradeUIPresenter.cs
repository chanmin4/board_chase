using UnityEngine;

public static class PlayerUpgradeUIPresenter
{
    private const int MaxLevel = 10;

    public static PlayerUpgradeUISnapshot Build(
        UpgradeCatalogSO catalog,
        PlayerUpgradeState state)
    {
        PlayerUpgradeUISnapshot snapshot = default;

        snapshot.unspentPoints = state != null ? state.UnspentPoints : 0;
        snapshot.removal = BuildTrack(catalog, state, PlayerUpgradeTrack.Removal, "제거");
        snapshot.occupation = BuildTrack(catalog, state, PlayerUpgradeTrack.Occupation, "점유");
        snapshot.control = BuildTrack(catalog, state, PlayerUpgradeTrack.Control, "특수제어 및 생존");

        return snapshot;
    }

    private static PlayerUpgradeTrackViewData BuildTrack(
        UpgradeCatalogSO catalog,
        PlayerUpgradeState state,
        PlayerUpgradeTrack track,
        string title)
    {
        int currentLevel = state != null ? state.GetTrackLevel(track) : 0;
        int nextLevel = Mathf.Clamp(currentLevel + 1, 1, MaxLevel);
        int unspentPoints = state != null ? state.UnspentPoints : 0;

        PlayerUpgradeNodeViewData[] nodes = new PlayerUpgradeNodeViewData[MaxLevel];

        for (int i = 0; i < MaxLevel; i++)
        {
            int level = i + 1;

            PlayerUpgradeDefinition upgrade = null;
            bool hasUpgrade = catalog != null && catalog.TryGetUpgrade(track, level, out upgrade);

            int cost = hasUpgrade ? Mathf.Max(0, upgrade.cost) : 0;
            bool lockedInBeta = hasUpgrade && upgrade.lockedInBeta;

            PlayerUpgradeNodeState nodeState = ResolveNodeState(
                level,
                currentLevel,
                cost,
                unspentPoints,
                lockedInBeta,
                hasUpgrade);

            nodes[i] = new PlayerUpgradeNodeViewData
            {
                track = track,
                level = level,
                cost = cost,
                icon = hasUpgrade ? upgrade.icon : null,
                displayName = hasUpgrade ? upgrade.displayName : $"Level {level}",
                description = hasUpgrade ? upgrade.description : string.Empty,
                state = nodeState,
                canPurchase = nodeState == PlayerUpgradeNodeState.Available,
                isPurchased = nodeState == PlayerUpgradeNodeState.Purchased
            };
        }

        return new PlayerUpgradeTrackViewData
        {
            track = track,
            title = title,
            currentLevel = currentLevel,
            nextLevel = nextLevel,
            maxLevel = MaxLevel,
            scrollFocusLevel = Mathf.Clamp(currentLevel <= 0 ? 1 : currentLevel, 1, MaxLevel),
            nodes = nodes
        };
    }

    private static PlayerUpgradeNodeState ResolveNodeState(
        int level,
        int currentLevel,
        int cost,
        int unspentPoints,
        bool lockedInBeta,
        bool hasUpgrade)
    {
        if (!hasUpgrade || lockedInBeta)
            return PlayerUpgradeNodeState.LockedInBeta;

        if (level <= currentLevel)
            return PlayerUpgradeNodeState.Purchased;

        if (level > currentLevel + 1)
            return PlayerUpgradeNodeState.LockedByPreviousLevel;

        if (unspentPoints < cost)
            return PlayerUpgradeNodeState.LockedByPoints;

        return PlayerUpgradeNodeState.Available;
    }
}
