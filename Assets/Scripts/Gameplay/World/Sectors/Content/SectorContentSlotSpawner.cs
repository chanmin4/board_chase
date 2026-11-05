/*
 * Role hierarchy: Gameplay > World > Sectors > Content
 * Primary responsibility: Deterministically populates authored sector content slots for one generated Normal Sector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime-only marker added to content instances. It lets the spawner safely
/// remove only generated slot content when the same sector is populated again;
/// fixed authored layout children are never touched.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("")]
public sealed class SectorContentRuntimeInstance : MonoBehaviour
{
    public string SlotId { get; private set; }
    private WorldItemPickup _pickup;
    private RaidSectorContentSpawnSaveData _saved;

    // Purpose: Initializes initialize for use by this component.
    public void Initialize(string slotId)
    {
        SlotId = slotId;
    }

    public void BindPickup(WorldItemPickup pickup, RaidSectorContentSpawnSaveData saved)
    {
        if (_pickup != null) _pickup.PickedUp -= OnPickedUp;
        _pickup = pickup; _saved = saved;
        if (_pickup != null) _pickup.PickedUp += OnPickedUp;
    }
    private void OnPickedUp(WorldItemPickup pickup)
    {
        if (_saved != null) _saved.pickedUp = true;
        // Inventory mutation notifications precede PickedUp. Save again with the final pickup flag.
        var raid = RaidRuntime.Active;
        if (Application.isPlaying && raid != null && raid.State != null && raid.State.active &&
            raid.State.sectorContent?.Exists(room => room?.spawns != null && room.spawns.Contains(_saved)) == true)
        {
            raid.Capture(SaveSystem.Active?.RunData);
            SaveSystem.Active?.TrySaveRunDataToDisk();
        }
    }
    private void OnDestroy() { if (_pickup != null) _pickup.PickedUp -= OnPickedUp; }
}

/// <summary>
/// Uses the generated stage seed, sector coordinate and stable slot ID to make
/// a slot roll reproducible. It does not alter profile assets or prefab assets.
/// </summary>
public static class SectorContentSlotSpawner
{
    private static readonly List<SectorContentSlotAnchor> SlotBuffer = new();

    /// <summary>
    /// Populates every configured anchor in this sector. Missing profiles,
    /// rows and candidates are optional authoring states and simply produce no
    /// object. A profile/anchor kind mismatch is skipped with a targeted log.
    /// </summary>
    public static int Populate(
        SectorRuntime sector,
        SectorContentProfileSO profile,
        int stageSeed,
        RaidRunSaveData raidState = null)
    {
        if (sector == null)
            return 0;
        var population = sector.GetComponent<SectorContentPopulationState>();
        if (population != null && population.Initialized) return 0;
        var saved = raidState?.sectorContent?.Find(room => room != null && room.coord == sector.Coord)
            ?? population?.Snapshot;
        if (saved == null && profile == null) return 0;
        if (population == null) population = sector.gameObject.AddComponent<SectorContentPopulationState>();
        sector.CollectContentSlotAnchors(SlotBuffer);
        SlotBuffer.Sort(CompareAnchors);
        var choices = new List<ResolvedSpawn>();
        if (saved == null)
        {
            saved = new RaidSectorContentSaveData { coord = sector.Coord, authoredLootPlacement = profile.UseAuthoredLootPlacement };
            var processed = new HashSet<string>(StringComparer.Ordinal);
            var randomBoxes = new List<ResolvedSpawn>();
            var randomItems = new List<ResolvedSpawn>();
            foreach (var anchor in SlotBuffer)
            {
                if (anchor == null || !anchor.EnabledForRuntime || string.IsNullOrWhiteSpace(anchor.SlotId)) continue;
                if (!processed.Add(anchor.SlotId))
                { Debug.LogWarning($"[SectorContentSlotSpawner] Duplicate slot skipped: {sector.name}/{anchor.SlotId}", anchor); continue; }
                if (!profile.TryGetSlotDefinition(anchor.SlotId, out var definition) || definition.contentKind != anchor.ContentKind)
                    continue;
                var random = new System.Random(BuildSlotSeed(stageSeed, sector.Coord, anchor.SlotId));
                var choice = RollSlot(sector, anchor, definition, random);
                if (choice == null) continue;
                if (definition.alwaysSpawn || definition.contentKind == SectorContentSlotKind.SpecialInteractable) choices.Add(choice);
                else if (definition.contentKind == SectorContentSlotKind.WorldItem) randomItems.Add(choice);
                else randomBoxes.Add(choice);
            }
            // 성공한 자리들 중 무작위로 잘라야 목록 앞자리 우선 편향이 없습니다.
            TakeCapped(randomBoxes, profile.MaxLootObjects, new System.Random(BuildSlotSeed(stageSeed, sector.Coord, "__box_cap")), choices);
            TakeCapped(randomItems, profile.MaxWorldItems, new System.Random(BuildSlotSeed(stageSeed, sector.Coord, "__item_cap")), choices);
            choices.Sort((a, b) => string.CompareOrdinal(a.saved.slotId, b.saved.slotId));
            foreach (var choice in choices) saved.spawns.Add(choice.saved);
            if (raidState != null)
            {
                raidState.sectorContent ??= new();
                raidState.sectorContent.Add(saved);
            }
        }
        else
        {
            var catalog = Resources.Load<RuntimeContentCatalogSO>("RuntimeContentCatalog");
            if (saved.spawns != null) foreach (var entry in saved.spawns)
            {
                if (entry == null || entry.pickedUp) continue;
                var choice = new ResolvedSpawn { saved = entry };
                if (entry.kind == SectorContentSlotKind.WorldItem)
                {
                    if (catalog == null || !catalog.TryResolveItem(entry.itemGuid, out choice.item))
                    { WarnMissing(sector, entry); continue; }
                }
                else if (catalog == null || !catalog.TryResolveSectorSpawn(entry.prefabKey, out choice.prefab, out choice.lootObject))
                { WarnMissing(sector, entry); continue; }
                choices.Add(choice);
            }
        }
        population.Snapshot = saved;
        population.Initialized = true;
        int spawnedCount = 0;
        foreach (var choice in choices) if (Spawn(sector, choice)) spawnedCount++;
        SlotBuffer.Clear();
        return spawnedCount;
    }

    private sealed class ResolvedSpawn
    {
        public RaidSectorContentSpawnSaveData saved;
        public GameObject prefab;
        public LootObjectSO lootObject;
        public ItemSO item;
    }

    private static ResolvedSpawn RollSlot(SectorRuntime sector, SectorContentSlotAnchor anchor,
        SectorContentSlotDefinition definition, System.Random random)
    {
        if (!definition.IsConfigured) return null;
        var choice = new ResolvedSpawn();
        var saved = new RaidSectorContentSpawnSaveData { slotId = anchor.SlotId, kind = anchor.ContentKind,
            localPosition = sector.transform.InverseTransformPoint(anchor.SpawnParent.position),
            localRotation = Quaternion.Inverse(sector.transform.rotation) * anchor.SpawnParent.rotation };
        choice.saved = saved;
        if (definition.contentKind == SectorContentSlotKind.WorldItem || definition.lootObjectTable != null)
        {
            if (!definition.alwaysSpawn && random.NextDouble() * 100d >= Mathf.Clamp(definition.spawnChancePercent, 0, 100)) return null;
            if (definition.contentKind == SectorContentSlotKind.WorldItem)
            {
                var row = definition.worldItemTable.Pick(random);
                if (row == null) return null;
                saved.amount = row.RollAmount(random);
                if (PassiveInstanceRuntime.NeedsRoll(row.item) && saved.amount != 1)
                {
                    Debug.LogError("[Sector Content] 랜덤 패시브 바닥 배치는 개체 보존을 위해 자리당 수량 1을 사용하세요.");
                    return null;
                }
                choice.item = PassiveInstanceRuntime.Create(row.item, random);
                if (choice.item == null) return null;
                saved.itemGuid = choice.item.Guid;
                float condition = row.RollCondition(random);
                if (row.item is ArmorItemSO armor) { saved.armorMaxDurability = armor.MaxDurability; saved.armorDurability = armor.MaxDurability * condition; }
                if (row.item is RangedWeaponSO weapon) { saved.weaponMaxDurability = weapon.MaxDurability; saved.weaponDurability = weapon.MaxDurability * condition; }
                if (row.item is ConsumableItemSO medical && medical.UsesDurability) { saved.weaponMaxDurability = medical.MaxDurability; saved.weaponDurability = medical.MaxDurability * condition; }
                if (row.item is GasMaskItemSO mask) { saved.filterMaxDurability = mask.MaxFilterDurability; saved.filterDurability = mask.MaxFilterDurability * condition; }
            }
            else
            {
                var row = definition.lootObjectTable.Pick(random);
                if (row == null) return null;
                choice.lootObject = row.lootObject; choice.prefab = row.lootObject.WorldPrefab; saved.prefabKey = row.persistenceKey;
            }
        }
        else
        {
            // 기존 프로필은 기존 seed/정수 가중치 추첨을 그대로 사용합니다.
            if (!definition.TryRollPrefab(random, out choice.prefab)) return null;
            var selected = definition.prefabCandidates.Find(row => row != null && row.prefab == choice.prefab && row.weight > 0);
            saved.prefabKey = selected?.persistenceKey;
        }
        Vector3 worldScale = Vector3.Scale(anchor.SpawnParent.lossyScale, choice.prefab != null ? choice.prefab.transform.localScale : Vector3.one);
        Vector3 sectorScale = sector.transform.lossyScale;
        saved.localScale = new Vector3(worldScale.x / Mathf.Max(0.0001f, sectorScale.x),
            worldScale.y / Mathf.Max(0.0001f, sectorScale.y), worldScale.z / Mathf.Max(0.0001f, sectorScale.z));
        return choice;
    }

    private static void TakeCapped(List<ResolvedSpawn> candidates, int limit, System.Random random, List<ResolvedSpawn> result)
    {
        if (candidates.Count > limit)
            for (int i = candidates.Count - 1; i > 0; i--)
            { int other = random.Next(i + 1); (candidates[i], candidates[other]) = (candidates[other], candidates[i]); }
        for (int i = 0; i < Mathf.Min(limit, candidates.Count); i++) result.Add(candidates[i]);
    }

    private static bool Spawn(SectorRuntime sector, ResolvedSpawn choice)
    {
        var saved = choice.saved;
        GameObject instance;
        WorldItemPickup pickup = null;
        if (saved.kind == SectorContentSlotKind.WorldItem)
        {
            if (saved.pickedUp || !WorldItemPickupSpawner.TrySpawnPlaced(sector, choice.item, saved, out pickup))
            { if (!saved.pickedUp) Debug.LogError($"[SectorContentSlotSpawner] World item spawn failed: {sector.name}/{saved.slotId}", sector); return false; }
            instance = pickup.gameObject;
            Vector3 desiredScale = Vector3.Scale(saved.localScale, sector.transform.lossyScale);
            Vector3 ownerScale = instance.transform.parent != null ? instance.transform.parent.lossyScale : Vector3.one;
            instance.transform.localScale = new Vector3(desiredScale.x / Mathf.Max(0.0001f, ownerScale.x),
                desiredScale.y / Mathf.Max(0.0001f, ownerScale.y), desiredScale.z / Mathf.Max(0.0001f, ownerScale.z));
        }
        else
        {
            if (choice.prefab == null) return false;
            var anchor = SlotBuffer.Find(row => row != null && row.SlotId == saved.slotId);
            Transform parent = anchor != null ? anchor.SpawnParent : sector.transform;
            instance = UnityEngine.Object.Instantiate(choice.prefab, parent);
            instance.transform.SetPositionAndRotation(sector.transform.TransformPoint(saved.localPosition), sector.transform.rotation * saved.localRotation);
            Vector3 parentScale = parent.lossyScale, sectorScale = sector.transform.lossyScale;
            Vector3 worldScale = Vector3.Scale(saved.localScale, sectorScale);
            instance.transform.localScale = new Vector3(worldScale.x / Mathf.Max(0.0001f, parentScale.x),
                worldScale.y / Mathf.Max(0.0001f, parentScale.y), worldScale.z / Mathf.Max(0.0001f, parentScale.z));
            if (choice.lootObject != null) instance.GetComponentInChildren<LootableObject>(true)?.ConfigureRuntimeSource(choice.lootObject);
        }
        instance.name = $"{(choice.prefab != null ? choice.prefab.name : choice.item.name)} [Slot:{saved.slotId}]";
        var marker = instance.GetComponent<SectorContentRuntimeInstance>() ?? instance.AddComponent<SectorContentRuntimeInstance>();
        marker.Initialize(saved.slotId);
        if (pickup != null) { pickup.SetDiscoveryEligibility(true); marker.BindPickup(pickup, saved); }
        return true;
    }

    private static void WarnMissing(SectorRuntime sector, RaidSectorContentSpawnSaveData saved) =>
        Debug.LogError($"[SectorContentSlotSpawner] Saved content reference missing; NOT rerolled. sector={sector.name}, slot={saved.slotId}, prefab={saved.prefabKey}, item={saved.itemGuid}", sector);

    /// <summary>
    /// Removes only objects created by this spawner. Generated sector teardown
    /// already destroys all children, but this keeps re-population safe for
    /// debug rebuilds and future reroll operations.
    /// </summary>
    public static void ClearGeneratedContent(SectorRuntime sector)
    {
        if (sector == null)
            return;

        SectorContentRuntimeInstance[] instances =
            sector.GetComponentsInChildren<SectorContentRuntimeInstance>(true);

        for (int i = instances.Length - 1; i >= 0; i--)
        {
            SectorContentRuntimeInstance instance = instances[i];
            if (instance == null)
                continue;

            if (Application.isPlaying)
            {
                // 지연 Destroy가 끝나기 전 중복 상호작용/콜라이더가 남지 않게 먼저 끕니다.
                instance.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(instance.gameObject);
            }
            else
                UnityEngine.Object.DestroyImmediate(instance.gameObject);
        }

        // 다시 채울 때도 기존 추첨 결과를 복원합니다. 제거가 새 추첨을 뜻하지 않습니다.
        var population = sector.GetComponent<SectorContentPopulationState>();
        if (population != null) population.Initialized = false;
    }

    // Purpose: Performs the compare anchors workflow for this component.
    private static int CompareAnchors(
        SectorContentSlotAnchor left,
        SectorContentSlotAnchor right)
    {
        if (ReferenceEquals(left, right))
            return 0;
        if (left == null)
            return -1;
        if (right == null)
            return 1;

        return string.Compare(left.SlotId, right.SlotId, StringComparison.Ordinal);
    }

    // Purpose: Builds the slot seed from the current authored data.
    private static int BuildSlotSeed(int stageSeed, Vector2Int sectorCoord, string slotId)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + stageSeed;
            hash = hash * 31 + sectorCoord.x;
            hash = hash * 31 + sectorCoord.y;

            if (!string.IsNullOrEmpty(slotId))
            {
                for (int i = 0; i < slotId.Length; i++)
                    hash = hash * 31 + slotId[i];
            }

            return hash;
        }
    }
}
