/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player > Definitions
 * Primary responsibility: Defines the PlayerStatsConfigSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(
    fileName = "PlayerStatsConfig",
    menuName = "VSplatter/Player/Stats Config")]
public class PlayerStatsConfigSO : ShooterStatConfigSO
{
    [SerializeField, Min(0f)] private float _baseRangedAttackDamage = 10f;
    public float BaseRangedAttackDamage => Mathf.Max(0f, _baseRangedAttackDamage);
    [Tooltip("부하 100%에 해당하는 기준 무게입니다. 초과 소지를 막지 않습니다.")]
    [SerializeField, Min(.1f)] private float _carryCapacityKg = 70f;
    public float CarryCapacityKg => _carryCapacityKg > 0f && !float.IsInfinity(_carryCapacityKg) ? _carryCapacityKg : 70f;
    [Header("WIP Legacy Numeric Currency")]
    [Tooltip("Retained only to deserialize old stats assets. New runs start with physical loadout items, not a numerical wallet.")]
    [FormerlySerializedAs("startingRunCurrency")]
    [SerializeField, HideInInspector] private int _wipLegacyStartingRunCurrency;

    // Legacy fields are retained solely for old serialized assets.
    [HideInInspector, Min(0f)] [SerializeField] private float dashCooldownSeconds = 3f;

    [Header("Player Survival")]
    [Range(0f, 1f)] [SerializeField] private float dodgeChance = 0f;

    [HideInInspector, Min(0f)] [SerializeField] private float _meleeDamage = 5f;
    public float MeleeDamage => 0f; // WIP fists; active melee damage belongs to MeleeWeaponSO.

    [Header("Interaction")]
    [SerializeField] private PlayerInteractConfigSO _interactConfig;

    public float DodgeChance => Mathf.Clamp01(dodgeChance);
    public PlayerInteractConfigSO InteractConfig => _interactConfig;

    // Purpose: Attempts to roll player loadout and reports whether the request succeeded.
    public bool TryRollPlayerLoadout(out ShooterLoadoutSnapshot snapshot)
    {
        return TryRollLoadout(out snapshot);
    }
}
