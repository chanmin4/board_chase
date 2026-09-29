/*
 * Role hierarchy: Gameplay > Items > Weapons > Definitions
 * Primary responsibility: Handles SpecialBulletLaunchContext combat resolution, damage, projectile, or status changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public readonly struct SpecialBulletLaunchContext
{
    public readonly Vector3 GameplayStart;
    public readonly Vector3 GameplayDirection;
    public readonly Vector3 VisualStart;
    public readonly Vector3 VisualTarget;
    public readonly Vector3 ImpactTarget;
    public readonly MaskRenderManager MaskRenderManager;
    public readonly GameObject Source;
    public readonly SoundStimulusEventChannelSO SoundStimulusEvent;

    public SpecialBulletLaunchContext(
        Vector3 gameplayStart,
        Vector3 gameplayDirection,
        Vector3 visualStart,
        Vector3 visualTarget,
        Vector3 impactTarget,
        MaskRenderManager maskRenderManager,
        GameObject source,
        SoundStimulusEventChannelSO soundStimulusEvent)
    {
        GameplayStart = gameplayStart;
        GameplayDirection = gameplayDirection;
        VisualStart = visualStart;
        VisualTarget = visualTarget;
        ImpactTarget = impactTarget;
        MaskRenderManager = maskRenderManager;
        Source = source;
        SoundStimulusEvent = soundStimulusEvent;
    }
}

/// <summary>
/// Type boundary for special ammunition. Each concrete special owns its fields,
/// projectile validation, and launch initialization instead of sharing an enum.
/// </summary>
public abstract class SpecialBulletSO : BulletSO
{
    [Header("Special Targeting")]
    [Tooltip("Enable only for a special bullet that creates a zone at a selected world point. While this ammo is selected, the player sees a red warning circle clamped to Max Range, and firing uses its center as the projectile endpoint. When disabled, the projectile travels the full Max Range along the aim direction.")]
    [SerializeField] private bool _rangeControlZone;

    public sealed override BulletAmmoType AmmoType => BulletAmmoType.Special;

    public abstract Color DebugLineColor { get; }
    public virtual bool RequiresMaskRenderManager => false;
    public bool RangeControlZone => _rangeControlZone;
    public virtual float RangeControlZoneRadius => 0f;

    // Purpose: Validates special configuration and reports invalid authored data.
    public abstract bool ValidateSpecialConfiguration(out string error);

    // Purpose: Attempts to initialize projectile and reports whether the request succeeded.
    public abstract bool TryInitializeProjectile(
        GameObject projectileInstance,
        in SpecialBulletLaunchContext context,
        out string error);
}
