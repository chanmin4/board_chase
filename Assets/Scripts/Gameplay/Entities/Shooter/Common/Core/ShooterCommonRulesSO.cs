using UnityEngine;

/// <summary>One authored source for shared rules. Character configs opt into local overrides explicitly.</summary>
[CreateAssetMenu(fileName = "ShooterCommonRules", menuName = "Game/Character/Shooter Common Rules")]
public sealed class ShooterCommonRulesSO : ScriptableObject
{
    [SerializeField] private ShooterLocomotionSettings _locomotion = new();
    [SerializeField] private ShooterAimFeedbackSettings _aimFeedback = new();
    [SerializeField] private EntityVisionSettings _enemyVision = EntityVisionSettings.CreateSharedEnemyShooterDetection();
    [SerializeField] private Vector2 _normalArmorDropDurability = new(350f, 500f);
    [SerializeField] private Vector2 _bossArmorDropDurability = new(500f, 650f);

    public ShooterLocomotionSettings Locomotion => _locomotion;
    public ShooterAimFeedbackSettings AimFeedback => _aimFeedback;
    public EntityVisionSettings EnemyVision => _enemyVision;
    public Vector2 NormalArmorDropDurability => _normalArmorDropDurability;
    public Vector2 BossArmorDropDurability => _bossArmorDropDurability;
}
