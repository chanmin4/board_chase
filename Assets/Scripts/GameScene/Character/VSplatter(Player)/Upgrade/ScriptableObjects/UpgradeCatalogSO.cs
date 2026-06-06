using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerUpgradeCatalog",
    menuName = "Game/Player/Upgrade Catalog")]
public class UpgradeCatalogSO : ScriptableObject
{
    [Header("Level Upgrade Tracks")]
    [SerializeField] private PlayerUpgradeTrackDefinition[] _tracks;

    [Header("Named/Boss Reward Pool")]
    [SerializeField] private BossUpgradeDefinition[] _bossUpgrades;

    public bool TryGetUpgrade(
        PlayerUpgradeTrack track,
        int level,
        out PlayerUpgradeDefinition upgrade)
    {
        upgrade = null;

        if (level <= 0)
            return false;

        PlayerUpgradeTrackDefinition trackDefinition = FindTrack(track);
        if (trackDefinition == null || trackDefinition.upgrades == null)
            return false;

        for (int i = 0; i < trackDefinition.upgrades.Length; i++)
        {
            PlayerUpgradeDefinition candidate = trackDefinition.upgrades[i];
            if (candidate != null && candidate.level == level)
            {
                upgrade = candidate;
                return true;
            }
        }

        return false;
    }

    public bool TryGetBossUpgrade(string id, out BossUpgradeDefinition upgrade)
    {
        upgrade = null;

        if (string.IsNullOrWhiteSpace(id) || _bossUpgrades == null)
            return false;

        for (int i = 0; i < _bossUpgrades.Length; i++)
        {
            BossUpgradeDefinition candidate = _bossUpgrades[i];
            if (candidate != null && candidate.id == id)
            {
                upgrade = candidate;
                return true;
            }
        }

        return false;
    }

    public int GetUpgradeCost(PlayerUpgradeTrack track, int level)
    {
        if (TryGetUpgrade(track, level, out PlayerUpgradeDefinition upgrade))
            return Mathf.Max(0, upgrade.cost);

        return int.MaxValue;
    }

    private PlayerUpgradeTrackDefinition FindTrack(PlayerUpgradeTrack track)
    {
        if (_tracks == null)
            return null;

        for (int i = 0; i < _tracks.Length; i++)
        {
            if (_tracks[i] != null && _tracks[i].track == track)
                return _tracks[i];
        }

        return null;
    }
}
