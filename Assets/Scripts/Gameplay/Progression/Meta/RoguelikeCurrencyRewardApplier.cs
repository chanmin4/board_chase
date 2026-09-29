/*
 * Role hierarchy: Gameplay > Progression > Meta
 * Primary responsibility: Implements the focused RoguelikeCurrencyRewardApplier responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public class RoguelikeCurrencyRewardApplier : MonoBehaviour
{
    [Header("WIP Legacy System")]
    [Tooltip("Retired numerical reward system. Enemy and stage rewards belong in physical loot/currency items, not permanent meta currency.")]
    [SerializeField, HideInInspector] private bool _enableWipRoguelikeRewards;

    // Purpose: Keeps the old component inert so existing prefabs remain valid.
    private void OnEnable()
    {
        _enableWipRoguelikeRewards = false;
    }

    // Purpose: Retains the old reset API without tracking retired numeric rewards.
    public void ResetRunRewardHistory()
    {
    }

    // Purpose: Retains the old mid-boss API without awarding retired numeric currency.
    public void GrantMidBossDefeatedReward()
    {
    }

    // Purpose: Retains the old final-boss API without awarding retired numeric currency.
    public void GrantFinalBossDefeatedReward()
    {
    }
}
