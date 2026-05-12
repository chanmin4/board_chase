using System;
using UnityEngine;

[Serializable]
public struct NamedNormalAttackOption
{
    public NamedEnemyAttackType attackType;

    [Min(0f)] public float minRange;
    [Min(0f)] public float maxRange;

    [Min(1)] public int weight;

    public bool IsValid => attackType != NamedEnemyAttackType.None && weight > 0;

    public bool IsInRange(float distance)
    {
        float min = Mathf.Min(minRange, maxRange);
        float max = Mathf.Max(minRange, maxRange);
        return distance >= min && distance <= max;
    }
}

[CreateAssetMenu(
    fileName = "NamedNormalAttackConfig",
    menuName = "Game/Named Enemy/Normal Attack Config")]
public class NamedNormalAttackConfigSO : ScriptableObject
{
    [Header("Timing")]
    [SerializeField, Min(0f)] private float _normalAttackCooldown = 2f;

    [Header("Attacks")]
    [SerializeField] private NamedNormalAttackOption[] _attacks =
    {
        new NamedNormalAttackOption
        {
            attackType = NamedEnemyAttackType.Charge,
            minRange = 4f,
            maxRange = 10f,
            weight = 1
        },
        new NamedNormalAttackOption
        {
            attackType = NamedEnemyAttackType.Projectile,
            minRange = 5f,
            maxRange = 14f,
            weight = 1
        },
        new NamedNormalAttackOption
        {
            attackType = NamedEnemyAttackType.PoisonPuddle,
            minRange = 2f,
            maxRange = 8f,
            weight = 1
        }
    };

    public float NormalAttackCooldown => _normalAttackCooldown;

    public bool HasAnyAttackInRange(float distance)
    {
        if (_attacks == null)
            return false;

        for (int i = 0; i < _attacks.Length; i++)
        {
            if (_attacks[i].IsValid && _attacks[i].IsInRange(distance))
                return true;
        }

        return false;
    }

    public bool TryPickAttack(float distance, out NamedEnemyAttackType selectedAttack)
    {
        selectedAttack = NamedEnemyAttackType.None;

        if (_attacks == null || _attacks.Length == 0)
            return false;

        int totalWeight = 0;

        for (int i = 0; i < _attacks.Length; i++)
        {
            if (!_attacks[i].IsValid || !_attacks[i].IsInRange(distance))
                continue;

            totalWeight += Mathf.Max(1, _attacks[i].weight);
        }

        if (totalWeight <= 0)
            return false;

        int roll = UnityEngine.Random.Range(0, totalWeight);

        for (int i = 0; i < _attacks.Length; i++)
        {
            if (!_attacks[i].IsValid || !_attacks[i].IsInRange(distance))
                continue;

            roll -= Mathf.Max(1, _attacks[i].weight);

            if (roll < 0)
            {
                selectedAttack = _attacks[i].attackType;
                return true;
            }
        }

        return false;
    }
}
