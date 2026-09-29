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

    // Purpose: Initializes initialize for use by this component.
    public void Initialize(string slotId)
    {
        SlotId = slotId;
    }
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
        int stageSeed)
    {
        if (sector == null || profile == null)
            return 0;

        ClearGeneratedContent(sector);
        sector.CollectContentSlotAnchors(SlotBuffer);
        SlotBuffer.Sort(CompareAnchors);

        int spawnedCount = 0;
        HashSet<string> processedSlotIds = new(StringComparer.Ordinal);

        for (int i = 0; i < SlotBuffer.Count; i++)
        {
            SectorContentSlotAnchor anchor = SlotBuffer[i];
            if (anchor == null || string.IsNullOrWhiteSpace(anchor.SlotId))
                continue;

            if (!processedSlotIds.Add(anchor.SlotId))
            {
                Debug.LogWarning(
                    $"[SectorContentSlotSpawner] Duplicate content Slot ID was skipped. " +
                    $"sector={sector.name}, slotId={anchor.SlotId}",
                    anchor);
                continue;
            }

            if (!profile.TryGetSlotDefinition(anchor.SlotId, out SectorContentSlotDefinition definition))
                continue;

            if (definition.contentKind != anchor.ContentKind)
            {
                Debug.LogWarning(
                    $"[SectorContentSlotSpawner] Content slot kind mismatch was skipped. " +
                    $"sector={sector.name}, slotId={anchor.SlotId}, anchor={anchor.ContentKind}, profile={definition.contentKind}",
                    anchor);
                continue;
            }

            System.Random random = new(BuildSlotSeed(stageSeed, sector.Coord, anchor.SlotId));
            if (!profile.TryRollContentForSlot(anchor.SlotId, random, out GameObject prefab) ||
                prefab == null)
            {
                continue;
            }

            Transform spawnParent = anchor.SpawnParent;
            GameObject instance = UnityEngine.Object.Instantiate(prefab, spawnParent);
            instance.transform.SetPositionAndRotation(
                spawnParent.position,
                spawnParent.rotation);
            instance.name = $"{prefab.name} [Slot:{anchor.SlotId}]";

            SectorContentRuntimeInstance marker =
                instance.GetComponent<SectorContentRuntimeInstance>();
            if (marker == null)
                marker = instance.AddComponent<SectorContentRuntimeInstance>();
            marker.Initialize(anchor.SlotId);

            spawnedCount++;
        }

        SlotBuffer.Clear();
        return spawnedCount;
    }

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
                UnityEngine.Object.Destroy(instance.gameObject);
            else
                UnityEngine.Object.DestroyImmediate(instance.gameObject);
        }
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
