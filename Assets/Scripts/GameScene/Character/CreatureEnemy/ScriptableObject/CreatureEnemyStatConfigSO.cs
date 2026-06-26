using System;
using UnityEngine;

[Serializable]
public class EnemyAttackSelectionOption
{
    [Header("Attack")]
    public EnemyAttackConfigSO attackConfig;

    [Header("Selection")]
    [Min(1)] public int weight = 1;
    public bool enabled = true;

    public bool IsValid => enabled && attackConfig != null;

    public bool IsSelectable(float distance)
    {
        return IsValid && attackConfig.IsInActivationRange(distance);
    }
}

public abstract class CreatureEnemyStatConfigSO : EnemyStatConfigSO
{
    [Header("Prefab")]
    [SerializeField] private Enemy _enemyPrefab;

    [Header("Creature Movement")]
    [SerializeField, Min(0f)] private float _playerChaseMovementSpeed = 3.5f;

    [Header("Contact Damage")]
    [SerializeField, Min(0f)] private float _contactHealthDamage = 5f;
    [SerializeField, Min(0f)] private float _contactInfectionDamage = 10f;
    [SerializeField, Min(0f)] private float _contactHitCooldown = 1f;

    [Header("Virus Trail")]
    [SerializeField] private bool _virusTrailEnabled = true;
    [SerializeField, Min(0f)] private float _virusTrailRadius = 0.3f;
    [SerializeField, Min(0f)] private float _virusTrailSelfExclusionDistance = 0.25f;
    [SerializeField, Min(0f)] private float _virusTrailMinSegmentDistance = 0.5f;
    [SerializeField] private int _virusTrailPaintPriority = 10;
    [SerializeField, Min(0f)] private float _virusTrailMinMoveSpeed = 0.05f;
    [SerializeField, Min(0f)] private float _virusTrailTeleportResetDistance = 40f;
    [SerializeField, Min(0f)] private float _virusTrailStampInterval = 0.04f;
    [SerializeField, Min(0f)] private float _virusTrailMaxPaintSegmentDistance = 1f;
    [SerializeField, Min(0f)] private float _virusTrailSpacing = 0.5f;
    [SerializeField, Min(1)] private int _virusTrailMaxSteps = 3;

    [Header("Attacks")]
    [SerializeField] private EnemyAttackSelectionOption[] _attackOptions;

    public override float ResolveInitialHealth()
    {
        return Mathf.Max(1f, DifficultyRuntime.ApplyEnemyHealth(InitialHealth));
    }

    public override float ReferenceMoveSpeed =>
        Mathf.Max(NormalMovementSpeed, PlayerChaseMovementSpeed);

    public override Enemy EnemyPrefab => _enemyPrefab;

    public float NormalMovementSpeed =>
        DifficultyRuntime.ApplyEnemyNormalMoveSpeed(MoveSpeed);

    public float PlayerChaseMovementSpeed =>
        DifficultyRuntime.ApplyEnemyChaseMoveSpeed(_playerChaseMovementSpeed);

    public float ContactHealthDamage =>
        DifficultyRuntime.ApplyEnemyDamage(_contactHealthDamage);

    public float ContactInfectionDamage => Mathf.Max(0f, _contactInfectionDamage);
    public float ContactHitCooldown => Mathf.Max(0f, _contactHitCooldown);

    public bool VirusTrailEnabled => _virusTrailEnabled;
    public float VirusTrailRadius => DifficultyRuntime.ApplyEnemyVirusPaintRadius(_virusTrailRadius);
    public float VirusTrailSelfExclusionDistance => Mathf.Max(0f, _virusTrailSelfExclusionDistance);
    public float VirusTrailMinSegmentDistance => Mathf.Max(0f, _virusTrailMinSegmentDistance);
    public int VirusTrailPaintPriority => _virusTrailPaintPriority;
    public float VirusTrailMinMoveSpeed => Mathf.Max(0f, _virusTrailMinMoveSpeed);
    public float VirusTrailTeleportResetDistance => Mathf.Max(0f, _virusTrailTeleportResetDistance);
    public float VirusTrailStampInterval => Mathf.Max(0f, _virusTrailStampInterval);
    public float VirusTrailMaxPaintSegmentDistance => Mathf.Max(0f, _virusTrailMaxPaintSegmentDistance);
    public float VirusTrailSpacing => Mathf.Max(0f, _virusTrailSpacing);
    public int VirusTrailMaxSteps => Mathf.Max(1, _virusTrailMaxSteps);

    public EnemyAttackSelectionOption[] AttackOptions => _attackOptions;

    public bool HasSelectableAttack(float distance)
    {
        if (_attackOptions == null)
            return false;

        for (int i = 0; i < _attackOptions.Length; i++)
        {
            if (_attackOptions[i] != null && _attackOptions[i].IsSelectable(distance))
                return true;
        }

        return false;
    }

    public bool TryPickAttack(float distance, out EnemyAttackConfigSO attackConfig)
    {
        attackConfig = null;

        if (_attackOptions == null)
            return false;

        int totalWeight = 0;

        for (int i = 0; i < _attackOptions.Length; i++)
        {
            EnemyAttackSelectionOption option = _attackOptions[i];

            if (option != null && option.IsSelectable(distance))
                totalWeight += Mathf.Max(1, option.weight);
        }

        if (totalWeight <= 0)
            return false;

        int roll = UnityEngine.Random.Range(0, totalWeight);

        for (int i = 0; i < _attackOptions.Length; i++)
        {
            EnemyAttackSelectionOption option = _attackOptions[i];

            if (option == null || !option.IsSelectable(distance))
                continue;

            roll -= Mathf.Max(1, option.weight);

            if (roll < 0)
            {
                attackConfig = option.attackConfig;
                return attackConfig != null;
            }
        }

        return false;
    }
}
