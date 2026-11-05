/*
 * Role hierarchy: Authoring > Editor > Validation
 * Primary responsibility: Runs a read-only project-content preflight before Play mode.
 * Execution boundary: Unity Editor only (Assembly-CSharp-Editor)
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public enum ContentPreflightSeverity
{
    Error,
    Warning,
    Info
}

public readonly struct ContentPreflightIssue
{
    public ContentPreflightIssue(
        ContentPreflightSeverity severity,
        string category,
        string message,
        UnityEngine.Object context)
    {
        Severity = severity;
        Category = category;
        Message = message;
        Context = context;
    }

    public ContentPreflightSeverity Severity { get; }
    public string Category { get; }
    public string Message { get; }
    public UnityEngine.Object Context { get; }
}

/// <summary>
/// Read-only content verification for the project-owned data that can stop a run
/// from starting correctly. It intentionally reports problems only: it does not
/// normalize authored loot data, generate assets, or rewrite any ScriptableObject.
/// </summary>
public static class ContentPreflightValidator
{
    private const int ValidationSeed = 104729;

    // Purpose: Collects issues for this workflow.
    public static List<ContentPreflightIssue> CollectIssues()
    {
        List<ContentPreflightIssue> issues = new();
        HashSet<ItemSO> inspectedItems = new();

        List<StageDefinitionSO> stages = FindAssets<StageDefinitionSO>();
        ValidateStageRunFlows(issues);
        ValidateStages(stages, issues);
        ValidateSectorContentSlots(issues);
        ValidateLootProgression(issues, inspectedItems);
        ValidateShops(issues, inspectedItems);
        ValidateQuests(issues, inspectedItems);
        ValidateAudioCues(issues);
        ValidateBulletPrefabs(inspectedItems, issues);
        ValidateEnemyPrefabs(issues);

        return issues;
    }

    // Purpose: Validates stage run flows and reports invalid authored data.
    private static void ValidateStageRunFlows(List<ContentPreflightIssue> issues)
    {
        List<StageRunFlowSO> flows = FindAssets<StageRunFlowSO>();
        if (flows.Count == 0)
        {
            AddWarning(issues, "Stages", "No StageRunFlowSO asset was found. Stage progression cannot resolve the authored run order.", null);
            return;
        }

        for (int flowIndex = 0; flowIndex < flows.Count; flowIndex++)
        {
            StageRunFlowSO flow = flows[flowIndex];
            HashSet<int> seenStageIndices = new();

            if (flow.Stages == null || flow.Stages.Count == 0)
            {
                AddError(issues, "Stages", "Stage run flow has no registered stages.", flow);
                continue;
            }

            for (int i = 0; i < flow.Stages.Count; i++)
            {
                StageDefinitionSO stage = flow.Stages[i];
                if (stage == null)
                {
                    AddError(issues, "Stages", $"Run flow entry {i} is missing its StageDefinitionSO reference.", flow);
                    continue;
                }

                if (stage.StageIndex < 1)
                {
                    AddError(issues, "Stages", $"Stage '{stage.name}' uses index {stage.StageIndex}. Playable stages must start at index 1.", stage);
                }

                if (!seenStageIndices.Add(stage.StageIndex))
                {
                    AddError(issues, "Stages", $"Run flow contains duplicate stage index {stage.StageIndex}.", flow);
                }
            }
        }
    }

    // Purpose: Validates stages and reports invalid authored data.
    private static void ValidateStages(
        IReadOnlyList<StageDefinitionSO> stages,
        List<ContentPreflightIssue> issues)
    {
        for (int i = 0; i < stages.Count; i++)
        {
            StageDefinitionSO stage = stages[i];
            if (stage == null)
                continue;

            if (!stage.TryPickSectorPreset(
                    ValidationSeed,
                    Vector2Int.zero,
                    StageRoomType.Start,
                    out _))
            {
                AddError(issues, "Stages", "No valid weighted Start Sector preset can be selected.", stage);
            }

            if (!stage.UseStartSectorOnly)
            {
                if (!stage.TryPickSectorPreset(
                        ValidationSeed,
                        Vector2Int.one,
                        StageRoomType.Normal,
                        out _))
                {
                    AddError(issues, "Stages", "No valid weighted Normal Sector preset can be selected.", stage);
                }

                if (stage.HasNormalSectorThatUsesEnemyEncounter() &&
                    !stage.TryPickNormalSectorEncounter(
                        ValidationSeed,
                        new Vector2Int(2, 1),
                        out _))
                {
                    AddError(issues, "Stages", "No valid weighted Normal Sector encounter can be selected.", stage);
                }
            }

            if (stage.MapPolicy == StageMapPolicy.RadialBraidedRaid)
            {
                if (!stage.UseStartSectorOnly && stage.RoomGridSize > 1 && stage.RaidMap.bossSpawnPercent > 0)
                {
                    var bossCoord = new Vector2Int(2, 2);
                    if (!stage.TryPickBossEncounter(ValidationSeed, bossCoord, out _))
                        AddError(issues, "Stages", "No valid V3 Boss encounter can be selected.", stage);
                    var bossRoom = new StageRoomNode(bossCoord, StageRoomType.Boss, StageRoomConnectionFlags.None) { combatPoolId = "theme" };
                    if (!stage.RoomComposition.TryResolve(stage, new StageMapLayout { stageSeed = ValidationSeed }, bossRoom, out _))
                        AddError(issues, "Stages", "V3 보스용 스테이지 전용맵 풀이 없습니다. Normal 전용맵 프리셋을 등록하세요.", stage);
                }
            }
            else if (!stage.TryPickBossBattle(
                    ValidationSeed,
                    new Vector2Int(2, 2),
                    out _,
                    out _))
            {
                AddError(issues, "Stages", "No valid Boss battle pair can be selected. Every boss entry needs both a valid encounter and a valid Boss sector preset.", stage);
            }

            if (stage.MapPolicy == StageMapPolicy.OddSquareRaid)
            {
                if (!RaidMapGenerator.TryGenerate(ValidationSeed, stage.StageIndex, stage.RoomGridSize, Vector2Int.left,
                        stage.RaidMap, out _, out string raidError)) AddError(issues, "Stages", raidError, stage);
                if (stage.RaidRuntime.generatorBattery == null)
                    AddError(issues, "Stages", "Raid generator has no Battery ItemSO.", stage);
            }
            else if (!stage.TryValidateBranchTopologyPool(out string topologyError))
            {
                AddError(
                    issues,
                    "Stages",
                    $"Branch Topology is invalid: {topologyError}",
                    stage);
            }

        }
    }

    /// <summary>
    /// Validates the bridge between authored Normal Sector prefabs and their
    /// deterministic content plans. The runner deliberately skips incomplete
    /// rows at runtime so a work-in-progress room can still be opened in the
    /// editor; Preflight is where those silent skips become actionable.
    /// </summary>
    private static void ValidateSectorContentSlots(List<ContentPreflightIssue> issues)
    {
        List<SectorPresetSO> presets = FindAssets<SectorPresetSO>();

        for (int presetIndex = 0; presetIndex < presets.Count; presetIndex++)
        {
            SectorPresetSO preset = presets[presetIndex];
            if (preset == null || preset.ContentProfile == null)
                continue;

            if (preset.RoomType != StageRoomType.Normal)
            {
                AddWarning(
                    issues,
                    "Sector Content",
                    $"Sector preset '{preset.name}' has a Content Profile, but only Normal Sector presets populate content slots at runtime.",
                    preset);
                continue;
            }

            if (preset.SectorPrefab == null)
            {
                AddError(
                    issues,
                    "Sector Content",
                    $"Normal Sector preset '{preset.name}' has a Content Profile but no SectorRuntime prefab.",
                    preset);
                continue;
            }

            ValidateSectorContentProfile(preset, preset.ContentProfile, issues);
        }
    }

    // Purpose: Validates sector content profile and reports invalid authored data.
    private static void ValidateSectorContentProfile(
        SectorPresetSO preset,
        SectorContentProfileSO profile,
        List<ContentPreflightIssue> issues)
    {
        SectorContentSlotAnchor[] anchors = preset.SectorPrefab.GetComponentsInChildren<SectorContentSlotAnchor>(true);
        Dictionary<string, SectorContentSlotAnchor> anchorById = new(StringComparer.Ordinal);
        HashSet<string> duplicateAnchorIds = new(StringComparer.Ordinal);

        for (int anchorIndex = 0; anchorIndex < anchors.Length; anchorIndex++)
        {
            SectorContentSlotAnchor anchor = anchors[anchorIndex];
            if (anchor == null)
                continue;

            string slotId = anchor.SlotId;
            if (string.IsNullOrWhiteSpace(slotId))
            {
                AddError(
                    issues,
                    "Sector Content",
                    $"Normal Sector prefab '{preset.SectorPrefab.name}' has a content anchor with an empty Slot Id.",
                    anchor);
                continue;
            }

            if (!anchorById.TryAdd(slotId, anchor))
            {
                if (duplicateAnchorIds.Add(slotId))
                {
                    AddError(
                        issues,
                        "Sector Content",
                        $"Normal Sector prefab '{preset.SectorPrefab.name}' has duplicate content anchor Slot Id '{slotId}'. Each runtime spawn location must be unique.",
                        anchor);
                }

                continue;
            }
        }

        IReadOnlyList<SectorContentSlotDefinition> definitions = profile.SlotDefinitions;
        if (definitions == null || definitions.Count == 0)
        {
            if (anchors.Length > 0)
            {
                AddError(
                    issues,
                    "Sector Content",
                    $"Content Profile '{profile.name}' is assigned to '{preset.name}' but has no slot definitions for its {anchors.Length} anchor(s).",
                    profile);
            }

            return;
        }

        Dictionary<string, SectorContentSlotDefinition> definitionById = new(StringComparer.Ordinal);
        for (int definitionIndex = 0; definitionIndex < definitions.Count; definitionIndex++)
        {
            SectorContentSlotDefinition definition = definitions[definitionIndex];
            if (definition == null)
            {
                AddWarning(
                    issues,
                    "Sector Content",
                    $"Content Profile '{profile.name}' contains an empty slot definition at index {definitionIndex}.",
                    profile);
                continue;
            }

            string slotId = definition.slotId;
            if (string.IsNullOrWhiteSpace(slotId))
            {
                AddError(
                    issues,
                    "Sector Content",
                    $"Content Profile '{profile.name}' has a slot definition with an empty Slot Id.",
                    profile);
                continue;
            }

            if (!definitionById.TryAdd(slotId, definition))
            {
                AddError(
                    issues,
                    "Sector Content",
                    $"Content Profile '{profile.name}' has duplicate slot definition '{slotId}'.",
                    profile);
                continue;
            }

            ValidateSectorContentCandidates(profile, definition, issues);

            if (definition.enabled && HasPositiveContentCandidate(definition) && !anchorById.TryGetValue(slotId, out _))
            {
                AddError(
                    issues,
                    "Sector Content",
                    $"Content Profile '{profile.name}' configures slot '{slotId}', but Normal Sector prefab '{preset.SectorPrefab.name}' has no matching anchor.",
                    profile);
            }
        }

        foreach (KeyValuePair<string, SectorContentSlotAnchor> pair in anchorById)
        {
            string slotId = pair.Key;
            SectorContentSlotAnchor anchor = pair.Value;
            if (!definitionById.TryGetValue(slotId, out SectorContentSlotDefinition definition))
            {
                if (anchor.EnabledForRuntime)
                {
                    AddError(
                        issues,
                        "Sector Content",
                        $"Normal Sector prefab '{preset.SectorPrefab.name}' has enabled anchor '{slotId}', but Content Profile '{profile.name}' has no matching definition.",
                        anchor);
                }

                continue;
            }

            if (definition.contentKind != anchor.ContentKind)
            {
                AddError(
                    issues,
                    "Sector Content",
                    $"Content slot '{slotId}' has conflicting kinds: profile is {definition.contentKind}, anchor is {anchor.ContentKind}.",
                    anchor);
            }

            if (anchor.EnabledForRuntime && definition.enabled && !HasPositiveContentCandidate(definition))
            {
                AddWarning(
                    issues,
                    "Sector Content",
                    $"Content slot '{slotId}' is enabled but has no positive-weight prefab candidate, so it will spawn nothing.",
                    profile);
            }
        }
    }

    // Purpose: Validates sector content candidates and reports invalid authored data.
    private static void ValidateSectorContentCandidates(
        SectorContentProfileSO profile,
        SectorContentSlotDefinition definition,
        List<ContentPreflightIssue> issues)
    {
        if (!definition.enabled || definition.prefabCandidates == null)
            return;

        for (int candidateIndex = 0; candidateIndex < definition.prefabCandidates.Count; candidateIndex++)
        {
            WeightedSectorContentPrefab candidate = definition.prefabCandidates[candidateIndex];
            if (candidate == null)
            {
                AddWarning(
                    issues,
                    "Sector Content",
                    $"Content Profile '{profile.name}', slot '{definition.slotId}' has an empty candidate at index {candidateIndex}.",
                    profile);
                continue;
            }

            if (candidate.prefab == null)
            {
                if (candidate.weight > 0)
                {
                    AddWarning(
                        issues,
                        "Sector Content",
                        $"Content Profile '{profile.name}', slot '{definition.slotId}' has a positive-weight candidate with no prefab.",
                        profile);
                }

                continue;
            }

            if (candidate.weight <= 0)
                continue;

            if (!PrefabUtility.IsPartOfPrefabAsset(candidate.prefab))
            {
                AddError(
                    issues,
                    "Sector Content",
                    $"Content slot '{definition.slotId}' references '{candidate.prefab.name}', which is not a prefab asset. Runtime content must instantiate a project prefab.",
                    candidate.prefab);
                continue;
            }

            if (definition.contentKind == SectorContentSlotKind.SpecialInteractable)
                continue;

            LootableObject lootableObject = candidate.prefab.GetComponentInChildren<LootableObject>(true);
            if (lootableObject == null)
            {
                AddError(
                    issues,
                    "Sector Content",
                    $"{definition.contentKind} slot '{definition.slotId}' candidate '{candidate.prefab.name}' needs a LootableObject component so the player can interact with it.",
                    candidate.prefab);
                continue;
            }

            LootObjectSO lootObject = lootableObject.LootObject;
            if (lootObject == null)
            {
                AddError(
                    issues,
                    "Sector Content",
                    $"{definition.contentKind} candidate '{candidate.prefab.name}' has a LootableObject with no direct LootObjectSO.",
                    lootableObject);
                continue;
            }

            if (lootObject.LootTable == null)
            {
                AddError(
                    issues,
                    "Sector Content",
                    $"{definition.contentKind} candidate '{candidate.prefab.name}' uses LootObjectSO '{lootObject.name}', but that asset has no direct LootTableSO.",
                    lootableObject);
                continue;
            }

            if (lootObject.WorldPrefab != null && lootObject.WorldPrefab != candidate.prefab)
            {
                AddWarning(
                    issues,
                    "Sector Content",
                    $"{definition.contentKind} candidate '{candidate.prefab.name}' references LootObjectSO '{lootObject.name}', whose World Prefab is '{lootObject.WorldPrefab.name}'. Confirm this shared table is intentional.",
                    candidate.prefab);
            }
        }
    }

    // Purpose: Checks whether this component has positive content candidate.
    private static bool HasPositiveContentCandidate(SectorContentSlotDefinition definition)
    {
        if (definition?.prefabCandidates == null)
            return false;

        for (int i = 0; i < definition.prefabCandidates.Count; i++)
        {
            WeightedSectorContentPrefab candidate = definition.prefabCandidates[i];
            if (candidate != null && candidate.prefab != null && candidate.weight > 0)
                return true;
        }

        return false;
    }

    // Purpose: Validates loot progression and reports invalid authored data.
    private static void ValidateLootProgression(
        List<ContentPreflightIssue> issues,
        HashSet<ItemSO> inspectedItems)
    {
        List<LootProgressionConfigSO> configs = FindAssets<LootProgressionConfigSO>();
        if (configs.Count == 0)
        {
            AddWarning(issues, "Loot", "No LootProgressionConfigSO asset was found. Enemy and object loot sources will have no common pool.", null);
            return;
        }

        for (int configIndex = 0; configIndex < configs.Count; configIndex++)
        {
            LootProgressionConfigSO config = configs[configIndex];
            HashSet<string> sourceIds = new(StringComparer.Ordinal);

            if (config.Sources == null || config.Sources.Count == 0)
            {
                AddWarning(issues, "Loot", "Loot progression config has no registered loot sources.", config);
                continue;
            }

            for (int sourceIndex = 0; sourceIndex < config.Sources.Count; sourceIndex++)
            {
                LootSourceDefinition source = config.Sources[sourceIndex];
                if (source == null)
                {
                    AddError(issues, "Loot", $"Loot source entry {sourceIndex} is null.", config);
                    continue;
                }

                if (source.sourceKind != LootSourceKind.EnemyShooter)
                    continue;

                if (string.IsNullOrWhiteSpace(source.sourceId))
                {
                    AddError(issues, "Loot", $"Loot source '{source.displayName}' has an empty Source Id.", config);
                }
                else if (!sourceIds.Add(source.sourceId))
                {
                    AddError(issues, "Loot", $"Duplicate loot Source Id '{source.sourceId}'.", config);
                }

                if (source.enemyShooterConfig == null)
                {
                    AddError(issues, "Loot", $"Enemy loot source '{source.sourceId}' has no EnemyShooterConfigSO.", config);
                }
                else if (!source.enemyShooterConfig.IsValid)
                {
                    AddError(issues, "Loot", $"Enemy loot source '{source.sourceId}' references an EnemyShooterConfigSO with no enemy prefab.", source.enemyShooterConfig);
                }

                ValidateCommonLootPool(source, config, issues, inspectedItems);
            }
        }

        List<LootObjectSO> lootObjects = FindAssets<LootObjectSO>();
        if (lootObjects.Count == 0)
        {
            AddWarning(issues, "Loot", "No LootObjectSO asset was found. World loot prefabs have no direct object record.", null);
            return;
        }

        for (int i = 0; i < lootObjects.Count; i++)
        {
            LootObjectSO lootObject = lootObjects[i];
            if (lootObject == null)
                continue;

            if (lootObject.WorldPrefab == null)
                AddError(issues, "Loot", $"LootObjectSO '{lootObject.name}' has no World Prefab.", lootObject);

            LootTableSO table = lootObject.LootTable;
            if (table == null)
            {
                AddError(issues, "Loot", $"LootObjectSO '{lootObject.name}' has no direct LootTableSO.", lootObject);
                continue;
            }

            ValidateDirectLootTable(table, issues, inspectedItems);
        }
    }

    // Purpose: Validates an independently authored LootTableSO used by a world LootObjectSO.
    private static void ValidateDirectLootTable(
        LootTableSO table,
        List<ContentPreflightIssue> issues,
        HashSet<ItemSO> inspectedItems)
    {
        LootObjectPool pool = table.Pool;
        if (pool == null)
        {
            AddError(issues, "Loot", $"LootTableSO '{table.name}' has no fixed object pool.", table);
            return;
        }

        LootStagePool validationPool = new LootStagePool
        {
            minRollCount = pool.minRollCount,
            maxRollCount = pool.maxRollCount,
            rarityTiers = pool.rarityTiers
        };
        ValidateCommonLootPool(
            new LootSourceDefinition
            {
                sourceId = $"{table.name} (Fixed Object Pool)",
                stagePools = new List<LootStagePool> { validationPool }
            },
            table,
            issues,
            inspectedItems);
    }

    // Purpose: Validates common loot pool and reports invalid authored data.
    private static void ValidateCommonLootPool(
        LootSourceDefinition source,
        UnityEngine.Object context,
        List<ContentPreflightIssue> issues,
        HashSet<ItemSO> inspectedItems)
    {
        if (source.stagePools == null || source.stagePools.Count == 0)
        {
            AddError(issues, "Loot", $"Loot source '{source.sourceId}' has no common pool (Stage Index 0).", context);
            return;
        }

        LootStagePool commonPool = null;
        int commonPoolCount = 0;
        for (int i = 0; i < source.stagePools.Count; i++)
        {
            LootStagePool pool = source.stagePools[i];
            if (pool == null)
            {
                AddWarning(issues, "Loot", $"Loot source '{source.sourceId}' contains a null pool entry.", context);
                continue;
            }

            if (pool.stageIndex == 0)
            {
                commonPool = pool;
                commonPoolCount++;
            }
            else
            {
                AddWarning(issues, "Loot", $"Loot source '{source.sourceId}' contains an unused Stage {pool.stageIndex} override. Only the Stage 0 common pool is used.", context);
            }
        }

        if (commonPoolCount != 1 || commonPool == null)
        {
            AddError(issues, "Loot", $"Loot source '{source.sourceId}' requires exactly one Stage 0 common pool (found {commonPoolCount}).", context);
            return;
        }

        if (commonPool.minRollCount > commonPool.maxRollCount)
        {
            AddError(issues, "Loot", $"Loot source '{source.sourceId}' has Min Rolls greater than Max Rolls.", context);
        }

        if (commonPool.rarityTiers == null || commonPool.rarityTiers.Count == 0)
        {
            AddError(issues, "Loot", $"Loot source '{source.sourceId}' common pool has no rarity tiers.", context);
            return;
        }

        bool hasEligibleItem = false;
        HashSet<PlayerShopItemRarity> tiers = new();
        for (int tierIndex = 0; tierIndex < commonPool.rarityTiers.Count; tierIndex++)
        {
            LootRarityTier tier = commonPool.rarityTiers[tierIndex];
            if (tier == null)
            {
                AddWarning(issues, "Loot", $"Loot source '{source.sourceId}' contains a null rarity tier.", context);
                continue;
            }

            if (!tiers.Add(tier.rarity))
            {
                AddError(issues, "Loot", $"Loot source '{source.sourceId}' has duplicate {tier.rarity} rarity tiers.", context);
            }

            float manualShareTotal = 0f;
            HashSet<ItemSO> tierItems = new();
            int validItemCount = 0;
            if (tier.items != null)
            {
                for (int itemIndex = 0; itemIndex < tier.items.Count; itemIndex++)
                {
                    LootItemEntry entry = tier.items[itemIndex];
                    if (entry == null || entry.item == null)
                    {
                        AddWarning(issues, "Loot", $"Loot source '{source.sourceId}', {tier.rarity} tier has an empty item entry at index {itemIndex}.", context);
                        continue;
                    }

                    if (entry.minAmount > entry.maxAmount)
                    {
                        AddError(issues, "Loot", $"Loot item '{entry.item.name}' has Min Amount greater than Max Amount.", entry.item);
                    }

                    if (!tierItems.Add(entry.item))
                    {
                        AddWarning(issues, "Loot", $"Loot source '{source.sourceId}', {tier.rarity} tier lists '{entry.item.name}' more than once.", context);
                    }

                    if (entry.useManualShare)
                        manualShareTotal += Mathf.Max(0f, entry.manualSharePercent);

                    validItemCount++;
                    inspectedItems.Add(entry.item);
                }
            }

            if (manualShareTotal > 100.001f)
            {
                AddError(issues, "Loot", $"Loot source '{source.sourceId}', {tier.rarity} tier reserves {manualShareTotal:0.##}% manual share. It must not exceed 100%.", context);
            }

            if (tier.chancePercent > 0f && validItemCount == 0)
            {
                AddWarning(issues, "Loot", $"Loot source '{source.sourceId}', {tier.rarity} tier has positive rarity chance but no valid items.", context);
            }

            hasEligibleItem |= tier.chancePercent > 0f && validItemCount > 0;
        }

        if (!hasEligibleItem)
        {
            AddError(issues, "Loot", $"Loot source '{source.sourceId}' cannot roll any valid item from its common pool.", context);
        }
    }

    // Purpose: Validates shops and reports invalid authored data.
    private static void ValidateShops(
        List<ContentPreflightIssue> issues,
        HashSet<ItemSO> inspectedItems)
    {
        List<ShopConfigSO> configs = FindAssets<ShopConfigSO>();
        if (configs.Count == 0)
        {
            AddError(issues, "Shop", "No ShopConfigSO asset was found. The Home shop cannot create offers.", null);
            return;
        }

        if (configs.Count > 1)
        {
            AddWarning(issues, "Shop", $"{configs.Count} ShopConfigSO assets were found. Confirm the Home Shop station references the intended one.", null);
        }

        for (int configIndex = 0; configIndex < configs.Count; configIndex++)
        {
            ShopConfigSO config = configs[configIndex];
            Dictionary<string, ShopConfigSO.ProductDefinition> productsById = new(StringComparer.Ordinal);
            int validBullets = 0;
            int validItems = 0;

            if (config.Products == null || config.Products.Count == 0)
            {
                AddError(issues, "Shop", "Shop catalog has no products.", config);
            }
            else
            {
                for (int productIndex = 0; productIndex < config.Products.Count; productIndex++)
                {
                    ShopConfigSO.ProductDefinition product = config.Products[productIndex];
                    if (product == null)
                    {
                        AddWarning(issues, "Shop", $"Shop product entry {productIndex} is null.", config);
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(product.ProductId))
                    {
                        AddError(issues, "Shop", $"Shop product entry {productIndex} has an empty Product Id.", config);
                    }
                    else if (!productsById.TryAdd(product.ProductId, product))
                    {
                        AddError(issues, "Shop", $"Duplicate shop Product Id '{product.ProductId}'.", config);
                    }

                    if (!product.IsValid)
                    {
                        AddError(issues, "Shop", $"Shop product '{product.ProductId}' requires an ItemSO and Offer Weight greater than zero.", config);
                        continue;
                    }

                    inspectedItems.Add(product.Item);
                    if (product.IsBullet)
                        validBullets++;
                    else
                        validItems++;
                }
            }

            if (config.BulletOfferCount > 0 && validBullets == 0)
            {
                AddError(issues, "Shop", "Bullet Offer Count is positive but the catalog has no valid bullet product.", config);
            }

            if (config.ItemOfferCount > 0 && validItems == 0)
            {
                AddError(issues, "Shop", "Item Offer Count is positive but the catalog has no valid non-bullet product.", config);
            }

            ValidateShopUnlockNodes(config, productsById, issues);
            ValidateExplicitShopUnlockRoutes(config, productsById, issues);
        }
    }

    // Purpose: Ensures every locked catalog product has a reachable explicit quest reward.
    // Shop nodes no longer poll stage or quest completion automatically, so this is the
    // authoring guard that catches permanently unavailable Home-shop products before Play.
    private static void ValidateExplicitShopUnlockRoutes(
        ShopConfigSO config,
        IReadOnlyDictionary<string, ShopConfigSO.ProductDefinition> productsById,
        List<ContentPreflightIssue> issues)
    {
        Dictionary<string, int> rewardRoutesByNodeId = new(StringComparer.Ordinal);
        List<QuestDefinitionSO> quests = FindAssets<QuestDefinitionSO>();

        for (int questIndex = 0; questIndex < quests.Count; questIndex++)
        {
            QuestDefinitionSO quest = quests[questIndex];
            QuestRewardDefinition[] rewards = quest != null ? quest.Rewards : null;
            for (int rewardIndex = 0; rewards != null && rewardIndex < rewards.Length; rewardIndex++)
            {
                QuestRewardDefinition reward = rewards[rewardIndex];
                if (reward == null ||
                    reward.RewardType != QuestRewardType.ShopUnlock ||
                    reward.ShopConfig != config)
                {
                    continue;
                }

                if (!config.TryGetNode(reward.ShopUnlockNodeId, out _))
                {
                    AddError(
                        issues,
                        "Quests",
                        $"Quest '{quest.QuestId}' reward {rewardIndex} references missing shop node '{reward.ShopUnlockNodeId}'.",
                        quest);
                    continue;
                }

                rewardRoutesByNodeId.TryGetValue(reward.ShopUnlockNodeId, out int routeCount);
                rewardRoutesByNodeId[reward.ShopUnlockNodeId] = routeCount + 1;
            }
        }

        HashSet<string> unlockableProductIds = new(StringComparer.Ordinal);
        IReadOnlyList<ShopConfigSO.UnlockNode> nodes = config.UnlockNodes;
        for (int nodeIndex = 0; nodes != null && nodeIndex < nodes.Count; nodeIndex++)
        {
            ShopConfigSO.UnlockNode node = nodes[nodeIndex];
            if (node == null || string.IsNullOrWhiteSpace(node.NodeId))
                continue;

            if (!rewardRoutesByNodeId.TryGetValue(node.NodeId, out int routeCount))
            {
                AddError(
                    issues,
                    "Shop",
                    $"Shop unlock node '{node.NodeId}' has no explicit QuestReward route and can never unlock.",
                    config);
            }
            else if (routeCount > 1)
            {
                AddWarning(
                    issues,
                    "Shop",
                    $"Shop unlock node '{node.NodeId}' is granted by {routeCount} quest rewards. This is safe but should be intentional.",
                    config);
            }

            IReadOnlyList<string> productIds = node.UnlockedProductIds;
            for (int productIndex = 0; productIds != null && productIndex < productIds.Count; productIndex++)
                unlockableProductIds.Add(productIds[productIndex]);
        }

        foreach (KeyValuePair<string, ShopConfigSO.ProductDefinition> pair in productsById)
        {
            if (!pair.Value.AvailableByDefault && !unlockableProductIds.Contains(pair.Key))
            {
                AddError(
                    issues,
                    "Shop",
                    $"Locked product '{pair.Key}' belongs to no unlock node and can never appear in the Home shop.",
                    config);
            }
        }
    }

    // Purpose: Validates shop unlock nodes and reports invalid authored data.
    private static void ValidateShopUnlockNodes(
        ShopConfigSO config,
        IReadOnlyDictionary<string, ShopConfigSO.ProductDefinition> productsById,
        List<ContentPreflightIssue> issues)
    {
        if (config.UnlockNodes == null || config.UnlockNodes.Count == 0)
            return;

        Dictionary<string, ShopConfigSO.UnlockNode> nodesById = new(StringComparer.Ordinal);
        for (int nodeIndex = 0; nodeIndex < config.UnlockNodes.Count; nodeIndex++)
        {
            ShopConfigSO.UnlockNode node = config.UnlockNodes[nodeIndex];
            if (node == null)
            {
                AddWarning(issues, "Shop", $"Shop unlock node entry {nodeIndex} is null.", config);
                continue;
            }

            if (string.IsNullOrWhiteSpace(node.NodeId))
            {
                AddError(issues, "Shop", $"Shop unlock node entry {nodeIndex} has an empty Node Id.", config);
                continue;
            }

            if (!nodesById.TryAdd(node.NodeId, node))
            {
                AddError(issues, "Shop", $"Duplicate shop unlock Node Id '{node.NodeId}'.", config);
            }
        }

        foreach (KeyValuePair<string, ShopConfigSO.UnlockNode> pair in nodesById)
        {
            ShopConfigSO.UnlockNode node = pair.Value;
            if (node.PrerequisiteNodeIds != null)
            {
                for (int i = 0; i < node.PrerequisiteNodeIds.Count; i++)
                {
                    string prerequisiteId = node.PrerequisiteNodeIds[i];
                    if (string.IsNullOrWhiteSpace(prerequisiteId) || !nodesById.ContainsKey(prerequisiteId))
                    {
                        AddError(issues, "Shop", $"Shop unlock node '{node.NodeId}' references missing prerequisite '{prerequisiteId}'.", config);
                    }
                }
            }

            if (node.UnlockedProductIds != null)
            {
                for (int i = 0; i < node.UnlockedProductIds.Count; i++)
                {
                    string productId = node.UnlockedProductIds[i];
                    if (string.IsNullOrWhiteSpace(productId) || !productsById.ContainsKey(productId))
                    {
                        AddError(issues, "Shop", $"Shop unlock node '{node.NodeId}' references missing product '{productId}'.", config);
                    }
                }
            }
        }

        DetectShopUnlockCycles(nodesById, config, issues);
    }

    // Purpose: Performs the detect shop unlock cycles workflow for this component.
    private static void DetectShopUnlockCycles(
        IReadOnlyDictionary<string, ShopConfigSO.UnlockNode> nodesById,
        ShopConfigSO config,
        List<ContentPreflightIssue> issues)
    {
        Dictionary<string, int> visitStates = new(StringComparer.Ordinal);
        HashSet<string> reportedNodes = new(StringComparer.Ordinal);

        foreach (string nodeId in nodesById.Keys)
        {
            VisitShopNode(nodeId, nodesById, visitStates, reportedNodes, config, issues);
        }
    }

    // Purpose: Performs the visit shop node workflow for this component.
    private static void VisitShopNode(
        string nodeId,
        IReadOnlyDictionary<string, ShopConfigSO.UnlockNode> nodesById,
        Dictionary<string, int> visitStates,
        HashSet<string> reportedNodes,
        ShopConfigSO config,
        List<ContentPreflightIssue> issues)
    {
        if (visitStates.TryGetValue(nodeId, out int state))
        {
            if (state == 1 && reportedNodes.Add(nodeId))
            {
                AddError(issues, "Shop", $"Shop unlock prerequisite cycle includes node '{nodeId}'.", config);
            }

            return;
        }

        visitStates[nodeId] = 1;
        ShopConfigSO.UnlockNode node = nodesById[nodeId];
        if (node.PrerequisiteNodeIds != null)
        {
            for (int i = 0; i < node.PrerequisiteNodeIds.Count; i++)
            {
                string prerequisiteId = node.PrerequisiteNodeIds[i];
                if (nodesById.ContainsKey(prerequisiteId))
                {
                    VisitShopNode(prerequisiteId, nodesById, visitStates, reportedNodes, config, issues);
                }
            }
        }

        visitStates[nodeId] = 2;
    }

    // Purpose: Validates quests and reports invalid authored data.
    private static void ValidateQuests(
        List<ContentPreflightIssue> issues,
        HashSet<ItemSO> inspectedItems)
    {
        List<QuestDefinitionSO> quests = FindAssets<QuestDefinitionSO>();
        if (quests.Count == 0)
        {
            AddWarning(issues, "Quests", "No QuestDefinitionSO assets were found.", null);
            return;
        }

        Dictionary<string, QuestDefinitionSO> questsById = new(StringComparer.Ordinal);
        for (int questIndex = 0; questIndex < quests.Count; questIndex++)
        {
            QuestDefinitionSO quest = quests[questIndex];
            string explicitId = GetSerializedString(quest, "_questId");
            if (string.IsNullOrWhiteSpace(explicitId))
            {
                AddWarning(issues, "Quests", $"Quest '{quest.name}' has no explicit Quest Id. Runtime will fall back to the asset name, which is unsafe for saved progress.", quest);
            }

            if (!questsById.TryAdd(quest.QuestId, quest))
            {
                AddError(issues, "Quests", $"Duplicate Quest Id '{quest.QuestId}'.", quest);
            }

            ValidateQuestDetails(quest, issues, inspectedItems);
        }

        DetectQuestCycles(questsById, issues);
    }

    // Purpose: Validates quest details and reports invalid authored data.
    private static void ValidateQuestDetails(
        QuestDefinitionSO quest,
        List<ContentPreflightIssue> issues,
        HashSet<ItemSO> inspectedItems)
    {
        if (quest.Objectives == null || quest.Objectives.Length == 0)
        {
            AddError(issues, "Quests", "Quest has no objectives.", quest);
        }
        else
        {
            for (int i = 0; i < quest.Objectives.Length; i++)
            {
                QuestObjectiveDefinition objective = quest.Objectives[i];
                if (objective == null)
                {
                    AddError(issues, "Quests", $"Objective {i} is null.", quest);
                    continue;
                }

                if (objective.ObjectiveType == QuestObjectiveType.CollectItem)
                {
                    if (objective.TargetItem == null)
                    {
                        AddError(issues, "Quests", $"Collect Item objective {i} has no target ItemSO.", quest);
                    }
                    else
                    {
                        inspectedItems.Add(objective.TargetItem);
                    }
                }
            }
        }

        if (quest.Rewards == null || quest.Rewards.Length == 0)
        {
            AddWarning(issues, "Quests", "Quest has no linked reward. This is allowed for WIP/content-only quests, but mission UI will show no reward.", quest);
        }
        else
        {
            for (int i = 0; i < quest.Rewards.Length; i++)
            {
                QuestRewardDefinition reward = quest.Rewards[i];
                if (reward == null)
                {
                    AddError(issues, "Quests", $"Reward {i} is null.", quest);
                    continue;
                }

                switch (reward.RewardType)
                {
                    case QuestRewardType.Item:
                        if (reward.Item == null)
                        {
                            AddError(issues, "Quests", $"Item reward {i} has no ItemSO.", quest);
                        }
                        else
                        {
                            inspectedItems.Add(reward.Item);
                        }
                        break;

                    case QuestRewardType.Stat:
                        if (Mathf.Approximately(reward.StatValue, 0f))
                        {
                            AddWarning(issues, "Quests", $"Stat reward {i} has value 0 and will not change the player.", quest);
                        }
                        break;

                    case QuestRewardType.ShopUnlock:
                        if (reward.ShopConfig == null ||
                            string.IsNullOrWhiteSpace(reward.ShopUnlockNodeId) ||
                            !reward.ShopConfig.TryGetNode(reward.ShopUnlockNodeId, out _))
                        {
                            AddError(issues, "Quests", $"Shop unlock reward {i} has no valid ShopConfigSO node.", quest);
                        }
                        break;

                    case QuestRewardType.StageUnlock:
                        if (reward.StageToUnlock == null)
                        {
                            AddError(issues, "Quests", $"Stage unlock reward {i} has no StageDefinitionSO.", quest);
                        }
                        break;

                    case QuestRewardType.WarehouseSlots:
                        if (reward.WarehouseSlotAmount <= 0)
                        {
                            AddError(issues, "Quests", $"Warehouse slot reward {i} must grant at least one slot.", quest);
                        }
                        break;
                }
            }
        }

        if (quest.Prerequisites != null)
        {
            for (int i = 0; i < quest.Prerequisites.Length; i++)
            {
                QuestDefinitionSO prerequisite = quest.Prerequisites[i];
                if (prerequisite == null)
                {
                    AddError(issues, "Quests", $"Prerequisite {i} is missing.", quest);
                }
                else if (prerequisite == quest)
                {
                    AddError(issues, "Quests", "Quest cannot require itself.", quest);
                }
            }
        }

        if (quest.CompletionAudioCue != null && !quest.CompletionAudioCue.HasPlayableClip)
        {
            AddError(issues, "Audio", $"Quest completion cue '{quest.CompletionAudioCue.name}' has no playable clip.", quest.CompletionAudioCue);
        }
    }

    // Purpose: Performs the detect quest cycles workflow for this component.
    private static void DetectQuestCycles(
        IReadOnlyDictionary<string, QuestDefinitionSO> questsById,
        List<ContentPreflightIssue> issues)
    {
        Dictionary<QuestDefinitionSO, int> visitStates = new();
        HashSet<QuestDefinitionSO> reported = new();
        foreach (QuestDefinitionSO quest in questsById.Values)
        {
            VisitQuest(quest, visitStates, reported, issues);
        }
    }

    // Purpose: Performs the visit quest workflow for this component.
    private static void VisitQuest(
        QuestDefinitionSO quest,
        Dictionary<QuestDefinitionSO, int> visitStates,
        HashSet<QuestDefinitionSO> reported,
        List<ContentPreflightIssue> issues)
    {
        if (quest == null)
            return;

        if (visitStates.TryGetValue(quest, out int state))
        {
            if (state == 1 && reported.Add(quest))
            {
                AddError(issues, "Quests", $"Quest prerequisite cycle includes '{quest.QuestId}'.", quest);
            }

            return;
        }

        visitStates[quest] = 1;
        QuestDefinitionSO[] prerequisites = quest.Prerequisites;
        for (int i = 0; i < prerequisites.Length; i++)
        {
            VisitQuest(prerequisites[i], visitStates, reported, issues);
        }

        visitStates[quest] = 2;
    }

    // Purpose: Validates audio cues and reports invalid authored data.
    private static void ValidateAudioCues(List<ContentPreflightIssue> issues)
    {
        List<AudioCueSO> cues = FindAssets<AudioCueSO>();
        Dictionary<string, AudioCueSO> cuesById = new(StringComparer.Ordinal);
        for (int i = 0; i < cues.Count; i++)
        {
            AudioCueSO cue = cues[i];
            if (string.IsNullOrWhiteSpace(cue.CueId))
            {
                AddError(issues, "Audio", "Audio Cue has no Cue Id. Select and resave it so OnValidate generates a stable id.", cue);
            }
            else if (!cuesById.TryAdd(cue.CueId, cue))
            {
                AddError(issues, "Audio", $"Duplicate Audio Cue Id '{cue.CueId}'.", cue);
            }

            if (!cue.HasPlayableClip)
            {
                AddWarning(issues, "Audio", "Audio Cue has no playable clips yet.", cue);
            }
        }

        List<AudioCueLibrarySO> libraries = FindAssets<AudioCueLibrarySO>();
        if (libraries.Count == 0)
        {
            AddWarning(issues, "Audio", "No AudioCueLibrarySO asset was found. Cue references still work, but the authoring catalogue is missing.", null);
            return;
        }

        if (libraries.Count > 1)
        {
            AddWarning(issues, "Audio", $"{libraries.Count} AudioCueLibrarySO assets were found. Keep one project catalogue unless separate libraries are intentional.", null);
        }

        HashSet<AudioCueSO> registeredCues = new();
        for (int libraryIndex = 0; libraryIndex < libraries.Count; libraryIndex++)
        {
            AudioCueLibrarySO library = libraries[libraryIndex];
            if (library.Cues == null || library.Cues.Count == 0)
            {
                AddWarning(issues, "Audio", "Audio Cue Library has no registered cues.", library);
                continue;
            }

            for (int cueIndex = 0; cueIndex < library.Cues.Count; cueIndex++)
            {
                AudioCueSO cue = library.Cues[cueIndex];
                if (cue == null)
                {
                    AddWarning(issues, "Audio", $"Audio Cue Library has a missing cue entry at index {cueIndex}.", library);
                    continue;
                }

                registeredCues.Add(cue);
            }
        }

        for (int i = 0; i < cues.Count; i++)
        {
            if (!registeredCues.Contains(cues[i]))
            {
                AddWarning(issues, "Audio", "Audio Cue is not registered in any Audio Cue Library.", cues[i]);
            }
        }
    }

    // Purpose: Validates bullet prefabs and reports invalid authored data.
    private static void ValidateBulletPrefabs(
        IEnumerable<ItemSO> inspectedItems,
        List<ContentPreflightIssue> issues)
    {
        foreach (ItemSO item in inspectedItems.Where(item => item != null))
        {
            if (item.WorldVisualPrefab == null)
            {
                AddWarning(issues, "Prefabs", $"Item '{item.name}' has no World Visual Prefab. World pickup will use the shared shell without an item visual.", item);
            }

            if (item is not BulletSO bullet)
                continue;

            if (bullet.ProjectilePrefab == null)
            {
                AddError(issues, "Prefabs", $"Bullet '{bullet.name}' has no Projectile Prefab.", bullet);
            }
            else if (!bullet.ProjectilePrefab.TryGetComponent<BasicBullet>(out _))
            {
                AddError(issues, "Prefabs", $"Bullet '{bullet.name}' Projectile Prefab requires a BasicBullet component.", bullet.ProjectilePrefab);
            }
        }
    }

    // Purpose: Validates enemy prefabs and reports invalid authored data.
    private static void ValidateEnemyPrefabs(List<ContentPreflightIssue> issues)
    {
        List<EnemyShooterConfigSO> configs = FindAssets<EnemyShooterConfigSO>();
        for (int i = 0; i < configs.Count; i++)
        {
            EnemyShooterConfigSO config = configs[i];
            if (!config.IsValid)
            {
                AddError(issues, "Prefabs", $"Enemy Shooter Config '{config.name}' has no Enemy Shooter prefab.", config);
            }
        }
    }

    private static List<T> FindAssets<T>() where T : UnityEngine.Object
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        List<T> assets = new(guids.Length);
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                assets.Add(asset);
        }

        return assets;
    }

    // Purpose: Returns the current serialized string.
    private static string GetSerializedString(UnityEngine.Object target, string propertyName)
    {
        SerializedObject serializedObject = new(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        return property != null && property.propertyType == SerializedPropertyType.String
            ? property.stringValue
            : string.Empty;
    }

    // Purpose: Adds the requested error to this component.
    private static void AddError(
        List<ContentPreflightIssue> issues,
        string category,
        string message,
        UnityEngine.Object context)
    {
        issues.Add(new ContentPreflightIssue(ContentPreflightSeverity.Error, category, message, context));
    }

    // Purpose: Adds the requested warning to this component.
    private static void AddWarning(
        List<ContentPreflightIssue> issues,
        string category,
        string message,
        UnityEngine.Object context)
    {
        issues.Add(new ContentPreflightIssue(ContentPreflightSeverity.Warning, category, message, context));
    }
}

public sealed class ContentPreflightWindow : EditorWindow
{
    private readonly List<ContentPreflightIssue> _issues = new();
    private Vector2 _scroll;
    private string _search = string.Empty;
    private bool _showWarnings = true;

    // Purpose: Opens the open UI or workflow.
    public static void Open()
    {
        ContentPreflightWindow window = GetWindow<ContentPreflightWindow>("Content Preflight");
        window.minSize = new Vector2(760f, 420f);
        window.RunPreflight(logToConsole: false);
        window.Show();
    }

    // Purpose: Responds to the gui callback.
    private void OnGUI()
    {
        DrawToolbar();
        DrawSummary();
        DrawIssues();
    }

    // Purpose: Draws the toolbar authoring UI.
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button("Run Preflight", EditorStyles.toolbarButton, GUILayout.Width(100f)))
        {
            RunPreflight(logToConsole: true);
        }

        GUILayout.FlexibleSpace();
        _showWarnings = GUILayout.Toggle(_showWarnings, "Warnings", EditorStyles.toolbarButton, GUILayout.Width(75f));
        _search = GUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.Width(240f));
        EditorGUILayout.EndHorizontal();

        AuthoringWorkspaceUi.DrawHeader(
            "Content Preflight",
            "Owner: Validation",
            "Read-only asset verification. Click an issue to select its source asset; this window never rewrites authored content.",
            "1 Run",
            "2 Filter",
            "3 Select Issue",
            "4 Fix Asset",
            "5 Run Again");
    }

    // Purpose: Draws the summary authoring UI.
    private void DrawSummary()
    {
        int errors = _issues.Count(issue => issue.Severity == ContentPreflightSeverity.Error);
        int warnings = _issues.Count(issue => issue.Severity == ContentPreflightSeverity.Warning);
        MessageType type = errors > 0
            ? MessageType.Error
            : warnings > 0 ? MessageType.Warning : MessageType.Info;

        string summary = errors > 0
            ? $"Preflight blocked: {errors} error(s), {warnings} warning(s). Fix errors before a content test."
            : warnings > 0
                ? $"Preflight passed without blocking errors: {warnings} warning(s) remain."
                : "Preflight passed: no configured content issues were found.";
        EditorGUILayout.HelpBox(summary, type);
    }

    // Purpose: Draws the issues authoring UI.
    private void DrawIssues()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        bool anyVisible = false;
        for (int i = 0; i < _issues.Count; i++)
        {
            ContentPreflightIssue issue = _issues[i];
            if (issue.Severity == ContentPreflightSeverity.Warning && !_showWarnings)
                continue;

            string line = $"[{issue.Category}] {issue.Message}";
            if (!string.IsNullOrWhiteSpace(_search) &&
                line.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0 &&
                (issue.Context == null || issue.Context.name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0))
            {
                continue;
            }

            anyVisible = true;
            MessageType messageType = issue.Severity == ContentPreflightSeverity.Error
                ? MessageType.Error
                : MessageType.Warning;

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.HelpBox(line, messageType);
            if (issue.Context != null && GUILayout.Button("Select", GUILayout.Width(58f), GUILayout.Height(36f)))
            {
                Selection.activeObject = issue.Context;
                EditorGUIUtility.PingObject(issue.Context);
            }

            EditorGUILayout.EndHorizontal();
        }

        if (!anyVisible)
        {
            EditorGUILayout.Space(16f);
            EditorGUILayout.LabelField(
                _issues.Count == 0 ? "Run Preflight to inspect project content." : "No issues match the current filter.",
                EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndScrollView();
    }

    // Purpose: Performs the run preflight workflow for this component.
    private void RunPreflight(bool logToConsole)
    {
        _issues.Clear();
        _issues.AddRange(ContentPreflightValidator.CollectIssues());

        if (logToConsole)
        {
            for (int i = 0; i < _issues.Count; i++)
            {
                ContentPreflightIssue issue = _issues[i];
                string message = $"[ContentPreflight][{issue.Category}] {issue.Message}";
                if (issue.Severity == ContentPreflightSeverity.Error)
                    Debug.LogError(message, issue.Context);
                else
                    Debug.LogWarning(message, issue.Context);
            }

            int errors = _issues.Count(issue => issue.Severity == ContentPreflightSeverity.Error);
            int warnings = _issues.Count(issue => issue.Severity == ContentPreflightSeverity.Warning);
            Debug.Log($"[ContentPreflight] Completed. errors={errors}, warnings={warnings}");
        }

        Repaint();
    }
}
