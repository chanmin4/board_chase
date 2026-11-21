/*
 * Role hierarchy: Core > Persistence
 * Primary responsibility: Owns local device settings, permanent profile, and resumable-run persistence.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "SaveSystem", menuName = "Save/Save System")]
public class SaveSystem : ScriptableObject
{
    public const string SettingsSaveKey = "BoardChase.DeviceSettings";
    public const string ProfileSaveKey = "BoardChase.PersistentProfile";
    public const string RunSaveKey = "BoardChase.ResumableRun";

    private const int BackupGenerationCount = 3;
    private const string LegacyBindingOverridesPrefsKey = "GameInput.BindingOverrides";
    private const string LegacyMasterVolumePrefsKey = "vol_master";
    private const string LegacyMusicVolumePrefsKey = "vol_music";
    private const string LegacySfxVolumePrefsKey = "vol_sfx";

    [SerializeField] private VoidEventChannelSO _saveSettingsEvent = default;
    [SerializeField] private SettingsSO _currentSettings = default;

    [Header("Partitioned Easy Save 3 Files")]
    [Tooltip("Device-local options: graphics, audio, language, and key bindings.")]
    [SerializeField] private string _settingsFilename = "settings.es3";
    [Tooltip("Permanent character progress. The default profile can later become profile_001, profile_002, and so on.")]
    [SerializeField] private string _profileFilename = "profile_default.es3";
    [Tooltip("Discardable continue-run snapshot. It is cleared on run loss.")]
    [SerializeField] private string _runFilename = "run_default.es3";
    [Tooltip("Changes from sliders and key rebinding are coalesced before the settings file is written.")]
    [SerializeField, Range(0.1f, 3f)] private float _settingsWriteDebounceSeconds = 0.75f;

    [Header("Legacy One-Time Migration")]
    [Tooltip("Former monolithic ES3 file. It is read only when no partitioned profile exists.")]
    [FormerlySerializedAs("_easySaveFilename")]
    [SerializeField] private string _legacyEasySaveFilename = "board_chase_progress.es3";
    [FormerlySerializedAs("_easySaveBackupFilename")]
    [SerializeField] private string _legacyEasySaveBackupFilename = "board_chase_progress.backup.es3";
    [Tooltip("Former JSON save file. It is read only when no ES3 save exists.")]
    public string saveFilename = "save.vaccine";
    public string backupSaveFilename = "save.vaccine.bak";

    [NonSerialized] public Save saveData = new Save();
    [NonSerialized] private DeviceSettingsSaveData _deviceSettings = new DeviceSettingsSaveData();
    [NonSerialized] private RunSaveData _runData = new RunSaveData();
    [NonSerialized] private bool _hasLoadedSaveData;
    [NonSerialized] private bool _loadedProfileFromDisk;
    [NonSerialized] private bool _settingsSavePending;
    [NonSerialized] private float _settingsSaveDueAt;
    [NonSerialized] private bool _runtimeSettingsInitialized;

    public static SaveSystem Active { get; private set; }

    public Save Profile => saveData ??= new Save();
    public DeviceSettingsSaveData DeviceSettings => _deviceSettings ??= new DeviceSettingsSaveData();
    public RunSaveData RunData => _runData ??= new RunSaveData();
    public string EasySaveFilename => _profileFilename;
    public string SettingsFilename => _settingsFilename;
    public string ProfileFilename => _profileFilename;
    public string RunFilename => _runFilename;
    public bool HasLoadedSaveData => _hasLoadedSaveData;
    public string PersistentDataDirectory => Application.persistentDataPath;

    // Purpose: Activates runtime subscriptions and exposes this save system to runtime services.
    private void OnEnable()
    {
        Active = this;

        if (_saveSettingsEvent != null)
            _saveSettingsEvent.OnEventRaised += RequestSettingsSave;
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_saveSettingsEvent != null)
            _saveSettingsEvent.OnEventRaised -= RequestSettingsSave;

        if (Active == this)
            Active = null;
    }

    /// <summary>
    /// Loads the three standard save partitions once. If the project finds an
    /// old monolithic profile first, it migrates it without deleting the source.
    /// </summary>
    public bool LoadSaveDataFromDisk()
    {
#if UNITY_EDITOR
        if (CombatLabRuntime.IsIsolatedSession) return false;
#endif
        if (_hasLoadedSaveData)
            return _loadedProfileFromDisk;

        Active = this;

        bool loadedProfile = TryLoadProfile(out Save profile);
        bool loadedSettings = TryLoadSettings(out DeviceSettingsSaveData settings);
        bool loadedRun = TryLoadRun(out RunSaveData run);

        saveData = loadedProfile ? profile : new Save();
        _deviceSettings = loadedSettings ? settings : new DeviceSettingsSaveData();
        _runData = loadedRun ? run : new RunSaveData();

        bool migratedLegacy = false;
        if ((!loadedProfile || !loadedSettings || !loadedRun) &&
            TryLoadLegacyMonolith(out LegacySaveData legacy))
        {
            if (!loadedProfile)
            {
                Save migratedProfile = legacy.ToProfileData();
                if (SaveMigration.TryMigrateProfile(migratedProfile, out string profileMigrationError))
                {
                    saveData = migratedProfile;
                    loadedProfile = true;
                    migratedLegacy = true;
                }
                else
                {
                    Debug.LogError($"[SaveSystem] Legacy profile migration failed: {profileMigrationError}", this);
                }
            }

            if (!loadedRun)
            {
                RunSaveData migratedRun = legacy.ToRunData();
                if (SaveMigration.TryMigrateRun(migratedRun, out string runMigrationError))
                {
                    _runData = migratedRun;
                    loadedRun = true;
                    migratedLegacy = true;
                }
                else
                {
                    Debug.LogWarning($"[SaveSystem] Legacy run migration failed: {runMigrationError}", this);
                }
            }

            if (!loadedSettings)
            {
                DeviceSettingsSaveData migratedSettings = legacy.ToDeviceSettingsData();
                if (SaveMigration.TryMigrateSettings(migratedSettings, out string settingsMigrationError))
                {
                    _deviceSettings = migratedSettings;
                    loadedSettings = true;
                    migratedLegacy = true;
                }
                else
                {
                    Debug.LogWarning($"[SaveSystem] Legacy settings migration failed: {settingsMigrationError}", this);
                }
            }
        }

        if (!SaveMigration.TryMigrateProfile(saveData, out string currentProfileError))
        {
            Debug.LogError($"[SaveSystem] Profile was rejected: {currentProfileError}", this);
            saveData = new Save();
            loadedProfile = false;
        }

        if (!SaveMigration.TryMigrateRun(_runData, out string currentRunError))
        {
            Debug.LogWarning($"[SaveSystem] Run snapshot was rejected: {currentRunError}", this);
            _runData = new RunSaveData();
        }

        if (!SaveMigration.TryMigrateSettings(_deviceSettings, out string currentSettingsError))
        {
            Debug.LogWarning($"[SaveSystem] Device settings were rejected: {currentSettingsError}", this);
            _deviceSettings = new DeviceSettingsSaveData();
        }

        CraftingRecipeRoll.ReconcileCommittedRecipes(saveData,_runData);
        bool migratedPlayerPrefs = !loadedSettings && !migratedLegacy && ImportLegacyPlayerPrefs();

        _hasLoadedSaveData = true;
        _loadedProfileFromDisk = loadedProfile || migratedLegacy;

        if (migratedLegacy)
        {
            WriteAllPartitions(captureRunState: false);
            Debug.Log("[SaveSystem] Migrated the legacy monolithic save into settings, profile, and run files.", this);
        }
        else if (migratedPlayerPrefs)
        {
            WriteSettingsPartition();
        }

        return _loadedProfileFromDisk;
    }

    /// <summary>
    /// Immediately writes all partitions. Use this for profile-affecting
    /// events such as run completion, death, quest rewards, and app exit.
    /// </summary>
    public void SaveDataToDisk()
    {
#if UNITY_EDITOR
        if (CombatLabRuntime.IsIsolatedSession) return;
#endif
        Active = this;
        WriteAllPartitions(captureRunState: true);
    }

    // A raid checkpoint writes its player inventory and map state in the SAME atomic run partition.
    public bool TrySaveRunDataToDisk()
    {
#if UNITY_EDITOR
        if (CombatLabRuntime.IsIsolatedSession) return false;
#endif
        Active = this;
        RunLifecycleCoordinator.CaptureRunStateForExternalSave(this);
        RunData.EnsureDefaults();
        return WriteRunPartition();
    }

    /// <summary>
    /// Coalesces a device-settings write. RunLifecycleCoordinator calls
    /// FlushPendingWrites every frame, while quit/pause uses SaveDataToDisk.
    /// </summary>
    public void RequestSettingsSave()
    {
        Active = this;
        CaptureCurrentSettings();
        ScheduleSettingsWrite();
    }

    /// <summary>
    /// Signals that SettingsSystem has copied the persisted device options
    /// into its runtime SettingsSO. Until then, an early profile save must not
    /// overwrite valid device data with the authoring asset's inspector
    /// defaults merely because Unity's Awake order happened to differ.
    /// </summary>
    public void MarkRuntimeSettingsInitialized()
    {
        _runtimeSettingsInitialized = true;
    }

    // Purpose: Defers a settings-only disk write without overwriting a direct runtime value.
    private void ScheduleSettingsWrite()
    {
        _settingsSavePending = true;
        _settingsSaveDueAt = Time.unscaledTime + Mathf.Max(0.1f, _settingsWriteDebounceSeconds);
    }

    // Purpose: Flushes deferred settings changes when their debounce window has elapsed.
    public void FlushPendingWrites()
    {
        if (!_settingsSavePending || Time.unscaledTime < _settingsSaveDueAt)
            return;

        _settingsSavePending = false;
        WriteSettingsPartition();
    }

    /// <summary>Stores key-binding overrides in settings.es3 rather than PlayerPrefs.</summary>
    public void SetInputBindingOverrides(string overridesJson)
    {
        DeviceSettings.inputBindingOverridesJson = overridesJson ?? string.Empty;
        ScheduleSettingsWrite();
    }

    public string GetInputBindingOverrides() => DeviceSettings.inputBindingOverridesJson ?? string.Empty;

    /// <summary>Updates one device-local audio channel and coalesces the write.</summary>
    public void SetDeviceAudioVolume(DeviceAudioChannel channel, float volume)
    {
        volume = Mathf.Clamp01(volume);

        switch (channel)
        {
            case DeviceAudioChannel.Master:
                DeviceSettings.masterVolume = volume;
                break;
            case DeviceAudioChannel.Music:
                DeviceSettings.musicVolume = volume;
                break;
            case DeviceAudioChannel.Sfx:
                DeviceSettings.sfxVolume = volume;
                break;
        }

        ScheduleSettingsWrite();
    }

    /// <summary>
    /// Deletes profile/run data for a deliberate New Game while intentionally
    /// preserving settings and custom key bindings on this device.
    /// </summary>
    public void WriteEmptySaveFile()
    {
#if UNITY_EDITOR
        if (CombatLabRuntime.IsIsolatedSession) return;
#endif
        saveData = new Save();
        _runData = new RunSaveData();
        _hasLoadedSaveData = true;
        _loadedProfileFromDisk = false;
        _settingsSavePending = false;

        try
        {
            DeletePartitionFiles(_profileFilename);
            DeletePartitionFiles(_runFilename);

            // Do not allow a deliberate New Game to resurrect the former
            // monolithic profile during a crash between reset and new write.
            DeleteFileIfPresent(_legacyEasySaveFilename);
            DeleteFileIfPresent(_legacyEasySaveBackupFilename);
            FileManager.WriteToFile(saveFilename, string.Empty);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[SaveSystem] Profile reset failed. error={exception.Message}", this);
        }
    }

    // Purpose: Initializes an empty profile and run snapshot, preserving device settings.
    public void SetNewGameData()
    {
        saveData = new Save();
        _runData = new RunSaveData();
        SaveDataToDisk();
    }

    public int GetHighestUnlockedStageIndex(int fallback)
    {
        return Profile.HighestUnlockedStageIndex >= 0
            ? Mathf.Max(fallback, Profile.HighestUnlockedStageIndex)
            : fallback;
    }

    public void SetHighestUnlockedStageIndex(int stageIndex)
    {
        Profile.HighestUnlockedStageIndex = stageIndex;
        SaveDataToDisk();
    }

    public string GetBackupFilename(string filename, int generation)
    {
        generation = Mathf.Clamp(generation, 1, BackupGenerationCount);
        string stem = Path.GetFileNameWithoutExtension(filename);
        string extension = Path.GetExtension(filename);
        return $"backups/{stem}.backup.{generation}{extension}";
    }

    public string GetAbsoluteSavePath(string relativeFilename) =>
        Path.Combine(Application.persistentDataPath, relativeFilename);

    // Purpose: Captures live state and writes every standard data partition immediately.
    private void WriteAllPartitions(bool captureRunState)
    {
        if (captureRunState)
            RunLifecycleCoordinator.CaptureRunStateForExternalSave(this);

        Profile.EnsureRuntimeDefaults();
        RunData.EnsureDefaults();
        CaptureCurrentSettings();

        bool profileWritten = WriteProfilePartition();
        bool runWritten = WriteRunPartition();
        bool settingsWritten = WriteSettingsPartition();
        _settingsSavePending = false;

        if (!profileWritten || !runWritten || !settingsWritten)
        {
            Debug.LogError(
                $"[SaveSystem] Partition write incomplete. profile={profileWritten}, run={runWritten}, settings={settingsWritten}",
                this);
        }
    }

    private void CaptureCurrentSettings()
    {
        if (!_runtimeSettingsInitialized || _currentSettings == null)
        {
            DeviceSettings.EnsureDefaults();
            return;
        }

        DeviceSettings.masterVolume = _currentSettings.MasterVolume;
        DeviceSettings.musicVolume = _currentSettings.MusicVolume;
        DeviceSettings.sfxVolume = _currentSettings.SfxVolume;
        DeviceSettings.resolutionIndex = _currentSettings.ResolutionsIndex;
        DeviceSettings.isFullscreen = _currentSettings.IsFullscreen;
        DeviceSettings.aimSensitivity = _currentSettings.AimSensitivity;
        DeviceSettings.useVSync = _currentSettings.UseVSync;
        DeviceSettings.targetFrameRate = _currentSettings.TargetFrameRate;
        DeviceSettings.antiAliasingIndex = _currentSettings.AntiAliasingIndex;
        DeviceSettings.shadowDistance = _currentSettings.ShadowDistance;

        if (_currentSettings.CurrentLocale != null)
            DeviceSettings.localeCode = _currentSettings.CurrentLocale.Identifier.Code;

        DeviceSettings.EnsureDefaults();
    }

    private bool TryLoadProfile(out Save profile)
    {
        profile = null;

        foreach (string filename in EnumeratePartitionCandidates(_profileFilename))
        {
            if (!TryReadJson(ProfileSaveKey, filename, out string json) ||
                !TryDeserialize(json, out Save candidate) ||
                !SaveMigration.TryMigrateProfile(candidate, out _))
            {
                continue;
            }

            profile = candidate;
            LogBackupRecoveryIfNeeded(_profileFilename, filename);
            return true;
        }

        return false;
    }

    private bool TryLoadRun(out RunSaveData run)
    {
        run = null;

        foreach (string filename in EnumeratePartitionCandidates(_runFilename))
        {
            if (!TryReadJson(RunSaveKey, filename, out string json) ||
                !TryDeserialize(json, out RunSaveData candidate) ||
                !SaveMigration.TryMigrateRun(candidate, out _))
            {
                continue;
            }

            run = candidate;
            LogBackupRecoveryIfNeeded(_runFilename, filename);
            return true;
        }

        return false;
    }

    private bool TryLoadSettings(out DeviceSettingsSaveData settings)
    {
        settings = null;

        foreach (string filename in EnumeratePartitionCandidates(_settingsFilename))
        {
            if (!TryReadJson(SettingsSaveKey, filename, out string json) ||
                !TryDeserialize(json, out DeviceSettingsSaveData candidate) ||
                !SaveMigration.TryMigrateSettings(candidate, out _))
            {
                continue;
            }

            settings = candidate;
            LogBackupRecoveryIfNeeded(_settingsFilename, filename);
            return true;
        }

        return false;
    }

    private bool TryLoadLegacyMonolith(out LegacySaveData legacy)
    {
        legacy = null;

        foreach (string filename in new[] { _legacyEasySaveFilename, _legacyEasySaveBackupFilename })
        {
            if (TryReadJson(ProfileSaveKey, filename, out string json) &&
                TryDeserialize(json, out LegacySaveData candidate))
            {
                legacy = candidate;
                return true;
            }
        }

        if (FileManager.LoadFromFile(saveFilename, out string legacyJson) &&
            TryDeserialize(legacyJson, out LegacySaveData legacyJsonData))
        {
            legacy = legacyJsonData;
            return true;
        }

        return false;
    }

    private bool ImportLegacyPlayerPrefs()
    {
        bool imported = false;

        if (string.IsNullOrWhiteSpace(DeviceSettings.inputBindingOverridesJson) &&
            PlayerPrefs.HasKey(LegacyBindingOverridesPrefsKey))
        {
            DeviceSettings.inputBindingOverridesJson =
                PlayerPrefs.GetString(LegacyBindingOverridesPrefsKey, string.Empty);
            imported = true;
        }

        if (PlayerPrefs.HasKey(LegacyMasterVolumePrefsKey))
        {
            DeviceSettings.masterVolume = PlayerPrefs.GetFloat(LegacyMasterVolumePrefsKey, DeviceSettings.masterVolume);
            imported = true;
        }

        if (PlayerPrefs.HasKey(LegacyMusicVolumePrefsKey))
        {
            DeviceSettings.musicVolume = PlayerPrefs.GetFloat(LegacyMusicVolumePrefsKey, DeviceSettings.musicVolume);
            imported = true;
        }

        if (PlayerPrefs.HasKey(LegacySfxVolumePrefsKey))
        {
            DeviceSettings.sfxVolume = PlayerPrefs.GetFloat(LegacySfxVolumePrefsKey, DeviceSettings.sfxVolume);
            imported = true;
        }

        DeviceSettings.EnsureDefaults();
        return imported;
    }

    private bool WriteProfilePartition()
    {
        PassiveInstanceRuntime.SynchronizeSnapshots(Profile, RunData);
        Profile.schemaVersion = Save.CurrentSchemaVersion;
        return WriteJsonAtomically(
            ProfileSaveKey,
            _profileFilename,
            JsonUtility.ToJson(Profile),
            json => TryDeserialize(json, out Save candidate) && SaveMigration.TryMigrateProfile(candidate, out _));
    }

    private bool WriteRunPartition()
    {
        PassiveInstanceRuntime.SynchronizeSnapshots(Profile, RunData);
        RunData.schemaVersion = RunSaveData.CurrentSchemaVersion;
        return WriteJsonAtomically(
            RunSaveKey,
            _runFilename,
            JsonUtility.ToJson(RunData),
            json => TryDeserialize(json, out RunSaveData candidate) && SaveMigration.TryMigrateRun(candidate, out _));
    }

    private bool WriteSettingsPartition()
    {
        DeviceSettings.schemaVersion = DeviceSettingsSaveData.CurrentSchemaVersion;
        return WriteJsonAtomically(
            SettingsSaveKey,
            _settingsFilename,
            JsonUtility.ToJson(DeviceSettings),
            json => TryDeserialize(json, out DeviceSettingsSaveData candidate) && SaveMigration.TryMigrateSettings(candidate, out _));
    }

    private bool WriteJsonAtomically(string key, string filename, string json, Func<string, bool> validateJson)
    {
#if UNITY_EDITOR
        // SO references can load SaveSystem even in a room-only sandbox. Never touch real partitions/backups.
        if (CombatLabRuntime.IsIsolatedSession) return false;
#endif
        string temporaryFilename = filename + ".tmp";

        try
        {
            ES3.Save(key, json, temporaryFilename);

            if (!TryReadJson(key, temporaryFilename, out string roundTripJson) ||
                !validateJson(roundTripJson))
            {
                throw new InvalidOperationException("Temporary save verification failed.");
            }

            RotateBackups(filename, key, validateJson);
            ES3.RenameFile(temporaryFilename, filename);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[SaveSystem] ES3 write failed. file={filename}, error={exception.Message}",
                this);
            DeleteFileIfPresent(temporaryFilename);
            return false;
        }
    }

    private void RotateBackups(string filename, string key, Func<string, bool> validateJson)
    {
        for (int generation = BackupGenerationCount; generation >= 2; generation--)
        {
            string source = GetBackupFilename(filename, generation - 1);
            string destination = GetBackupFilename(filename, generation);

            if (ES3.FileExists(source))
            {
                DeleteFileIfPresent(destination);
                ES3.RenameFile(source, destination);
            }
        }

        if (TryReadJson(key, filename, out string currentJson) && validateJson(currentJson))
        {
            string latestBackup = GetBackupFilename(filename, 1);
            DeleteFileIfPresent(latestBackup);
            ES3.CopyFile(filename, latestBackup);
        }
    }

    private bool TryReadJson(string key, string filename, out string json)
    {
        json = string.Empty;

        try
        {
            if (!ES3.FileExists(filename) || !ES3.KeyExists(key, filename))
                return false;

            json = ES3.Load<string>(key, filename, string.Empty);
            return !string.IsNullOrWhiteSpace(json);
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"[SaveSystem] ES3 read failed. file={filename}, error={exception.Message}",
                this);
            return false;
        }
    }

    private static bool TryDeserialize<T>(string json, out T data)
        where T : class
    {
        data = null;

        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            data = JsonUtility.FromJson<T>(json);
            return data != null;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[SaveSystem] JSON parse failed. error={exception.Message}");
            return false;
        }
    }

    private string[] EnumeratePartitionCandidates(string primaryFilename)
    {
        return new[]
        {
            primaryFilename,
            GetBackupFilename(primaryFilename, 1),
            GetBackupFilename(primaryFilename, 2),
            GetBackupFilename(primaryFilename, 3)
        };
    }

    private void DeletePartitionFiles(string filename)
    {
        DeleteFileIfPresent(filename);
        DeleteFileIfPresent(filename + ".tmp");

        for (int generation = 1; generation <= BackupGenerationCount; generation++)
            DeleteFileIfPresent(GetBackupFilename(filename, generation));
    }

    private static void DeleteFileIfPresent(string filename)
    {
        if (ES3.FileExists(filename))
            ES3.DeleteFile(filename);
    }

    private void LogBackupRecoveryIfNeeded(string primaryFilename, string loadedFilename)
    {
        if (string.Equals(primaryFilename, loadedFilename, StringComparison.Ordinal))
            return;

        Debug.LogWarning(
            $"[SaveSystem] Loaded backup because the primary file was unavailable or invalid. file={loadedFilename}",
            this);
    }
}

public enum DeviceAudioChannel
{
    Master,
    Music,
    Sfx
}
