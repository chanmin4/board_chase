using UnityEngine;

[DisallowMultipleComponent]
public class Attack_PaintBullet : AttackBullet
{
    private MaskRenderManager _maskRenderManager;
    private PaintChannel _paintChannel;
    private float _paintRadiusWorld;
    private int _paintPriority;
    private object _paintSender;

    public void Init(
        Vector3 gameplayStartPosition,
        Vector3 direction,
        Vector3 visualDirection,
        float maxDistance,
        float speed,
        float castRadius,
        float maxLifetime,
        float damage,
        int penetrationClass,
        float vaccineMarkAmountOnHit,
        float virusMarkAmountOnHit,
        float infectionDamageOnHit,
        float armorHealthDurabilityLossMultiplier,
        float armorInfectionDurabilityLossMultiplier,
        LayerMask damageTargetMask,
        LayerMask impactMask,
        QueryTriggerInteraction triggerInteraction,
        MaskRenderManager maskRenderManager,
        PaintChannel paintChannel,
        float paintRadiusWorld,
        int paintPriority,
        GameObject source,
        ShootHitConfirmedEventChannelSO shootHitConfirmedEvent)
    {
        _maskRenderManager = maskRenderManager;
        _paintChannel = paintChannel;
        _paintRadiusWorld = Mathf.Max(0.01f, paintRadiusWorld);
        _paintPriority = paintPriority;
        _paintSender = source;

        base.Init(
            gameplayStartPosition,
            direction,
            visualDirection,
            maxDistance,
            speed,
            castRadius,
            maxLifetime,
            damage,
            penetrationClass,
            vaccineMarkAmountOnHit,
            virusMarkAmountOnHit,
            infectionDamageOnHit,
            armorHealthDurabilityLossMultiplier,
            armorInfectionDurabilityLossMultiplier,
            damageTargetMask,
            impactMask,
            triggerInteraction,
            source,
            shootHitConfirmedEvent);
    }

    protected override void OnCompleted(Vector3 worldPoint)
    {
        if (_maskRenderManager == null)
            return;

        _maskRenderManager.RequestCircle(
            _paintChannel,
            worldPoint,
            _paintRadiusWorld,
            _paintPriority,
            _paintSender ?? this);
    }
}
