using UnityEngine;

public static class RaidRoomContentSpawner
{
    public static LootObjectSO PickGeneralSupply(RaidRuntimeSettings settings, int stageSeed, Vector2Int coord)
    {
        if (settings == null) return null;
        var random = new System.Random(StageDefinitionSO.BuildSectorSeed(stageSeed, coord, 1657));
        if (random.NextDouble() >= Mathf.Clamp01(settings.normalSupplyChance)) return null;
        return random.NextDouble() < Mathf.Clamp01(settings.weaponSupplyShare)
            ? settings.normalWeaponSupply : settings.normalMedicalSupply;
    }

    /// <summary>Adds one supply crate without replacing any authored crate's source or content.</summary>
    public static void AddGeneralSupply(SectorRuntime sector, RaidRuntimeSettings settings, int stageSeed)
    {
        if (sector == null) return;
        LootObjectSO source = PickGeneralSupply(settings, stageSeed, sector.Coord);
        if (source == null || source.WorldPrefab == null) return;
        Vector3 position = sector.PlayerStartPoint.position + Vector3.right * 3f;
        foreach (var anchor in sector.GetComponentsInChildren<SectorContentSlotAnchor>(true))
            if (anchor.EnabledForRuntime && anchor.ContentKind == SectorContentSlotKind.LootContainer &&
                anchor.SpawnParent.childCount == 0)
            { position = anchor.SpawnParent.position; break; }
        var instance = Object.Instantiate(source.WorldPrefab, sector.transform);
        instance.name = source.name + " [Raid general supply]";
        instance.transform.position = position;
        var loot = instance.GetComponentInChildren<LootableObject>(true);
        if (loot != null) loot.ConfigureRuntimeSource(source);
        else Debug.LogError($"[Raid] General supply lacks LootableObject: {source.name}", source);
    }

    public static void Populate(SectorRuntime sector, LootObjectSO source)
    {
        if (sector == null || source == null || source.WorldPrefab == null) return;
        LootableObject[] existing = sector.GetComponentsInChildren<LootableObject>(true);
        foreach (LootableObject container in existing)
        {
            // Preserve positions and geometry; select the room's typed pool on this instance only.
            container.ConfigureRuntimeSource(source);
        }
        if (existing.Length > 0) return;
        var instance = Object.Instantiate(source.WorldPrefab, sector.transform);
        instance.name = source.name + " [Raid prototype slot]";
        instance.transform.position = sector.PlayerStartPoint.position + Vector3.left * 3f;
        LootableObject loot = instance.GetComponentInChildren<LootableObject>(true);
        if (loot != null) loot.ConfigureRuntimeSource(source);
        else Debug.LogError($"[Raid] Loot prefab lacks LootableObject: {source.name}", source);
    }
}
