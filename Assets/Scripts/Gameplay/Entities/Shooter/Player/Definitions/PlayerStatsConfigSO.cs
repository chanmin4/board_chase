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
    [Header("WIP Legacy Numeric Currency")]
    [Tooltip("Retained only to deserialize old stats assets. New runs start with physical loadout items, not a numerical wallet.")]
    [FormerlySerializedAs("startingRunCurrency")]
    [SerializeField, HideInInspector] private int _wipLegacyStartingRunCurrency;

    [Header("Dash")]
    [Min(0f)] [SerializeField] private float dashCooldownSeconds = 3f;

    [Header("Player Survival")]
    [Range(0f, 1f)] [SerializeField] private float dodgeChance = 0f;

    [Header("Emergency Melee")]
    [Tooltip("Fixed HP damage for fists and empty-gun swings. Ignores armor and damage-reduction multipliers; independent of gun/bullet damage.")]
    [Min(0f)] [SerializeField] private float _meleeDamage = 5f;
    public float MeleeDamage => Mathf.Max(0f, _meleeDamage);

    [Header("Interaction")]
    [SerializeField] private PlayerInteractConfigSO _interactConfig;

    public float DashCooldownSeconds => Mathf.Max(0f, dashCooldownSeconds);
    public float DodgeChance => Mathf.Clamp01(dodgeChance);
    public PlayerInteractConfigSO InteractConfig => _interactConfig;

    // Purpose: Attempts to roll player loadout and reports whether the request succeeded.
    public bool TryRollPlayerLoadout(out ShooterLoadoutSnapshot snapshot)
    {
        return TryRollLoadout(out snapshot);
    }
}
