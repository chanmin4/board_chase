/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Core
 * Primary responsibility: Handles IMeleeAttackProfile combat resolution, damage, projectile, or status changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public interface IMeleeAttackProfile
{
    string ProfileName { get; }
    float Damage { get; }
    int PenetrationClass { get; }
    float ArmorHealthDurabilityLossMultiplier { get; }
    float Range { get; }
    float ArcAngle { get; }
    float HitRadius { get; }
    float CooldownSeconds { get; }
    float HitDelaySeconds { get; }
    float ActiveHitSeconds { get; }
    QueryTriggerInteraction TriggerInteraction { get; }
    AudioCueSO SwingAudioCue { get; }
    ParticleSystem SwingParticlePrefab { get; }
    ParticleSystem SwingTrailParticlePrefab { get; }
    LayerMask ResolveDamageTargetMask(ShooterFaction faction);
}
