using System;
using UnityEngine;

[Serializable]
public class NamedNormalAttackOption
{
    [Header("Attack")]
    [Tooltip("Unique attack id asset. Use this instead of enum/string.")]
    public NamedAttackIdSO attackId;

    [Tooltip("Only for inspector readability.")]
    public string displayName;

    [Header("Range")]
    [Min(0f)] public float minRange = 0f;
    [Min(0f)] public float maxRange = 5f;

    [Header("Selection")]
    [Min(1)] public int weight = 1;

    [Tooltip("If false, this option is ignored without deleting it.")]
    public bool enabled = true;

    public bool IsValid => enabled && attackId != null && maxRange >= minRange;

    public bool IsInRange(float distance)
    {
        return IsValid &&
               distance >= minRange &&
               distance <= maxRange;
    }
}

[CreateAssetMenu(
    fileName = "NamedNormalAttackConfig",
    menuName = "Named Enemy/Normal Attack Config")]
public class NamedNormalAttackConfigSO : ScriptableObject
{
    [Header("Options")]
    [SerializeField] private NamedNormalAttackOption[] _options;

    [Header("Preferred Positioning Range")]
    [Tooltip("Used by Chase/Reposition decisions, not by attack selection.")]
    [SerializeField, Min(0f)] private float _preferredMinRange = 3f;

    [Tooltip("Used by Chase/Reposition decisions, not by attack selection.")]
    [SerializeField, Min(0f)] private float _preferredMaxRange = 9f;

    public NamedNormalAttackOption[] Options => _options;
    public float PreferredMinRange => _preferredMinRange;
    public float PreferredMaxRange => Mathf.Max(_preferredMinRange, _preferredMaxRange);

    public bool TryPickAttack(float distance, out NamedAttackIdSO attackId)
    {
        attackId = null;

        if (_options == null || _options.Length == 0)
            return false;

        int totalWeight = 0;

        for (int i = 0; i < _options.Length; i++)
        {
            NamedNormalAttackOption option = _options[i];

            if (option == null || !option.IsInRange(distance))
                continue;

            totalWeight += Mathf.Max(1, option.weight);
        }

        if (totalWeight <= 0)
            return false;

        int roll = UnityEngine.Random.Range(0, totalWeight);

        for (int i = 0; i < _options.Length; i++)
        {
            NamedNormalAttackOption option = _options[i];

            if (option == null || !option.IsInRange(distance))
                continue;

            roll -= Mathf.Max(1, option.weight);

            if (roll < 0)
            {
                attackId = option.attackId;
                return attackId != null;
            }
        }

        return false;
    }
}
