using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Only the lab room is additive. Never replaces the user's open scenes.</summary>
[InitializeOnLoad]
internal static class CombatLabSession
{
    private const string BackupKey = "BoardChase.CombatLab.SceneBackup";
    [Serializable] private sealed class LegacyBackup { public SceneSetup[] Scenes; }
    static CombatLabSession()
    {
        EditorApplication.playModeStateChanged += OnPlayMode;
        AssemblyReloadEvents.beforeAssemblyReload += () => { if (Current != null && Current.Ready) Current.End(); };
        EditorApplication.delayCall += RemoveReturnedLabAfterReload;
    }
    internal static CombatLabRuntime Current => CombatLabRuntime.Active;
    internal static bool OwnsScene => Current != null;

    internal static CombatLabRuntime Prepare(SectorPresetSO preset)
    {
        if (!EditorApplication.isPlaying || EditorApplication.isPaused || Current != null)
            throw new InvalidOperationException("기존 게임을 HOME에서 Play한 뒤 시작하세요. 일시정지는 해제해야 합니다.");
        if (preset == null || !preset.IsValid || preset.RoomType == StageRoomType.Empty)
            throw new InvalidOperationException("유효한 전투용 Sector Preset을 선택하세요.");
        var manager = UnityEngine.Object.FindFirstObjectByType<StageProgressionManager>();
        var state = UnityEngine.Object.FindFirstObjectByType<SectorStateManager>();
        if (manager == null || manager.InitialHideoutSector == null || state == null || state.CurrentSector != manager.InitialHideoutSector)
            throw new InvalidOperationException("HOME에서만 전투실험을 시작할 수 있습니다.");
        var scene = SceneManager.CreateScene("Combat Lab — runtime only");
        var root = new GameObject("Combat Lab — temporary sector");
        root.SetActive(false);
        SceneManager.MoveGameObjectToScene(root, scene);
        var driver = root.AddComponent<CombatLabRuntime>();
        driver.Preset = preset;
        try
        {
            var sector = UnityEngine.Object.Instantiate(preset.SectorPrefab, root.transform);
            sector.name = "Lab room " + preset.name;
            sector.transform.position = manager.InitialHideoutSector.transform.position + new Vector3(2048, 0, 2048);
            driver.Sector = sector.GetComponent<SectorRuntime>();
            if (driver.Sector == null) throw new InvalidOperationException("SectorRuntime이 없는 프리팹입니다.");
            // Disable progression/travel authoring before any cloned OnEnable runs.
            foreach (var spawner in sector.GetComponentsInChildren<SectorEnemySpawner>(true)) spawner.enabled = false;
            foreach (var portal in sector.GetComponentsInChildren<SectorPortal>(true)) portal.gameObject.SetActive(false);
            foreach (var station in sector.GetComponentsInChildren<RaidStation>(true)) station.enabled = false;
            foreach (var hatch in sector.GetComponentsInChildren<SectorHatch>(true)) hatch.enabled = false;
            driver.PlayerPosition = sector.transform.InverseTransformPoint(driver.Sector.PlayerStartPoint.position);
            driver.PlayerYaw = driver.Sector.PlayerStartPoint.localEulerAngles.y;
            return driver;
        }
        catch { UnityEngine.Object.Destroy(root); SceneManager.UnloadSceneAsync(scene); throw; }
    }
    internal static void Start(CombatLabRuntime driver)
    {
        SessionState.SetBool(CombatLabRuntime.IsolationKey, true);
        try { driver.gameObject.SetActive(true); driver.Begin(); }
        catch
        {
            // Begin returns the borrowed player before throwing. Do not unload if it still owns it.
            if (driver.Player != null && driver.Player.gameObject.scene == driver.gameObject.scene)
                throw;
            var scene = driver.gameObject.scene;
            driver.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(driver.gameObject);
            SceneManager.UnloadSceneAsync(scene);
            SessionState.EraseBool(CombatLabRuntime.IsolationKey);
            throw;
        }
    }
    internal static void Restore() { Current?.End(); }
    private static void RemoveReturnedLabAfterReload()
    {
        if (!EditorApplication.isPlaying || Current != null) return;
        // Before reload End has returned the borrowed player. Only unload empty lab scenes.
        bool removed = false;
        for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.name != "Combat Lab — runtime only" || !scene.isLoaded) continue;
            if (scene.GetRootGameObjects().Any(root => root.GetComponentInChildren<PlayerCharacter>(true) != null)) continue;
            SceneManager.UnloadSceneAsync(scene); removed = true;
        }
        if (removed) SessionState.EraseBool(CombatLabRuntime.IsolationKey);
    }
    private static void OnPlayMode(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.ExitingPlayMode && Current != null) Current.End();
        if (change != PlayModeStateChange.EnteredEditMode) return;
        SessionState.EraseBool(CombatLabRuntime.IsolationKey);
        // One-time migration for the old, already-open isolated Untitled harness.
        string json = SessionState.GetString(BackupKey, "");
        SessionState.EraseString(BackupKey);
        if (string.IsNullOrEmpty(json) || !UnityEngine.Object.FindObjectsByType<CombatLabRuntime>(FindObjectsInactive.Include,
                FindObjectsSortMode.None).Any()) return;
        var backup = JsonUtility.FromJson<LegacyBackup>(json);
        if (backup?.Scenes != null && backup.Scenes.Length > 0 && backup.Scenes.All(s => !string.IsNullOrEmpty(s.path)))
            EditorApplication.delayCall += () => EditorSceneManager.RestoreSceneManagerSetup(backup.Scenes);
    }
}
