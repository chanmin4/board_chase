/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Core
 * Primary responsibility: Implements the focused EntityShootStats responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;


[Serializable]
public struct EntityShootStats
{
    public float maxRange;
    public float damage;
    public LayerMask damageTargetMask;
    public PaintMarkFaction hitMarkFaction;
    public float hitMarkAmount;
    public float infectionDamageOnHit;
    public int penetrationClass;
    public float armorHealthDurabilityLossMultiplier;
    public float armorInfectionDurabilityLossMultiplier;

    public EntityShootStats(
        float maxRange,
        float damage,
        LayerMask damageTargetMask,
        PaintMarkFaction hitMarkFaction,
        float hitMarkAmount,
        float infectionDamageOnHit,
        int penetrationClass,
        float armorHealthDurabilityLossMultiplier = 1f,
        float armorInfectionDurabilityLossMultiplier = 0.5f)
    {
        this.maxRange = Mathf.Max(0.1f, maxRange);
        this.damage = Mathf.Max(0f, damage);
        this.damageTargetMask = damageTargetMask;
        this.hitMarkFaction = hitMarkFaction;
        this.hitMarkAmount = Mathf.Max(0f, hitMarkAmount);
        this.infectionDamageOnHit = Mathf.Max(0f, infectionDamageOnHit);
        this.penetrationClass = Mathf.Max(0, penetrationClass);
        this.armorHealthDurabilityLossMultiplier = Mathf.Max(0f, armorHealthDurabilityLossMultiplier);
        this.armorInfectionDurabilityLossMultiplier = Mathf.Max(0f, armorInfectionDurabilityLossMultiplier);
    }
}

public abstract class EntityShootCore : MonoBehaviour
{
    public event Action<BulletAmmoType> Fired;

    // Purpose: Attempts to fire primary and reports whether the request succeeded.
    protected bool TryFirePrimary(
        BulletSO bulletConfig,
        RangedWeaponSO weaponConfig,
        Vector3 gameplayStart,
        Vector3 gameplayDirection,
        Vector3 visualDirection,
        float maxDistance,
        Vector3 visualSpawn,
        Transform projectilesRoot,
        EntityShootStats stats,
        GameObject source,
        SoundStimulusEventChannelSO soundStimulusEvent,
        ShootHitConfirmedEventChannelSO shootHitConfirmedEvent,
        bool debugLogs,
        bool debugDraw,
        float debugDrawDuration)
    {
        if (IsActionBlocked() || !ValidatePrimaryBullet(bulletConfig))
            return false;

        ResolveHitMarkAmounts(
            stats,
            out float vaccineMarkAmountOnHit,
            out float virusMarkAmountOnHit);

        int projectileCount = weaponConfig != null
            ? Mathf.Max(1, weaponConfig.ResolveProjectileCount(bulletConfig))
            : 1;
        float spreadAngle = weaponConfig != null &&
                            weaponConfig.FirePattern == RangedFirePattern.PelletSpread
            ? weaponConfig.PelletSpreadAngleDegrees
            : 0f;

        for (int i = 0; i < projectileCount; i++)
        {
            float pelletAngle = ResolvePelletAngleDegrees(i, projectileCount, spreadAngle);
            Quaternion pelletRotation = Quaternion.AngleAxis(pelletAngle, Vector3.up);
            Vector3 pelletGameplayDirection = (pelletRotation * gameplayDirection).normalized;
            Vector3 pelletVisualDirection = (pelletRotation * visualDirection).normalized;
            Quaternion rotation = Quaternion.LookRotation(pelletVisualDirection, Vector3.up);

            GameObject bulletObject = Instantiate(
                bulletConfig.ProjectilePrefab,
                visualSpawn,
                rotation,
                projectilesRoot);
            AttachProjectileTrail(bulletConfig, bulletObject);

            BasicBullet bullet = bulletObject.GetComponent<BasicBullet>();

            if (bullet == null)
            {
                Debug.LogError(
                    $"[EntityShootCore] Basic projectile prefab requires BasicBullet: {bulletConfig.ProjectilePrefab.name}",
                    bulletObject);
                Destroy(bulletObject);
                continue;
            }

            bullet.Init(
                bulletConfig,
                gameplayStart,
                pelletGameplayDirection,
                pelletVisualDirection,
                maxDistance,
                bulletConfig.Speed,
                bulletConfig.CastRadius,
                bulletConfig.MaxLifetime,
                stats.damage,
                stats.penetrationClass,
                vaccineMarkAmountOnHit,
                virusMarkAmountOnHit,
                stats.infectionDamageOnHit,
                stats.armorHealthDurabilityLossMultiplier,
                stats.armorInfectionDurabilityLossMultiplier,
                stats.damageTargetMask,
                bulletConfig.ProjectileCollisionMask,
                bulletConfig.ProjectileTriggerInteraction,
                source,
                soundStimulusEvent,
                shootHitConfirmedEvent);

            if (debugDraw)
            {
                Debug.DrawLine(
                    visualSpawn,
                    visualSpawn + pelletVisualDirection * maxDistance,
                    Color.yellow,
                    debugDrawDuration);
            }
        }

        if (debugLogs)
        {
            Debug.Log(
                $"[EntityShootCore] Fired primary. bullet={bulletConfig.name}, family={bulletConfig.Family}, projectiles={projectileCount}, spread={spreadAngle:0.##}",
                this);
        }

        PublishFired(bulletConfig);
        return true;
    }

    // Purpose: Resolves the pellet angle degrees needed by this component.
    private static float ResolvePelletAngleDegrees(
        int projectileIndex,
        int projectileCount,
        float fullSpreadAngleDegrees)
    {
        if (projectileCount <= 1 || fullSpreadAngleDegrees <= 0f)
            return 0f;

        float halfSpread = fullSpreadAngleDegrees * 0.5f;
        float t = Mathf.Clamp01(projectileIndex / (float)(projectileCount - 1));
        float evenlySpacedAngle = Mathf.Lerp(-halfSpread, halfSpread, t);

        // Keep the outside pellets defining the configured cone while adding a
        // small inner jitter so repeated shotgun shots do not form a rigid fan.
        if (projectileIndex == 0 || projectileIndex == projectileCount - 1)
            return evenlySpacedAngle;

        float cellWidth = fullSpreadAngleDegrees / (projectileCount - 1);
        return evenlySpacedAngle + UnityEngine.Random.Range(-cellWidth * 0.2f, cellWidth * 0.2f);
    }

    // Purpose: Attempts to fire special and reports whether the request succeeded.
    protected bool TryFireSpecial(
        SpecialBulletSO bulletConfig,
        Vector3 gameplayStart,
        Vector3 gameplayDirection,
        Vector3 visualSpawn,
        Vector3 visualTarget,
        Vector3 impactTarget,
        Transform projectilesRoot,
        MaskRenderManager maskRenderManager,
        GameObject sender,
        SoundStimulusEventChannelSO soundStimulusEvent,
        bool debugLogs,
        bool debugDraw,
        float debugDrawDuration)
    {
        if (IsActionBlocked() || !ValidateSpecialBullet(bulletConfig))
            return false;

        Quaternion rotation = Quaternion.LookRotation(gameplayDirection, Vector3.up);

        GameObject bulletObject = Instantiate(
            bulletConfig.ProjectilePrefab,
            visualSpawn,
            rotation,
            projectilesRoot);
        AttachProjectileTrail(bulletConfig, bulletObject);

        SpecialBulletLaunchContext context = new SpecialBulletLaunchContext(
            gameplayStart,
            gameplayDirection,
            visualSpawn,
            visualTarget,
            impactTarget,
            maskRenderManager,
            sender,
            soundStimulusEvent);

        if (!bulletConfig.TryInitializeProjectile(
                bulletObject,
                context,
                out string initializationError))
        {
            Debug.LogError(initializationError, bulletConfig);
            Destroy(bulletObject);
            return false;
        }

        if (debugDraw)
            Debug.DrawLine(
                visualSpawn,
                impactTarget,
                bulletConfig.DebugLineColor,
                debugDrawDuration);

        if (debugLogs)
            Debug.Log($"[EntityShootCore] Fired special. bullet={bulletConfig.name}, type={bulletConfig.GetType().Name}", this);

        PublishFired(bulletConfig);
        return true;
    }

    // Purpose: Validates primary bullet and reports invalid authored data.
    protected bool ValidatePrimaryBullet(BulletSO bulletConfig)
    {
        if (bulletConfig == null || bulletConfig.ProjectilePrefab == null)
            return false;

        if (!bulletConfig.IsPrimary)
        {
            Debug.LogError($"[EntityShootCore] Basic ammo requires a non-special BulletSO: {bulletConfig.name}", bulletConfig);
            return false;
        }

        if (!bulletConfig.ProjectilePrefab.TryGetComponent<BasicBullet>(out _))
        {
            Debug.LogError($"[EntityShootCore] Basic projectile prefab requires BasicBullet: {bulletConfig.ProjectilePrefab.name}", bulletConfig.ProjectilePrefab);
            return false;
        }

        return true;
    }

    // Purpose: Checks whether action blocked is currently true.
    private bool IsActionBlocked()
    {
        ShooterStatsRuntime statsRuntime =
            EntityComponentResolver.Resolve<ShooterStatsRuntime>(this);

        return statsRuntime != null &&
               statsRuntime.StatusEffects != null &&
               statsRuntime.StatusEffects.BlocksActions;
    }

    // Purpose: Validates special bullet and reports invalid authored data.
    protected bool ValidateSpecialBullet(SpecialBulletSO bulletConfig)
    {
        if (bulletConfig == null || bulletConfig.ProjectilePrefab == null)
            return false;

        if (!bulletConfig.ValidateSpecialConfiguration(out string error))
        {
            Debug.LogError(error, bulletConfig);
            return false;
        }

        return true;
    }

    // Purpose: Attempts to resolve shot from aim points and reports whether the request succeeded.
    protected bool TryResolveShotFromAimPoints(
        BulletSO bulletConfig,
        Vector3 aimPoint,
        Vector3 visualAimPoint,
        float maxRange,
        Transform visualFireOrigin,
        Transform rangeOrigin,
        out Vector3 gameplayStart,
        out Vector3 gameplayDirection,
        out Vector3 visualDirection,
        out float maxDistance,
        out Vector3 visualSpawn)
    {
        gameplayStart = default;
        gameplayDirection = default;
        visualDirection = default;
        maxDistance = 0f;
        visualSpawn = default;

        if (bulletConfig == null || visualFireOrigin == null || rangeOrigin == null)
            return false;

        Vector3 visualStart = visualFireOrigin.position;

        // Basic bullets use the cursor only as a direction. They keep travelling
        // until Max Range or a collision instead of stopping below screen-space UI.
        Vector3 targetDirection = aimPoint - visualStart;
        targetDirection.y = 0f;
        gameplayDirection = targetDirection;

        if (gameplayDirection.sqrMagnitude < 0.0001f)
            return false;

        gameplayDirection.Normalize();

        visualDirection = gameplayDirection;

        if (visualDirection.sqrMagnitude < 0.0001f)
            visualDirection = gameplayDirection;

        visualDirection.Normalize();

        float resolvedMaxRange = Mathf.Max(0.1f, maxRange);
        float spawnOffset = Mathf.Min(
            bulletConfig.SpawnOffset,
            Mathf.Max(0f, resolvedMaxRange - 0.01f));

        gameplayStart = visualStart + gameplayDirection * spawnOffset;
        visualSpawn = visualStart + visualDirection * spawnOffset;

        maxDistance = resolvedMaxRange - spawnOffset;

        return maxDistance > 0.001f;
    }

    // Purpose: Attempts to resolve special shot from aim points and reports whether the request succeeded.
    protected bool TryResolveSpecialShotFromAimPoints(
        SpecialBulletSO bulletConfig,
        Vector3 aimPoint,
        Vector3 visualAimPoint,
        float maxRange,
        Transform visualFireOrigin,
        Transform rangeOrigin,
        out Vector3 gameplayStart,
        out Vector3 gameplayDirection,
        out Vector3 visualSpawn,
        out Vector3 visualTarget,
        out Vector3 impactTarget)
    {
        gameplayStart = default;
        gameplayDirection = default;
        visualSpawn = default;
        visualTarget = default;
        impactTarget = default;

        if (bulletConfig == null || visualFireOrigin == null || rangeOrigin == null)
            return false;

        Vector3 visualStart = visualFireOrigin.position;

        float resolvedMaxRange = Mathf.Max(0.1f, maxRange);
        Vector3 flatAimDirection = aimPoint - visualStart;
        flatAimDirection.y = 0f;

        if (flatAimDirection.sqrMagnitude < 0.0001f)
            return false;

        flatAimDirection.Normalize();

        if (bulletConfig.RangeControlZone)
        {
            impactTarget = VSplatterAimUtility.ClampFlatPointToRange(
                rangeOrigin.position,
                aimPoint,
                resolvedMaxRange);
        }
        else
        {
            impactTarget = visualStart + flatAimDirection * resolvedMaxRange;
            impactTarget.y = aimPoint.y;
        }

        Vector3 gameplayTarget = impactTarget;
        gameplayTarget.y = visualStart.y;
        gameplayDirection = gameplayTarget - visualStart;

        if (gameplayDirection.sqrMagnitude < 0.0001f)
            return false;

        gameplayDirection.Normalize();

        visualAimPoint.y = visualStart.y;
        Vector3 visualDirection = visualAimPoint - visualStart;

        if (visualDirection.sqrMagnitude < 0.0001f)
            visualDirection = gameplayDirection;

        visualDirection.Normalize();

        float targetDistance = Vector3.Distance(
            new Vector3(visualStart.x, 0f, visualStart.z),
            new Vector3(impactTarget.x, 0f, impactTarget.z));

        if (targetDistance <= 0.001f)
            return false;

        float spawnOffset = Mathf.Min(
            bulletConfig.SpawnOffset,
            Mathf.Max(0f, targetDistance - 0.01f));

        gameplayStart = visualStart + gameplayDirection * spawnOffset;
        visualSpawn = visualStart + visualDirection * spawnOffset;

        float maxVisualDistance = Vector3.Distance(
            new Vector3(gameplayStart.x, 0f, gameplayStart.z),
            new Vector3(impactTarget.x, 0f, impactTarget.z));

        visualTarget = visualSpawn + visualDirection * maxVisualDistance;
        return true;
    }

    // Purpose: Resolves the mask render manager needed by this component.
    protected static MaskRenderManager ResolveMaskRenderManager(MaskRenderManager cached)
    {
        return cached != null ? cached : FindAnyObjectByType<MaskRenderManager>();
    }

    // Purpose: Performs the attach projectile trail workflow for this component.
    private static void AttachProjectileTrail(BulletSO bulletConfig, GameObject bulletObject)
    {
        if (bulletConfig == null ||
            bulletConfig.ProjectileTrailPrefab == null ||
            bulletObject == null)
        {
            return;
        }

        GameObject trailObject = Instantiate(
            bulletConfig.ProjectileTrailPrefab,
            bulletObject.transform);

        Transform trailTransform = trailObject.transform;
        trailTransform.localPosition = Vector3.zero;
        trailTransform.localRotation = Quaternion.identity;
        trailTransform.localScale = Vector3.one;
    }

    // Purpose: Resolves the hit mark amounts needed by this component.
    private static void ResolveHitMarkAmounts(
        EntityShootStats stats,
        out float vaccineMarkAmountOnHit,
        out float virusMarkAmountOnHit)
    {
        vaccineMarkAmountOnHit = 0f;
        virusMarkAmountOnHit = 0f;

        // Mark and Infection are separate hit payloads. Mark must use its
        // authored bullet/stat value; using infection damage here left normal
        // mark bullets with a zero payload whenever they had no infection.
        float resolvedMarkAmount = Mathf.Max(0f, stats.hitMarkAmount);
        if (resolvedMarkAmount <= 0f)
            return;

        switch (stats.hitMarkFaction)
        {
            case PaintMarkFaction.Vaccine:
                vaccineMarkAmountOnHit = resolvedMarkAmount;
                break;

            case PaintMarkFaction.Virus:
                virusMarkAmountOnHit = resolvedMarkAmount;
                break;
        }
    }

    // Purpose: Publishes the fired notification.
    protected void PublishFired(BulletSO bulletConfig)
    {
        if (bulletConfig != null)
            Fired?.Invoke(bulletConfig.AmmoType);
    }
}
