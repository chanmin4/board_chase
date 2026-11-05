/*
 * Role hierarchy: Gameplay > World > Sectors > Content
 * Primary responsibility: Defines the authored content-slot plan for a Normal Sector prefab.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Semantic label for a content anchor. The label is intentionally separate
/// from the prefab candidate so a designer can replace an entire loot or NPC
/// pool without rebuilding the sector prefab.
/// </summary>
public enum SectorContentSlotKind
{
    LootContainer = 0,
    RareCache = 1,
    SpecialInteractable = 2,
    WorldItem = 3 // 이전 enum 직렬화 값은 유지합니다.
}

[Serializable]
public sealed class WeightedSectorContentPrefab
{
    [Tooltip("Prefab instantiated into the matching SectorContentSlotAnchor by the Normal Sector runtime content runner.")]
    public GameObject prefab;

    [Tooltip("Relative selection weight. Zero keeps the candidate authored but disables it.")]
    [Min(0)] public int weight = 1;
    [HideInInspector] public string persistenceKey;
}

[Serializable]
public sealed class SectorContentSlotDefinition
{
    [Tooltip("Stable ID matched against SectorContentSlotAnchor.SlotId on the sector prefab. Keep IDs unique inside this profile.")]
    public string slotId = "content_slot";

    [Tooltip("Must match the semantic kind selected on the matching sector anchor.")]
    public SectorContentSlotKind contentKind = SectorContentSlotKind.LootContainer;

    [Tooltip("Lets a designer keep a slot authored but temporarily exclude it from runtime rolls.")]
    public bool enabled = true;

    [Tooltip("Independent chance that this one anchor receives content. Use a low value for a Rare Cache slot.")]
    [Range(0f, 100f)] public float spawnChancePercent = 100f;

    [Tooltip("확률과 랜덤 생성 상한을 무시하고 항상 생성합니다. 별도 보장 자리입니다.")]
    public bool alwaysSpawn;
    public SectorLootObjectSpawnTableSO lootObjectTable;
    public SectorWorldItemSpawnTableSO worldItemTable;

    [Tooltip("Candidate prefabs for this one anchor. Selection is deterministic when the caller supplies a seeded Random.")]
    public List<WeightedSectorContentPrefab> prefabCandidates = new();

    public bool IsConfigured =>
        enabled &&
        !string.IsNullOrWhiteSpace(slotId) &&
        (contentKind == SectorContentSlotKind.WorldItem ? worldItemTable != null && worldItemTable.HasCandidates :
            lootObjectTable != null ? lootObjectTable.HasCandidates : HasValidCandidate());

    /// <summary>
    /// Pure data roll; it does not instantiate anything. The future sector
    /// content runner owns the seed, collision checks and object lifetime.
    /// </summary>
    public bool TryRollPrefab(System.Random random, out GameObject prefab)
    {
        prefab = null;

        if (!IsConfigured || random == null)
            return false;

        if (!alwaysSpawn && random.NextDouble() * 100d >= Mathf.Clamp(spawnChancePercent, 0f, 100f))
            return false;

        if (lootObjectTable != null)
        {
            var selected = lootObjectTable.Pick(random);
            prefab = selected != null ? selected.lootObject.WorldPrefab : null;
            return prefab != null;
        }

        int totalWeight = 0;
        for (int i = 0; i < prefabCandidates.Count; i++)
        {
            WeightedSectorContentPrefab candidate = prefabCandidates[i];
            if (candidate != null && candidate.prefab != null && candidate.weight > 0)
                totalWeight += candidate.weight;
        }

        if (totalWeight <= 0)
            return false;

        int roll = random.Next(totalWeight);
        for (int i = 0; i < prefabCandidates.Count; i++)
        {
            WeightedSectorContentPrefab candidate = prefabCandidates[i];
            if (candidate == null || candidate.prefab == null || candidate.weight <= 0)
                continue;

            roll -= candidate.weight;
            if (roll < 0)
            {
                prefab = candidate.prefab;
                return true;
            }
        }

        return false;
    }

    // Purpose: Checks whether this component has valid candidate.
    private bool HasValidCandidate()
    {
        if (prefabCandidates == null)
            return false;

        for (int i = 0; i < prefabCandidates.Count; i++)
        {
            WeightedSectorContentPrefab candidate = prefabCandidates[i];
            if (candidate != null && candidate.prefab != null && candidate.weight > 0)
                return true;
        }

        return false;
    }
}

[CreateAssetMenu(
    fileName = "NormalSectorContentProfile",
    menuName = "Game/Sector/Normal Sector Content Profile")]
public sealed class SectorContentProfileSO : ScriptableObject
{
    [Tooltip("새 배치 규칙을 사용하는 방만 켭니다. 기존 방은 자동으로 전환하지 않습니다.")]
    [SerializeField] private bool _useAuthoredLootPlacement;
    [SerializeField] private bool _limitLootObjects;
    [SerializeField, Min(0)] private int _maxLootObjects = 6;
    [SerializeField] private bool _limitWorldItems;
    [SerializeField, Min(0)] private int _maxWorldItems = 8;
    public bool UseAuthoredLootPlacement => _useAuthoredLootPlacement;
    public int MaxLootObjects => _limitLootObjects ? Mathf.Max(0, _maxLootObjects) : int.MaxValue;
    public int MaxWorldItems => _limitWorldItems ? Mathf.Max(0, _maxWorldItems) : int.MaxValue;
    [Tooltip("Each row maps one-to-one to a SectorContentSlotAnchor in the selected Normal Sector prefab. The profile only describes candidates and chance; it never changes the prefab itself.")]
    [SerializeField] private List<SectorContentSlotDefinition> _slotDefinitions = new();

    public IReadOnlyList<SectorContentSlotDefinition> SlotDefinitions => _slotDefinitions;

    /// <summary>
    /// Editor tools use this when a designer paints a new anchor. The row is
    /// intentionally created without a prefab candidate so authoring cannot
    /// accidentally introduce a runtime object before the designer chooses it.
    /// </summary>
    public SectorContentSlotDefinition GetOrCreateSlotDefinition(
        string slotId,
        SectorContentSlotKind contentKind)
    {
        if (string.IsNullOrWhiteSpace(slotId))
            return null;

        if (TryGetSlotDefinition(slotId, out SectorContentSlotDefinition existing))
            return existing;

        _slotDefinitions ??= new List<SectorContentSlotDefinition>();
        SectorContentSlotDefinition created = new()
        {
            slotId = slotId.Trim(),
            contentKind = contentKind,
            enabled = true,
            spawnChancePercent = contentKind == SectorContentSlotKind.RareCache
                ? 5f
                : 100f
        };
        _slotDefinitions.Add(created);
        return created;
    }

    // Purpose: Attempts to get slot definition and reports whether the request succeeded.
    public bool TryGetSlotDefinition(string slotId, out SectorContentSlotDefinition definition)
    {
        definition = null;
        if (string.IsNullOrWhiteSpace(slotId) || _slotDefinitions == null)
            return false;

        for (int i = 0; i < _slotDefinitions.Count; i++)
        {
            SectorContentSlotDefinition candidate = _slotDefinitions[i];
            if (candidate != null && string.Equals(candidate.slotId, slotId, StringComparison.Ordinal))
            {
                definition = candidate;
                return true;
            }
        }

        return false;
    }

    // Purpose: Attempts to roll content for slot and reports whether the request succeeded.
    public bool TryRollContentForSlot(
        string slotId,
        System.Random random,
        out GameObject prefab)
    {
        prefab = null;
        return TryGetSlotDefinition(slotId, out SectorContentSlotDefinition definition) &&
               definition.TryRollPrefab(random, out prefab);
    }
}
