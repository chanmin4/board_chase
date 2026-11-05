using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Opt-in read-only native Preview Scene checks. Never enters Play Mode or saves user assets.</summary>
public static class PortalActivationVerification
{
    private const string Output = "Library/PortalActivationVerification";
    private static double _next;
    [InitializeOnLoadMethod] private static void Register() => EditorApplication.update += Poll;
    private static void Poll()
    {
        if (EditorApplication.timeSinceStartup < _next) return;
        _next = EditorApplication.timeSinceStartup + 1;
        if (!File.Exists(Output + "/request") || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Output + "/request");
        Run();
    }

    [MenuItem("Tools/Verification/Portal activation and faction assignments (read-only)")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Directory.CreateDirectory(Output);
        var passes = new System.Collections.Generic.List<string>();
        var save = SaveSystem.Active;
        string before = save != null ? JsonUtility.ToJson(save.RunData) : null;
        var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();
        var dirty = scenes.Select(s => s.isDirty).ToArray();
        var active = SceneManager.GetActiveScene();
        string[] names = {
            nameof(RaidExtractionPortalTests.DeviceActivationIsRequiredEvenAfterClearAndPersistsWithoutRerollOrClear),
            nameof(RaidExtractionPortalTests.FreshStartAndOtherRoomsDoNotPowerEachOther),
            nameof(RaidExtractionPortalTests.DeviceAndEveryPortalUseSeparateFiveSecondCastsWithoutCooldown),
            nameof(RaidExtractionPortalTests.FactionAssignmentsIncludeStageTwoAndThreeBossesAndGuards),
            nameof(RaidExtractionPortalTests.EveryAuthoredRoomInheritsExactlyOneDeviceAndItsReplaceableVisual),
            nameof(RaidExtractionPortalTests.PreplacedPortalIsHiddenUntilRoomClearThenOpensWithoutPowerAndClosesOnExtractionCommit),
            nameof(RaidExtractionPortalTests.WrongDirectionOtherSectorAndDuplicateExtractionCannotUseRaidPortal),
            nameof(RaidExtractionPortalTests.ExistingNeighbourTravelAndArrivalPortalArePreservedBesideExtraction),
            nameof(RaidExtractionPortalTests.ExtractionPortalUsesTheExistingInteractionTypeAndLocalizedExtractLabel)
        };
        try
        {
            foreach (string name in names)
            {
                var fixture = new RaidExtractionPortalTests();
                try { fixture.Setup(); typeof(RaidExtractionPortalTests).GetMethod(name).Invoke(fixture, null); passes.Add("PASS " + name); }
                finally { fixture.Cleanup(); }
            }
            Assert.AreEqual(active, SceneManager.GetActiveScene());
            Assert.AreSame(save, SaveSystem.Active);
            if (save != null) Assert.AreEqual(before, JsonUtility.ToJson(save.RunData));
            for (int i = 0; i < scenes.Length; i++) Assert.AreEqual(dirty[i], scenes[i].isDirty, scenes[i].name);
            passes.Add("PASS user run data, active scene and dirty flags preserved; no Play Mode entered");
            File.WriteAllLines(Output + "/PASSED.txt", passes);
            Debug.Log("[Portal activation] " + passes.Count + " native checks passed.");
        }
        catch (Exception error)
        {
            File.WriteAllLines(Output + "/FAILED.txt", passes.Concat(new[] { error.ToString() }));
            Debug.LogException(error);
        }
    }
}
