/*
 * Role hierarchy: WIP > Legacy > Gameplay > BossReward
 * Primary responsibility: Retains the retired boss choice-reward component so
 * existing prefabs deserialize safely. Active boss rewards are corpse loot and
 * quest rewards; no new gameplay may depend on this component.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[AddComponentMenu("WIP/Legacy/Boss Reward Resolver")]
public class BossRewardResolver : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private NamedSectorController _namedSectorController;

    [Header("Listening")]
    [SerializeField] private BossRewardSelectedEventChannelSO _rewardSelectedChannel;

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        if (_namedSectorController == null)
            _namedSectorController = FindAnyObjectByType<NamedSectorController>();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        if (_namedSectorController == null)
            _namedSectorController = FindAnyObjectByType<NamedSectorController>();
    }

    // Purpose: Intentionally keeps the retired component inert at runtime.
    private void OnEnable()
    {
        enabled = false;
    }

    // Purpose: Retains the former callback surface without registering it.
    private void OnDisable()
    {
    }

    // Purpose: Retains the old callback for serialized compatibility only.
    private void HandleRewardSelected(BossRewardSelection selection)
    {
        Debug.LogWarning(
            "[BossRewardResolver][WIP] Ignored retired choice-reward selection. " +
            "Use corpse loot and QuestDefinitionSO rewards instead.",
            this);
    }

}
