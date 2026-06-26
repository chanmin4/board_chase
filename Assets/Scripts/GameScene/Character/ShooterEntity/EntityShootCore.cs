using System;
using UnityEngine;


[Serializable]
public struct EntityShootStats
{
    public float maxRange;
    public float damage;
    public float paintRadius;
    public int paintPriority;
    public PaintChannel paintChannel;
    public LayerMask damageTargetMask;
    public PaintMarkFaction hitMarkFaction;
    public float hitMarkAmount;

    public EntityShootStats(
        float maxRange,
        float damage,
        float paintRadius,
        int paintPriority,
        PaintChannel paintChannel = PaintChannel.Vaccine)
        : this(
            maxRange,
            damage,
            paintRadius,
            paintPriority,
            paintChannel,
            default,
            PaintMarkFaction.None,
            0f)
    {
    }

    public EntityShootStats(
        float maxRange,
        float damage,
        float paintRadius,
        int paintPriority,
        PaintChannel paintChannel,
        LayerMask damageTargetMask,
        PaintMarkFaction hitMarkFaction,
        float hitMarkAmount)
    {
        this.maxRange = Mathf.Max(0.1f, maxRange);
        this.damage = Mathf.Max(0f, damage);
        this.paintRadius = Mathf.Max(0f, paintRadius);
        this.paintPriority = paintPriority;
        this.paintChannel = paintChannel;
        this.damageTargetMask = damageTargetMask;
        this.hitMarkFaction = hitMarkFaction;
        this.hitMarkAmount = Mathf.Max(0f, hitMarkAmount);
    }
}

public abstract class EntityShootCore : MonoBehaviour
{
    public event Action<BulletAmmoType> Fired;

    protected bool TryFirePrimary(
        BulletSO bulletConfig,
        Vector3 gameplayStart,
        Vector3 gameplayDirection,
        Vector3 visualDirection,
        float maxDistance,
        Vector3 visualSpawn,
        Transform projectilesRoot,
        MaskRenderManager maskRenderManager,
        EntityShootStats stats,
        GameObject source,
        ShootHitConfirmedEventChannelSO shootHitConfirmedEvent,
        bool debugLogs,
        bool debugDraw,
        float debugDrawDuration)
    {
        if (!ValidatePrimaryBullet(bulletConfig))
            return false;

        bool needsPaint = bulletConfig.AmmoType == BulletAmmoType.AttackAndPaint;

        if (needsPaint && maskRenderManager == null)
            return false;

        Quaternion rotation = Quaternion.LookRotation(visualDirection, Vector3.up);

        GameObject bulletObject = Instantiate(
            bulletConfig.ProjectilePrefab,
            visualSpawn,
            rotation,
            projectilesRoot);

        ResolveHitMarkAmounts(
            stats,
            out float vaccineMarkAmountOnHit,
            out float virusMarkAmountOnHit);

        if (needsPaint)
        {
            Attack_PaintBullet bullet = bulletObject.GetComponent<Attack_PaintBullet>();

            if (bullet == null)
            {
                Debug.LogError(
                    $"[EntityShootCore] Attack_PaintBullet missing after instantiate: {bulletConfig.ProjectilePrefab.name}",
                    bulletObject);
                Destroy(bulletObject);
                return false;
            }

            bullet.Init(
                gameplayStart,
                gameplayDirection,
                visualDirection,
                maxDistance,
                bulletConfig.Speed,
                bulletConfig.CastRadius,
                bulletConfig.MaxLifetime,
                stats.damage,
                vaccineMarkAmountOnHit,
                virusMarkAmountOnHit,
                stats.damageTargetMask,
                bulletConfig.ProjectileCollisionMask,
                bulletConfig.ProjectileTriggerInteraction,
                maskRenderManager,
                stats.paintChannel,
                stats.paintRadius,
                stats.paintPriority,
                source,
                shootHitConfirmedEvent);
        }
        else
        {
            AttackBullet bullet = bulletObject.GetComponent<AttackBullet>();

            if (bullet == null)
            {
                Debug.LogError(
                    $"[EntityShootCore] AttackBullet missing after instantiate: {bulletConfig.ProjectilePrefab.name}",
                    bulletObject);
                Destroy(bulletObject);
                return false;
            }

            bullet.Init(
                gameplayStart,
                gameplayDirection,
                visualDirection,
                maxDistance,
                bulletConfig.Speed,
                bulletConfig.CastRadius,
                bulletConfig.MaxLifetime,
                stats.damage,
                vaccineMarkAmountOnHit,
                virusMarkAmountOnHit,
                stats.damageTargetMask,
                bulletConfig.ProjectileCollisionMask,
                bulletConfig.ProjectileTriggerInteraction,
                source,
                shootHitConfirmedEvent);
        }

        if (debugDraw)
            Debug.DrawLine(visualSpawn, visualSpawn + visualDirection * maxDistance, Color.yellow, debugDrawDuration);

        if (debugLogs)
            Debug.Log($"[EntityShootCore] Fired primary. bullet={bulletConfig.name}, ammoType={bulletConfig.AmmoType}", this);

        PublishFired(bulletConfig);
        return true;
    }

    protected bool TryFirePaint(
        PaintBulletSO bulletConfig,
        Vector3 gameplayStart,
        Vector3 gameplayDirection,
        Vector3 visualSpawn,
        Vector3 visualTarget,
        Vector3 paintTarget,
        Transform projectilesRoot,
        MaskRenderManager maskRenderManager,
        EntityShootStats stats,
        object sender,
        bool debugLogs,
        bool debugDraw,
        float debugDrawDuration)
    {
        if (!ValidatePaintBullet(bulletConfig))
            return false;

        if (maskRenderManager == null)
            return false;

        Quaternion rotation = Quaternion.LookRotation(gameplayDirection, Vector3.up);

        GameObject bulletObject = Instantiate(
            bulletConfig.ProjectilePrefab,
            visualSpawn,
            rotation,
            projectilesRoot);

        PaintBullet bullet = bulletObject.GetComponent<PaintBullet>();

        if (bullet == null)
        {
            Debug.LogError(
                $"[EntityShootCore] PaintBullet missing after instantiate: {bulletConfig.ProjectilePrefab.name}",
                bulletObject);
            Destroy(bulletObject);
            return false;
        }

        bullet.Init(
            gameplayStart,
            gameplayDirection,
            visualSpawn,
            visualTarget,
            paintTarget,
            bulletConfig.Speed,
            bulletConfig.CastRadius,
            bulletConfig.MaxLifetime,
            bulletConfig.ProjectileCollisionMask,
            bulletConfig.ProjectileTriggerInteraction,
            maskRenderManager,
            stats.paintChannel,
            stats.paintRadius,
            stats.paintPriority,
            sender);

        if (debugDraw)
            Debug.DrawLine(visualSpawn, paintTarget, Color.cyan, debugDrawDuration);

        if (debugLogs)
            Debug.Log($"[EntityShootCore] Fired paint. bullet={bulletConfig.name}, ammoType={bulletConfig.AmmoType}", this);

        PublishFired(bulletConfig);
        return true;
    }

    protected bool ValidatePrimaryBullet(BulletSO bulletConfig)
    {
        if (bulletConfig == null || bulletConfig.ProjectilePrefab == null)
            return false;

        if (bulletConfig.AmmoType == BulletAmmoType.AttackAndPaint)
        {
            AttackAndPaintBulletSO attackAndPaintConfig = bulletConfig as AttackAndPaintBulletSO;

            if (attackAndPaintConfig == null)
            {
                Debug.LogError($"[EntityShootCore] AttackAndPaint ammo requires AttackAndPaintBulletSO: {bulletConfig.name}", bulletConfig);
                return false;
            }

            if (!bulletConfig.ProjectilePrefab.TryGetComponent<Attack_PaintBullet>(out _))
            {
                Debug.LogError($"[EntityShootCore] AttackAndPaint projectile prefab requires Attack_PaintBullet: {bulletConfig.ProjectilePrefab.name}", bulletConfig.ProjectilePrefab);
                return false;
            }
            return true;
        }

        AttackBulletSO attackConfig = bulletConfig as AttackBulletSO;

        if (attackConfig == null)
        {
            Debug.LogError($"[EntityShootCore] Attack ammo requires AttackBulletSO: {bulletConfig.name}", bulletConfig);
            return false;
        }

        bool hasAttackBullet = bulletConfig.ProjectilePrefab.TryGetComponent<AttackBullet>(out _);
        bool hasAttackPaintBullet = bulletConfig.ProjectilePrefab.TryGetComponent<Attack_PaintBullet>(out _);

        if (!hasAttackBullet || hasAttackPaintBullet)
        {
            Debug.LogError($"[EntityShootCore] Attack projectile prefab requires AttackBullet only: {bulletConfig.ProjectilePrefab.name}", bulletConfig.ProjectilePrefab);
            return false;
        }
        return true;
    }

    protected bool ValidatePaintBullet(PaintBulletSO bulletConfig)
    {
        if (bulletConfig == null || bulletConfig.ProjectilePrefab == null)
            return false;

        if (!bulletConfig.ProjectilePrefab.TryGetComponent<PaintBullet>(out _))
        {
            Debug.LogError($"[EntityShootCore] Paint projectile prefab requires PaintBullet: {bulletConfig.ProjectilePrefab.name}", bulletConfig.ProjectilePrefab);
            return false;
        }

        return true;
    }

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
        Vector3 targetPoint = VSplatterAimUtility.ClampFlatPointToRange(
            rangeOrigin.position,
            aimPoint,
            Mathf.Max(0.1f, maxRange));

        gameplayDirection = targetPoint - visualStart;
        gameplayDirection.y = 0f;

        if (gameplayDirection.sqrMagnitude < 0.0001f)
            return false;

        gameplayDirection.Normalize();

        visualDirection = visualAimPoint - visualStart;
        visualDirection.y = 0f;

        if (visualDirection.sqrMagnitude < 0.0001f)
            visualDirection = gameplayDirection;

        visualDirection.Normalize();

        float spawnOffset = bulletConfig.SpawnOffset;

        gameplayStart = visualStart + gameplayDirection * spawnOffset;
        visualSpawn = visualStart + visualDirection * spawnOffset;

        maxDistance = Vector3.Distance(
            new Vector3(gameplayStart.x, 0f, gameplayStart.z),
            new Vector3(targetPoint.x, 0f, targetPoint.z));

        return maxDistance > 0.001f;
    }

    protected bool TryResolvePaintShotFromAimPoints(
        PaintBulletSO bulletConfig,
        Vector3 aimPoint,
        Vector3 visualAimPoint,
        float maxRange,
        Transform visualFireOrigin,
        Transform rangeOrigin,
        out Vector3 gameplayStart,
        out Vector3 gameplayDirection,
        out Vector3 visualSpawn,
        out Vector3 visualTarget,
        out Vector3 paintTarget)
    {
        gameplayStart = default;
        gameplayDirection = default;
        visualSpawn = default;
        visualTarget = default;
        paintTarget = default;

        if (bulletConfig == null || visualFireOrigin == null || rangeOrigin == null)
            return false;

        Vector3 visualStart = visualFireOrigin.position;

        paintTarget = VSplatterAimUtility.ClampFlatPointToRange(
            rangeOrigin.position,
            aimPoint,
            Mathf.Max(0.1f, maxRange));

        gameplayDirection = paintTarget - visualStart;
        gameplayDirection.y = 0f;

        if (gameplayDirection.sqrMagnitude < 0.0001f)
            return false;

        gameplayDirection.Normalize();

        Vector3 visualDirection = visualAimPoint - visualStart;
        visualDirection.y = 0f;

        if (visualDirection.sqrMagnitude < 0.0001f)
            visualDirection = gameplayDirection;

        visualDirection.Normalize();

        float spawnOffset = bulletConfig.SpawnOffset;

        gameplayStart = visualStart + gameplayDirection * spawnOffset;
        visualSpawn = visualStart + visualDirection * spawnOffset;

        float maxVisualDistance = Vector3.Distance(
            new Vector3(gameplayStart.x, 0f, gameplayStart.z),
            new Vector3(paintTarget.x, 0f, paintTarget.z));

        visualTarget = visualSpawn + visualDirection * maxVisualDistance;
        return true;
    }

    protected static MaskRenderManager ResolveMaskRenderManager(MaskRenderManager cached)
    {
        return cached != null ? cached : FindAnyObjectByType<MaskRenderManager>();
    }

    private static void ResolveHitMarkAmounts(
        EntityShootStats stats,
        out float vaccineMarkAmountOnHit,
        out float virusMarkAmountOnHit)
    {
        vaccineMarkAmountOnHit = 0f;
        virusMarkAmountOnHit = 0f;

        if (stats.hitMarkAmount <= 0f)
            return;

        switch (stats.hitMarkFaction)
        {
            case PaintMarkFaction.Vaccine:
                vaccineMarkAmountOnHit = stats.hitMarkAmount;
                break;

            case PaintMarkFaction.Virus:
                virusMarkAmountOnHit = stats.hitMarkAmount;
                break;
        }
    }

    protected void PublishFired(BulletSO bulletConfig)
    {
        if (bulletConfig != null)
            Fired?.Invoke(bulletConfig.AmmoType);
    }
}
