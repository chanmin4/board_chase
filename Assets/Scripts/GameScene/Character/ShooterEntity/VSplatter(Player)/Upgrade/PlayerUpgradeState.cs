using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerUpgradeState : MonoBehaviour

{
    [Header("Broadcasting")]
    [SerializeField] private PlayerUpgradeStateReadyEventChannelSO _upgradeStateReadyChannel;
    [Header("Don't touch these, they are runtime values \n \n")]

    [Header("Runtime Points")]
    [SerializeField] private int _unspentPoints;

    [Header("Track Levels")]
    [SerializeField, Range(0, 10)] private int _removalLevel;
    [SerializeField, Range(0, 10)] private int _occupationLevel;
    [SerializeField, Range(0, 10)] private int _controlLevel;

    [Header("Boss Upgrade Picks")]
    [SerializeField] private List<BossUpgradePickRecord> _bossUpgradePicks = new();

    public int UnspentPoints => _unspentPoints;
    public IReadOnlyList<BossUpgradePickRecord> BossUpgradePicks => _bossUpgradePicks;

    public event Action OnChanged;
    private void OnEnable()
    {
        if (_upgradeStateReadyChannel != null)
            _upgradeStateReadyChannel.RaiseEvent(this);
    }

    private void OnDisable()
    {
        if (_upgradeStateReadyChannel != null)
            _upgradeStateReadyChannel.Clear(this);
    }

    public int GetTrackLevel(PlayerUpgradeTrack track)
    {
        return track switch
        {
            PlayerUpgradeTrack.Removal => _removalLevel,
            PlayerUpgradeTrack.Occupation => _occupationLevel,
            PlayerUpgradeTrack.Control => _controlLevel,
            _ => 0
        };
    }

    public void AddPoints(int amount)
    {
        if (amount <= 0)
            return;

        _unspentPoints += amount;
        OnChanged?.Invoke();
    }

    public bool TryUpgradeTrack(PlayerUpgradeTrack track, UpgradeCatalogSO catalog)
    {
        if (catalog == null)
            return false;

        int currentLevel = GetTrackLevel(track);
        int nextLevel = currentLevel + 1;

        if (nextLevel > 10)
            return false;

        if (!catalog.TryGetUpgrade(track, nextLevel, out PlayerUpgradeDefinition upgrade))
            return false;

        if (upgrade.lockedInBeta)
            return false;

        int cost = Mathf.Max(0, upgrade.cost);
        if (_unspentPoints < cost)
            return false;

        _unspentPoints -= cost;
        SetTrackLevel(track, nextLevel);

        OnChanged?.Invoke();
        return true;
    }

    public void AddBossUpgradePick(string bossUpgradeId, bool infected)
    {
        if (string.IsNullOrWhiteSpace(bossUpgradeId))
            return;

        _bossUpgradePicks.Add(new BossUpgradePickRecord
        {
            bossUpgradeId = bossUpgradeId,
            infected = infected
        });

        OnChanged?.Invoke();
    }

    private void SetTrackLevel(PlayerUpgradeTrack track, int level)
    {
        level = Mathf.Clamp(level, 0, 10);

        switch (track)
        {
            case PlayerUpgradeTrack.Removal:
                _removalLevel = level;
                break;
            case PlayerUpgradeTrack.Occupation:
                _occupationLevel = level;
                break;
            case PlayerUpgradeTrack.Control:
                _controlLevel = level;
                break;
        }
    }
}
