/*
 * Role hierarchy: Editor > Persistence
 * Primary responsibility: Provides a read-only inspection surface for the local ES3 save partitions and backups.
 * Execution boundary: Editor only
 */

#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class SaveInspectorWindow : EditorWindow
{
    private const float SummaryLabelWidth = 158f;

    private Vector2 _scrollPosition;
    private SaveSystem _saveSystemAsset;
    private PartitionSnapshot _settingsSnapshot;
    private PartitionSnapshot _profileSnapshot;
    private PartitionSnapshot _runSnapshot;

    [MenuItem("Tools/Board Chase/Save Inspector")]
    public static void Open()
    {
        SaveInspectorWindow window = GetWindow<SaveInspectorWindow>("Save Inspector");
        window.minSize = new Vector2(720f, 520f);
        window.RefreshSnapshots();
        window.Show();
    }

    private void OnEnable()
    {
        RefreshSnapshots();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Board Chase Save Inspector", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Read-only development viewer for settings, profile, run data, and their rotating ES3 backups. " +
            "It never edits or deletes a save file.",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Persistent path", Application.persistentDataPath, EditorStyles.miniLabel);

            if (GUILayout.Button("Open Folder", GUILayout.Width(96f)))
                EditorUtility.RevealInFinder(Application.persistentDataPath);

            if (GUILayout.Button("Refresh", GUILayout.Width(80f)))
                RefreshSnapshots();
        }

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        DrawPartition(_settingsSnapshot);
        DrawPartition(_profileSnapshot);
        DrawPartition(_runSnapshot);
        EditorGUILayout.EndScrollView();
    }

    private void RefreshSnapshots()
    {
        _saveSystemAsset = FindSaveSystemAsset();
        string settingsFilename = _saveSystemAsset != null ? _saveSystemAsset.SettingsFilename : "settings.es3";
        string profileFilename = _saveSystemAsset != null ? _saveSystemAsset.ProfileFilename : "profile_default.es3";
        string runFilename = _saveSystemAsset != null ? _saveSystemAsset.RunFilename : "run_default.es3";

        _settingsSnapshot = ReadSnapshot<DeviceSettingsSaveData>(
            "Device Settings",
            SaveSystem.SettingsSaveKey,
            settingsFilename,
            BuildSettingsSummary);
        _profileSnapshot = ReadSnapshot<Save>(
            "Permanent Profile",
            SaveSystem.ProfileSaveKey,
            profileFilename,
            BuildProfileSummary);
        _runSnapshot = ReadSnapshot<RunSaveData>(
            "Resumable Run",
            SaveSystem.RunSaveKey,
            runFilename,
            BuildRunSummary);
        Repaint();
    }

    private PartitionSnapshot ReadSnapshot<T>(
        string title,
        string key,
        string filename,
        Func<T, string[]> summaryBuilder)
        where T : class
    {
        PartitionSnapshot snapshot = new PartitionSnapshot(title, key, filename);
        snapshot.primary = ReadFile<T>(key, filename, summaryBuilder);

        for (int generation = 1; generation <= 3; generation++)
        {
            string backupFilename = GetBackupFilename(filename, generation);
            snapshot.backups[generation - 1] = ReadFile<T>(key, backupFilename, summaryBuilder);
        }

        return snapshot;
    }

    private static FileSnapshot ReadFile<T>(string key, string relativeFilename, Func<T, string[]> summaryBuilder)
        where T : class
    {
        FileSnapshot snapshot = new FileSnapshot(relativeFilename);
        snapshot.absolutePath = Path.Combine(Application.persistentDataPath, relativeFilename);

        if (!File.Exists(snapshot.absolutePath))
            return snapshot;

        snapshot.exists = true;
        FileInfo info = new FileInfo(snapshot.absolutePath);
        snapshot.lastWriteTime = info.LastWriteTime;
        snapshot.sizeBytes = info.Length;

        try
        {
            if (!ES3.KeyExists(key, relativeFilename))
            {
                snapshot.error = $"Missing ES3 key: {key}";
                return snapshot;
            }

            string json = ES3.Load<string>(key, relativeFilename, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                snapshot.error = "The ES3 value is empty.";
                return snapshot;
            }

            T payload = JsonUtility.FromJson<T>(json);
            if (payload == null)
            {
                snapshot.error = "JSON payload could not be deserialized.";
                return snapshot;
            }

            snapshot.valid = true;
            snapshot.prettyJson = JsonUtility.ToJson(payload, true);
            snapshot.summary = summaryBuilder(payload);
        }
        catch (Exception exception)
        {
            snapshot.error = exception.Message;
        }

        return snapshot;
    }

    private void DrawPartition(PartitionSnapshot partition)
    {
        if (partition == null)
            return;

        EditorGUILayout.Space(8f);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(partition.title, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("ES3 Key", partition.key, EditorStyles.miniLabel);
            DrawFileSnapshot("Primary", partition.primary, true);

            for (int index = 0; index < partition.backups.Length; index++)
                DrawFileSnapshot($"Backup {index + 1}", partition.backups[index], false);
        }
    }

    private static void DrawFileSnapshot(string label, FileSnapshot snapshot, bool expandedByDefault)
    {
        if (snapshot == null)
            return;

        EditorGUILayout.Space(3f);
        using (new EditorGUILayout.VerticalScope(EditorStyles.textArea))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel, GUILayout.Width(SummaryLabelWidth));
                EditorGUILayout.LabelField(snapshot.relativeFilename, EditorStyles.miniLabel);

                GUI.enabled = snapshot.valid;
                if (GUILayout.Button("Copy JSON", GUILayout.Width(84f)))
                    EditorGUIUtility.systemCopyBuffer = snapshot.prettyJson;
                GUI.enabled = true;
            }

            if (!snapshot.exists)
            {
                EditorGUILayout.LabelField("Not found", EditorStyles.miniLabel);
                return;
            }

            EditorGUILayout.LabelField("Modified", snapshot.lastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"), EditorStyles.miniLabel);
            EditorGUILayout.LabelField("Size", $"{snapshot.sizeBytes:N0} bytes", EditorStyles.miniLabel);

            if (!snapshot.valid)
            {
                EditorGUILayout.HelpBox(snapshot.error ?? "Unknown read error.", MessageType.Error);
                return;
            }

            if (snapshot.summary != null)
            {
                for (int index = 0; index < snapshot.summary.Length; index++)
                    EditorGUILayout.LabelField(snapshot.summary[index], EditorStyles.miniLabel);
            }

            snapshot.showJson = EditorGUILayout.Foldout(snapshot.showJson, "View JSON", true);

            if (snapshot.showJson)
            {
                GUI.enabled = false;
                EditorGUILayout.TextArea(snapshot.prettyJson, GUILayout.MinHeight(120f));
                GUI.enabled = true;
            }
        }
    }

    private static string[] BuildSettingsSummary(DeviceSettingsSaveData settings)
    {
        settings.EnsureDefaults();
        return new[]
        {
            $"Schema: {settings.schemaVersion}   Locale: {DisplayOrDash(settings.localeCode)}",
            $"Audio: Master {settings.masterVolume:0.00} / Music {settings.musicVolume:0.00} / SFX {settings.sfxVolume:0.00}",
            $"Display: Resolution index {settings.resolutionIndex}, Fullscreen {settings.isFullscreen}, VSync {settings.useVSync}, FPS {settings.targetFrameRate}",
            $"Key binding JSON: {settings.inputBindingOverridesJson?.Length ?? 0:N0} characters"
        };
    }

    private static string[] BuildProfileSummary(Save profile)
    {
        profile.EnsureRuntimeDefaults();
        return new[]
        {
            $"Schema: {profile.schemaVersion}   Highest stage: {profile.HighestUnlockedStageIndex}",
            $"Warehouse: {profile.InheritanceWarehouse.items.Count}/{profile.InheritanceWarehouse.capacity} slots",
            $"Meta upgrades: {CountMetaLevels(profile.MetaUpgrades)} total levels",
            $"Quests: {profile.QuestProgress.Count}   Shop unlock groups: {profile.ShopUnlockProgress.Count}",
            $"Persistent rewards: stats {profile.QuestStatRewards.Count}, warehouse slots {profile.QuestWarehouseSlotRewards.Count}"
        };
    }

    private static string[] BuildRunSummary(RunSaveData run)
    {
        run.EnsureDefaults();
        PlayerRunStateSaveData state = run.PlayerRunState;
        return new[]
        {
            $"Schema: {run.schemaVersion}   Snapshot active: {state.hasSnapshot}",
            $"Inventory: {state.inventorySlots.Count}   Weapons: {state.equippedWeapons.Count}   Key tools: {state.keyToolSlots.Count}",
            $"Magazines: {state.weaponMagazines.Count}   Passives: {state.equippedPassiveGuids.Count}   Effects: {state.statusEffects.Count}",
            $"Health: {state.currentHealth:0.##}   Infection: {state.currentInfection:0.##}   Shop states: {state.shopStates.Count}",
            $"Raid active: {run.raid.active}   Stage: {run.raid.layout?.stageIndex}   Generation: {run.raid.layout?.generationVersion}   Grid: {run.raid.layout?.gridSize}",
            $"Power: {run.raid.generatorPowered}   Hatch revealed: {run.raid.hatchRevealed}   Boss defeated: {run.raid.bossDefeated}",
            $"Raid seconds: {run.raid.remainingSeconds:0.##}   Gas: {run.raid.gasReleased}   Extraction committed: {run.raid.extractionCommitted}"
        };
    }

    private static int CountMetaLevels(MetaUpgradeSaveData meta)
    {
        if (meta == null)
            return 0;

        return meta.maxHealthLevel + meta.attackDamageLevel + meta.magazineSizeLevel +
               meta.moveSpeedLevel + meta.reloadDurationLevel + meta.attackSpeedLevel + meta.paintSpeedLevel;
    }

    private static string DisplayOrDash(string value) => string.IsNullOrWhiteSpace(value) ? "-" : value;

    private static SaveSystem FindSaveSystemAsset()
    {
        string[] guids = AssetDatabase.FindAssets("t:SaveSystem");
        for (int index = 0; index < guids.Length; index++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[index]);
            SaveSystem saveSystem = AssetDatabase.LoadAssetAtPath<SaveSystem>(path);
            if (saveSystem != null)
                return saveSystem;
        }

        return null;
    }

    private static string GetBackupFilename(string filename, int generation)
    {
        string stem = Path.GetFileNameWithoutExtension(filename);
        string extension = Path.GetExtension(filename);
        return $"backups/{stem}.backup.{generation}{extension}";
    }

    private sealed class PartitionSnapshot
    {
        public readonly string title;
        public readonly string key;
        public readonly FileSnapshot[] backups = new FileSnapshot[3];
        public FileSnapshot primary;

        public PartitionSnapshot(string title, string key, string filename)
        {
            this.title = title;
            this.key = key;
        }
    }

    private sealed class FileSnapshot
    {
        public readonly string relativeFilename;
        public string absolutePath;
        public bool exists;
        public bool valid;
        public bool showJson;
        public long sizeBytes;
        public DateTime lastWriteTime;
        public string error;
        public string prettyJson;
        public string[] summary;

        public FileSnapshot(string relativeFilename)
        {
            this.relativeFilename = relativeFilename;
        }
    }
}
#endif
