using UnityEngine;

/// <summary>Faction identity is independent of Player/NPC weapon, inventory and balance rules.</summary>
public static class ShooterFactionUtility
{
    public static ShooterFaction Resolve(Component owner)
    {
        if (owner == null) return ShooterFaction.Scraper;
        var stats = EntityComponentResolver.Resolve<ShooterStatsRuntime>(owner);
        if (stats != null) return stats.Faction;
        var shooter = EntityComponentResolver.Resolve<EnemyShooterShoot>(owner);
        if (shooter != null && shooter.Config != null) return shooter.Config.Faction;
        return EntityComponentResolver.Resolve<PlayerCharacter>(owner) != null
            ? ShooterFaction.Player : ShooterFaction.Scraper;
    }

    // This class does not change encounter composition or automatically acquire AI targets.
    public static bool AreHostile(ShooterFaction a, ShooterFaction b) => a != b;
}
