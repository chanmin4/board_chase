/*
 * Role hierarchy: Authoring > Editor > Loot
 * Primary responsibility: Keeps world-container runtime wiring distinct from authored Loot Table data.
 * Execution boundary: Editor only
 */

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StructureLootInventoryRuntime))]
public sealed class StructureLootInventoryRuntimeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        StructureLootInventoryRuntime runtime = (StructureLootInventoryRuntime)target;
        LootableObject lootableObject = runtime.GetComponent<LootableObject>();

        // SectorTreasure still owns its own authored settings. Only the new
        // LootableObject flow is intentionally table-driven and read-only here.
        if (lootableObject == null)
        {
            DrawDefaultInspector();
            return;
        }

        EditorGUILayout.HelpBox(
            "AUTOMATIC RUNTIME COMPONENT\n" +
            "Do not author display text, slot count, channels, equipment, or refs here. " +
            "LootableObject reads them from its assigned LootObjectSO and shared channels when Play starts.",
            MessageType.Info);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("Authoring Owner", lootableObject, typeof(LootableObject), true);
            EditorGUILayout.ObjectField("Loot Object (SO)", lootableObject.LootObject, typeof(LootObjectSO), false);
            EditorGUILayout.ObjectField("Loot Table (SO)", lootableObject.LootTable, typeof(LootTableSO), false);

            if (lootableObject.TryGetLootObject(out LootObjectSO lootObject))
            {
                EditorGUILayout.TextField("Display Label (SO)", lootObject.ResolveDisplayName());
                EditorGUILayout.IntField("Loot Slot Count (SO)", lootObject.LootSlotCount);
            }
            else
            {
                EditorGUILayout.TextField("Loot Object Binding", "Not assigned");
            }

            EditorGUILayout.IntField("Runtime Slot Count", runtime.RuntimeSlotCount);
            EditorGUILayout.Toggle("Open Request Channel Ready", runtime.HasOpenRequestChannel);
            EditorGUILayout.Toggle("Overlay Request Channel Ready", runtime.HasOverlayRequestChannel);
        }

        EditorGUILayout.Space(4f);
        EditorGUILayout.HelpBox(
            Application.isPlaying
                ? "The fields above are the current Play Mode values. Stop Play Mode to change the LootObjectSO or LootTableSO."
                : "The resolved values are applied in Awake. Edit the LootableObject component or its direct LootObjectSO instead.",
            MessageType.None);
    }
}
