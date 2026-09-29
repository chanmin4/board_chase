/*
 * Role hierarchy: Gameplay > Entities > Creature > NamedEnemy > ScriptableObject
 * Primary responsibility: Defines the BossRewardOptionSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "BossRewardOption",
    menuName = "Boss Reward/Reward Option")]
public class BossRewardOptionSO : ScriptableObject
{
    [Header("Display")]
    [SerializeField] private string _title;
    [TextArea]
    [SerializeField] private string _description;
    [SerializeField] private Sprite _icon;

    [Header("Debug")]
    [SerializeField] private string _rewardId;

    public string Title => _title;
    public string Description => _description;
    public Sprite Icon => _icon;
    public string RewardId => string.IsNullOrWhiteSpace(_rewardId) ? name : _rewardId;
}
