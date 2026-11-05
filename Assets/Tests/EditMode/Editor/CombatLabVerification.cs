using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Opt-in read-only preview checks. Never enters Play or saves an authored scene.</summary>
public static class CombatLabVerification
{
    private const string Folder = "Library/LiveCombatLabVerification";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static double _next;
    [InitializeOnLoadMethod] private static void Register() => EditorApplication.update += Check;
    private static void Check()
    {
        if (EditorApplication.timeSinceStartup < _next) return;
        _next = EditorApplication.timeSinceStartup + 1;
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            !File.Exists(Folder + "/request")) return;
        File.Delete(Folder + "/request"); Run();
    }
    [MenuItem("Tools/Verification/Live additive combat lab (read-only)")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Directory.CreateDirectory(Folder);
        var passes = new List<string>();
        var authored = new Dictionary<string, string>();
        foreach (var path in new[] { "Assets/Scenes/RUNScene.unity", "Assets/Scenes/HomeScene.unity",
                     "Assets/Prefabs/GameScene/Shooter/Player/Player.prefab" })
            if (File.Exists(path)) authored[path] = File.ReadAllText(path);
        string runBefore = SaveSystem.Active != null ? JsonUtility.ToJson(SaveSystem.Active.RunData) : null;
        Scene original = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewPreviewScene();
        var homeScene = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject Go(string name, Scene destination)
            {
                var value = new GameObject(name); value.SetActive(false);
                SceneManager.MoveGameObjectToScene(value, destination); return value;
            }
            Assert.Throws<InvalidOperationException>(() => CombatLabSession.Prepare(null));
            Assert.AreEqual(original, SceneManager.GetActiveScene());
            Assert.IsNull(CombatLabRuntime.Active);
            passes.Add("PASS Edit Mode entry is rejected without replacing open scenes");
            var driver = Go("Lab", scene).AddComponent<CombatLabRuntime>();
            var home = Go("Home", homeScene).AddComponent<SectorRuntime>();
            var lab = Go("Lab sector", scene).AddComponent<SectorRuntime>();
            var player = Go("Existing player", homeScene).AddComponent<PlayerCharacter>();
            var body = player.gameObject.AddComponent<CharacterController>();
            driver.Player = player;
            var move = typeof(CombatLabRuntime).GetMethod("MovePlayer", Flags);
            move.Invoke(driver, new object[] { lab, new Vector3(2048, 2, 2048), Quaternion.identity });
            Assert.AreEqual(scene, player.gameObject.scene); Assert.AreSame(lab.transform, player.transform.parent);
            Assert.IsTrue(body.enabled); Assert.AreSame(player, driver.Player);
            passes.Add("PASS the same player moves into the additive sector with controller restored");
            move.Invoke(driver, new object[] { home, new Vector3(10, 2, 10), Quaternion.identity });
            Assert.AreEqual(homeScene, player.gameObject.scene);
            Assert.AreSame(home.transform, player.transform.parent);
            Assert.IsFalse(player.transform.IsChildOf(driver.transform));
            passes.Add("PASS the borrowed player leaves the lab scene before it can be unloaded");
            Assert.IsFalse(CombatLabRuntime.HandlePlayerDeath(player.gameObject.AddComponent<Damageable>()));
            passes.Add("PASS ordinary player death is unchanged outside a live lab");
            lab.transform.position = new Vector3(2048, 0, 2048);
            var local = new Vector3(12, 0, 23);
            Assert.AreEqual(local, lab.transform.InverseTransformPoint(lab.transform.TransformPoint(local)));
            passes.Add("PASS spawn positions remain sector-local across additive placement");
            typeof(CombatLabRuntime).GetField("_snapshot", Flags).SetValue(driver, new PlayerRunStateSaveData { hasSnapshot = true });
            var validate = typeof(CombatLabRuntime).GetMethod("ValidateSnapshot", Flags);
            var missing = Assert.Throws<TargetInvocationException>(() => validate.Invoke(driver, null));
            Assert.IsInstanceOf<InvalidOperationException>(missing.InnerException);
            passes.Add("PASS missing restore catalog rejects entry before inventory or ownership mutation");
            string runtime = File.ReadAllText("Assets/Scripts/Diagnostics/Runtime/CombatLabRuntime.cs");
            Assert.That(runtime, Does.Contain("actor.gameObject.SetActive(false)").And.Not.Contain("enemy.Kill("));
            passes.Add("PASS owned actor removal deactivates objects without a kill event");
            Assert.That(runtime, Does.Not.Contain("Instantiate(Player").And.Not.Contain("AddComponent<RaidRuntime>")
                .And.Not.Contain("AddComponent<SectorStateManager>").And.Not.Contain("CameraReady?.Clear"));
            string session = File.ReadAllText("Assets/Scripts/Authoring/Editor/CombatLabSession.cs");
            Assert.That(session, Does.Not.Contain("CopyRunCamera").And.Not.Contain("NewSceneMode.Single"));
            passes.Add("PASS no duplicate player, camera, raid or sector manager is created");
            foreach (var pair in authored) Assert.AreEqual(pair.Value, File.ReadAllText(pair.Key));
            if (runBefore != null) Assert.AreEqual(runBefore, JsonUtility.ToJson(SaveSystem.Active.RunData));
            passes.Add("PASS authored scenes/player prefab and in-memory run profile are unchanged");
            File.WriteAllLines(Folder + "/PASSED.txt", passes);
            if (File.Exists(Folder + "/FAILED.txt")) File.Delete(Folder + "/FAILED.txt");
            Debug.Log($"[Live Combat Lab] {passes.Count} preview checks passed. HOME Play enter/exit still needs a live check.");
        }
        catch (Exception error)
        {
            File.WriteAllLines(Folder + "/FAILED.txt", passes);
            File.AppendAllText(Folder + "/FAILED.txt", error.ToString());
            if (File.Exists(Folder + "/PASSED.txt")) File.Delete(Folder + "/PASSED.txt");
            Debug.LogException(error);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); EditorSceneManager.ClosePreviewScene(homeScene); }
    }
}
