/*
 * Role hierarchy: Authoring > Editor > Debug Monitor Encounter
 * Primary responsibility: Shows the resolved runtime encounter plan for a selected entity's sector.
 * Execution boundary: Editor only (Assembly-CSharp-Editor)
 */

using UnityEditor;
using UnityEngine;

public sealed partial class DebugMonitorWindow
{
    // Purpose: Draws the selected sector encounter's authored binding and resolved runtime wave plan.
    private void DrawSelectedEncounterDetails(CharacterRow row)
    {
        if (row == null || row.sector == null)
            return;

        SectorEnemySpawner spawner =
            row.sector.GetComponentInChildren<SectorEnemySpawner>(true);

        if (spawner == null)
            return;

        EditorGUILayout.Space(6f);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Sector Encounter Runtime", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This is the resolved runtime result for the selected character's room. " +
            "Use it to distinguish an authored Stage encounter from an old or incomplete spawn plan.",
            MessageType.Info);

        EditorGUILayout.LabelField("Sector", row.sector.name);
        EditorGUILayout.LabelField("Configured Encounter", spawner.ConfiguredEncounterPresetName);
        EditorGUILayout.LabelField("Selected Encounter", spawner.SelectedEncounterPresetName);
        EditorGUILayout.LabelField("Resolved Wave Plan", spawner.DebugResolvedWavePlan, EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField(
            "Spawn Progress",
            $"planned {spawner.EncounterTotalSpawnCount} / spawned {spawner.SpawnedEncounterEnemyCount} / " +
            $"pending {spawner.PendingEncounterSpawnCount} / max alive {spawner.EncounterMaxAlive}");
        EditorGUILayout.LabelField(
            "State",
            $"started={spawner.HasStartedEncounter}, finished={spawner.EncounterSpawnFinished}, " +
            $"configuration failed={spawner.EncounterConfigurationFailed}");

        EditorGUILayout.EndVertical();
    }
}
