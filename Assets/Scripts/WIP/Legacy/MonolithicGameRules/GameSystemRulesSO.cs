using System;
using UnityEngine;

/// <summary>WIP snapshot compatibility only. No resource loader, authoring menu or runtime consumers.</summary>
public sealed class GameSystemRulesSO : ScriptableObject
{
    public ExperienceRules experience = new();
    public LegacyTraversalRules traversal = new();
    public LegacyInfectionExposureRules infection = new();
    public RecyclingRules recycling = new();
    public UsageLevelRules usage = new();
    public Color[] rarityColors;
}
[Serializable] public sealed class LegacyTraversalRules
{
    public bool freeSectorTravel = true;
    [Min(0)] public float portalCooldown = 6f;
    [Min(0)] public float portalCast = 1f;
    public bool hideUnvisited = true;
    public bool hideBossIcons = true;
    [Min(0)] public float dashCooldown = 10f;
    [Min(0)] public float dashDistanceMultiplier = 1.3f;
    public bool homeOnlyPassiveChanges = true;
    [Min(0)] public float alertReactionMultiplier = .5f;
    [Min(0)] public float alertMemorySeconds = 15f;
    [Min(0)] public float patrolRadiusMultiplier = 1.5f;
    [Min(.1f)] public float patrolIdleMultiplier = .75f;
    [Range(0,1)] public float extraEnemyChance = .15f;
    [Range(0,1)] public float stage3PunisherReplacementChance = .03f;
    public EnemyShooterConfigSO stage3Punisher;
}
[Serializable] public sealed class LegacyInfectionExposureRules
{
    public bool enabled = true;
    [Min(1)] public float baseGaugeCapacity = 100;
    [Min(0)] public float defenseCapacityPerLevel = 25;
    [Min(0)] public float naturalGaugeRecoveryPerSecond = .25f;
    [Min(1)] public int maximumStacks = 20;
    [Min(.1f)] public float damageInterval = 5;
    [Min(0)] public float infectionPerStackPerTick = 1;
    [Min(0)] public float lootOpenExposure = 4;
    [Min(0)] public float itemPickupExposure = 1;
    [Min(0)] public float hitExposure = 5;
    [Min(0)] public float bleedingExposurePerSecond = 1;
    [Min(0)] public float gasExposurePerPressureSecond = 5;
    public int[] infectedRoomsByStage = {0,0,1,2,3,4,5};
    [Range(0,1)] public float infectedEnemyChance = .25f;
    [Min(0)] public float infectedBulletExposure = 15;
}
