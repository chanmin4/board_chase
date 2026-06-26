using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class NamedNormalAttackOption
{
    [Header("Attack")]
    [Tooltip("Attack config asset. The asset reference itself is used as the selected attack identity.")]
    [FormerlySerializedAs("attackId")]
    public EnemyAttackConfigSO attackConfig;

    [Header("Selection")]
    [Min(1)] public int weight = 1;

    [Tooltip("If false, this option is ignored without deleting it.")]
    public bool enabled = true;

    public bool IsValid => enabled && attackConfig != null;

    public bool IsSelectable(float distance)
    {
        return IsValid && attackConfig.IsInActivationRange(distance);
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
    [Tooltip("If true, attack selection is blocked until target distance is inside Preferred Min/Max Range.")]
    [SerializeField] private bool _requirePreferredRangeBeforeSelection = true;

    [SerializeField, Min(0f)] private float _preferredMinRange = 3f;
    [SerializeField, Min(0f)] private float _preferredMaxRange = 9f;

    public NamedNormalAttackOption[] Options => _options;
    public bool RequirePreferredRangeBeforeSelection => _requirePreferredRangeBeforeSelection;
    public float PreferredMinRange => _preferredMinRange;
    public float PreferredMaxRange => Mathf.Max(_preferredMinRange, _preferredMaxRange);

    public bool IsInPreferredRange(float distance)
    {
        return distance >= PreferredMinRange && distance <= PreferredMaxRange;
    }

    public bool TryPickAttack(float distance, out EnemyAttackConfigSO attackConfig)
    {
        attackConfig = null;

        if (_requirePreferredRangeBeforeSelection && !IsInPreferredRange(distance))
            return false;

        if (_options == null || _options.Length == 0)
            return false;

        int totalWeight = 0;

        for (int i = 0; i < _options.Length; i++)
        {
            NamedNormalAttackOption option = _options[i];

            if (option == null || !option.IsSelectable(distance))
                continue;

            totalWeight += Mathf.Max(1, option.weight);
        }

        if (totalWeight <= 0)
            return false;

        int roll = UnityEngine.Random.Range(0, totalWeight);

        for (int i = 0; i < _options.Length; i++)
        {
            NamedNormalAttackOption option = _options[i];

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
