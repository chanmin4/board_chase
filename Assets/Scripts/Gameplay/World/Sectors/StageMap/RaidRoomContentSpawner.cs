using UnityEngine;

public static class RaidRoomContentSpawner
{
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
