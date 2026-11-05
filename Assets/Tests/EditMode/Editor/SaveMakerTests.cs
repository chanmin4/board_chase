using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>고유 Library 폴더에만 임시 ES3 파일을 생성한다. persistentDataPath, PlayerPrefs, 씬과 실제 세이브는 변경하지 않는다.</summary>
public sealed class SaveMakerTests
{
    internal string Root;
    internal readonly string[] Names = { "settings.es3", "profile_default.es3", "run_default.es3" };
    [SetUp] public void SetUp()
    {
        Root = Path.GetFullPath("Library/SaveMakerVerification/fixture-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
        Store(SaveMakerPartition.Settings, JsonUtility.ToJson(new DeviceSettingsSaveData { masterVolume = .5f, inputBindingOverridesJson = "custom-bindings" }));
        Store(SaveMakerPartition.Profile, JsonUtility.ToJson(new Save { _highestUnlockedStageIndex = 3 }));
        Store(SaveMakerPartition.Run, JsonUtility.ToJson(new RunSaveData()));
    }
    [TearDown] public void TearDown()
    {
        string allowed = Path.GetFullPath("Library/SaveMakerVerification") + Path.DirectorySeparatorChar;
        if (Root != null && Directory.Exists(Root))
        {
            Assert.That(Path.GetFullPath(Root).StartsWith(allowed, StringComparison.OrdinalIgnoreCase), Is.True);
            Assert.That(Path.GetFileName(Root), Does.StartWith("fixture-"));
            Directory.Delete(Root, true);
        }
    }
    private void Store(SaveMakerPartition p, string json, string relative = null) => ES3.Save(SaveMakerStorage.Key(p), json,
        new ES3Settings(SaveMakerStorage.Resolve(Root, relative ?? Names[(int)p]), ES3.Location.File));
    private SaveMakerStorage.Snapshot Read(SaveMakerPartition p) => SaveMakerStorage.Read(Root, p, Names[(int)p]);
    private SaveMakerStorage.Snapshot[] Originals() => Enumerable.Range(0, 3).Select(i => Read((SaveMakerPartition)i)).ToArray();
    private Dictionary<string, string> Hashes() => Directory.GetFiles(Root, "*", SearchOption.AllDirectories).ToDictionary(p => p, SaveMakerStorage.Fingerprint);
    private string Write(SaveMakerPartition p, string json) => SaveMakerStorage.Write(Root, Names, Originals(), new Dictionary<SaveMakerPartition, string> { [p] = json }, "fixture edit");

    [Test] public void ReadingAllPartitionsDoesNotWriteOrMigrateFiles()
    {
        Store(SaveMakerPartition.Run, "{\"schemaVersion\":1,\"playerRunState\":{\"hasSnapshot\":false}}");
        var before = Hashes(); foreach (var s in Originals()) Assert.That(s.Valid, Is.True, s.Error);
        Assert.That(Hashes(), Is.EqualTo(before)); Assert.That(JObject.Parse(Read(SaveMakerPartition.Run).Json)["schemaVersion"].Value<int>(), Is.EqualTo(1));
    }
    [Test] public void MissingReadDoesNotCreateDirectoryOrFile()
    {
        var result = SaveMakerStorage.Read(Root, SaveMakerPartition.Run, "absent/run.es3");
        Assert.That(result.Exists, Is.False); Assert.That(result.Error, Is.Null); Assert.That(Directory.Exists(Path.Combine(Root, "absent")), Is.False);
    }
    [Test] public void InvalidContainerIsReportedWithoutRepair()
    {
        string file = Path.Combine(Root, Names[2]); File.WriteAllText(file, "not an ES3 container");
        string before = SaveMakerStorage.Fingerprint(file); Assert.That(Read(SaveMakerPartition.Run).Valid, Is.False);
        Assert.That(SaveMakerStorage.Fingerprint(file), Is.EqualTo(before));
    }
    [Test] public void WrongPartitionKeyIsRejected()
    {
        var result = SaveMakerStorage.Read(Root, SaveMakerPartition.Profile, Names[0]);
        Assert.That(result.Valid, Is.False); Assert.That(result.Error, Does.Contain("ES3"));
    }
    [Test] public void FutureSchemaIsRejectedWithoutModification()
    {
        Store(SaveMakerPartition.Profile, "{\"schemaVersion\":999,\"_highestUnlockedStageIndex\":4}");
        var before = Hashes(); Assert.That(Read(SaveMakerPartition.Profile).Valid, Is.False); Assert.That(Hashes(), Is.EqualTo(before));
    }
    [Test] public void EmptyAndWrongShapeJsonAreRejected()
    {
        Assert.Throws<IOException>(() => SaveMakerStorage.ValidateJson(SaveMakerPartition.Profile, "{}"));
        Assert.Throws<IOException>(() => SaveMakerStorage.ValidateJson(SaveMakerPartition.Run, JsonUtility.ToJson(new DeviceSettingsSaveData())));
    }
    [Test] public void DuplicatePathsIncludingDifferentCaseAreRejected()
    { Assert.That(SaveMakerStorage.ValidateFilenames(new[] { "Settings.es3", "settings.es3", "run.es3" }), Is.Not.Null); }
    [Test] public void UnsafeAndReservedFilenamesAreRejected()
    {
        foreach (string value in new[] { "../profile.es3", "sub/profile.es3", "C:\\save.es3", "CON.es3", "LPT1.es3", "bad:stream.es3", "save.json", " profile.es3" })
            Assert.That(SaveMakerStorage.ValidateFilenames(new[] { "settings.es3", value, "run.es3" }), Is.Not.Null, value);
    }
    [Test] public void PlainDistinctFilenamesAreValid()
    { Assert.That(SaveMakerStorage.ValidateFilenames(Names), Is.Null); }
    [Test] public void RootEscapePathsAreRejected()
    {
        foreach (string value in new[] { "../outside.es3", "C:\\outside.es3", "sub/../../outside.es3", "a:stream", "./settings.es3" })
            Assert.Throws<IOException>(() => SaveMakerStorage.Resolve(Root, value), value);
    }
    [Test] public void BackupNamesMatchRuntimeConvention()
    { Assert.That(SaveMakerStorage.BackupName("profile_default.es3", 3), Is.EqualTo("backups/profile_default.backup.3.es3")); }
    [Test] public void SettingsPatchPreservesBindingsAndUnknownFields()
    {
        var obj = JObject.Parse(Read(SaveMakerPartition.Settings).Json); obj["extension"] = new JObject { ["keep"] = 42 };
        var draft = JsonUtility.FromJson<DeviceSettingsSaveData>(obj.ToString()); draft.musicVolume = .25f; draft.inputBindingOverridesJson = "should-not-overwrite";
        var result = JObject.Parse(SaveMakerStorage.PatchSettings(obj.ToString(), draft));
        Assert.That(result["inputBindingOverridesJson"].Value<string>(), Is.EqualTo("custom-bindings"));
        Assert.That(result["extension"]["keep"].Value<int>(), Is.EqualTo(42)); Assert.That(result["musicVolume"].Value<float>(), Is.EqualTo(.25f));
    }
    [Test] public void WritingSettingsPreservesOtherPartitionsAndArchivesOriginals()
    {
        var before = Originals(); var settings = JsonUtility.FromJson<DeviceSettingsSaveData>(before[0].Json); settings.masterVolume = .2f;
        string archive = Write(SaveMakerPartition.Settings, SaveMakerStorage.PatchSettings(before[0].Json, settings));
        Assert.That(Read(SaveMakerPartition.Profile).Fingerprint, Is.EqualTo(before[1].Fingerprint));
        Assert.That(Read(SaveMakerPartition.Run).Fingerprint, Is.EqualTo(before[2].Fingerprint));
        Assert.That(SaveMakerStorage.Fingerprint(Path.Combine(archive, Names[0])), Is.EqualTo(before[0].Fingerprint));
        Assert.That(File.Exists(Path.Combine(archive, "manifest.json")), Is.True);
        Assert.That(JsonUtility.FromJson<DeviceSettingsSaveData>(Read(SaveMakerPartition.Settings).Json).masterVolume, Is.EqualTo(.2f));
    }
    [Test] public void OtherEs3KeysSurviveTargetKeyEdits()
    {
        var settings = new ES3Settings(Path.Combine(Root, Names[0]), ES3.Location.File); ES3.Save("Extra.Key", "keep", settings);
        Write(SaveMakerPartition.Settings, JsonUtility.ToJson(new DeviceSettingsSaveData()));
        Assert.That(ES3.Load<string>("Extra.Key", settings), Is.EqualTo("keep"));
    }
    [Test] public void ChangedPrimaryRefusesWriteAndDoesNotMakeBackup()
    {
        var before = Originals(); Store(SaveMakerPartition.Profile, JsonUtility.ToJson(new Save { _highestUnlockedStageIndex = 8 }));
        Assert.Throws<IOException>(() => SaveMakerStorage.Write(Root, Names, before,
            new Dictionary<SaveMakerPartition, string> { [SaveMakerPartition.Settings] = JsonUtility.ToJson(new DeviceSettingsSaveData()) }, "stale"));
        Assert.That(Directory.Exists(Path.Combine(Root, "SaveMakerBackups")), Is.False);
    }
    [Test] public void MissingExpectedTargetIsRejected()
    {
        Assert.Throws<IOException>(() => SaveMakerStorage.Write(Root, Names, Array.Empty<SaveMakerStorage.Snapshot>(),
            new Dictionary<SaveMakerPartition, string> { [SaveMakerPartition.Run] = JsonUtility.ToJson(new RunSaveData()) }, "missing proof"));
    }
    [Test] public void InvalidReplacementIsRejectedBeforeAnyWrite()
    {
        var before = Hashes(); Assert.Throws<IOException>(() => Write(SaveMakerPartition.Run, "{}")); Assert.That(Hashes(), Is.EqualTo(before));
    }
    [Test] public void MultiPartitionFailureRollsBackCommittedFile()
    {
        var before = Originals();
        Assert.Throws<IOException>(() => SaveMakerStorage.Write(Root, Names, before, new Dictionary<SaveMakerPartition, string>
        { [SaveMakerPartition.Profile] = SaveMakerStorage.FreshProfile(new[] { "home_shop" }), [SaveMakerPartition.Run] = JsonUtility.ToJson(new RunSaveData()) },
            "forced rollback", n => { if (n == 1) throw new IOException("simulated failure"); }));
        Assert.That(Originals().Select(s => s.Fingerprint), Is.EqualTo(before.Select(s => s.Fingerprint)));
        Assert.That(Directory.GetFiles(Root, "*.tmp"), Is.Empty);
    }
    [Test] public void ManualArchiveLeavesPrimariesAndRotatingBackupsIntact()
    {
        Store(SaveMakerPartition.Profile, Read(SaveMakerPartition.Profile).Json, SaveMakerStorage.BackupName(Names[1], 1));
        var before = Hashes(); string archive = SaveMakerStorage.Archive(Root, Names, "manual");
        foreach (var pair in before) Assert.That(SaveMakerStorage.Fingerprint(pair.Key), Is.EqualTo(pair.Value));
        Assert.That(File.Exists(Path.Combine(archive, "backups/profile_default.backup.1.es3")), Is.True);
    }
    [Test] public void FreshProfileBlocksLegacyStageAndShopReimport()
    {
        var profile = JsonUtility.FromJson<Save>(SaveMakerStorage.FreshProfile(new[] { "home_shop", "home_shop", "extra", "" }));
        Assert.That(profile.HighestUnlockedStageIndex, Is.Zero);
        Assert.That(profile.ShopUnlockProgress.Count, Is.EqualTo(2));
        Assert.That(profile.ShopUnlockProgress.All(s => s.unlockedNodeIds.Count == 0), Is.True); Assert.That(profile.QuestProgress, Is.Empty);
    }
    [Test] public void RunOnlyResetPreservesProfileAndSettings()
    {
        var before = Originals(); Write(SaveMakerPartition.Run, JsonUtility.ToJson(new RunSaveData()));
        Assert.That(Read(SaveMakerPartition.Profile).Fingerprint, Is.EqualTo(before[1].Fingerprint));
        Assert.That(Read(SaveMakerPartition.Settings).Fingerprint, Is.EqualTo(before[0].Fingerprint));
    }
    [Test] public void CorruptPrimaryCanBeResetAfterRawOriginalIsArchived()
    {
        string file = Path.Combine(Root, Names[2]); File.WriteAllText(file, "corrupt original"); string hash = SaveMakerStorage.Fingerprint(file);
        string archive = Write(SaveMakerPartition.Run, JsonUtility.ToJson(new RunSaveData()));
        Assert.That(Read(SaveMakerPartition.Run).Valid, Is.True);
        Assert.That(SaveMakerStorage.Fingerprint(Path.Combine(archive, Names[2])), Is.EqualTo(hash));
    }
    [Test] public void NewlyCreatedFileIsRemovedOnRollback()
    {
        File.Delete(Path.Combine(Root, Names[2])); var before = Originals();
        Assert.Throws<IOException>(() => SaveMakerStorage.Write(Root, Names, before,
            new Dictionary<SaveMakerPartition, string> { [SaveMakerPartition.Run] = JsonUtility.ToJson(new RunSaveData()) }, "rollback new", n => throw new IOException("fail")));
        Assert.That(File.Exists(Path.Combine(Root, Names[2])), Is.False);
    }
}
