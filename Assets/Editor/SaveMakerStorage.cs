using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

internal enum SaveMakerPartition { Settings, Profile, Run }

/// <summary>
/// Save Maker의 디스크 경계. SaveSystem.Load/Save를 호출하지 않아 조회 중 마이그레이션/자동 저장이 발생하지 않는다.
/// 쓰기는 명시적으로 요청한 파일만, Play Mode 밖에서, 변경 감지와 원본 백업 이후 실행한다.
/// </summary>
internal static class SaveMakerStorage
{
    internal sealed class Snapshot
    {
        internal SaveMakerPartition Partition;
        internal string RelativePath, AbsolutePath, Fingerprint, Json, Error;
        internal bool Exists, Valid;
        internal long Size;
        internal DateTime Modified;
        internal string Status => !string.IsNullOrEmpty(Error) ? "확인 필요" : !Exists ? "아직 저장 없음" : "정상";
    }
    internal static string Key(SaveMakerPartition partition) => partition switch
    {
        SaveMakerPartition.Settings => SaveSystem.SettingsSaveKey,
        SaveMakerPartition.Profile => SaveSystem.ProfileSaveKey,
        _ => SaveSystem.RunSaveKey
    };
    internal static string Label(SaveMakerPartition partition) => partition switch
    {
        SaveMakerPartition.Settings => "기기 설정",
        SaveMakerPartition.Profile => "영구 진행",
        _ => "현재 런"
    };
    internal static string BackupName(string filename, int generation) =>
        $"backups/{Path.GetFileNameWithoutExtension(filename)}.backup.{generation}{Path.GetExtension(filename)}";

    internal static string ValidateFilenames(IReadOnlyList<string> names)
    {
        if (names == null || names.Count != 3) return "설정·프로필·런 파일을 각각 지정하세요.";
        var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string name in names)
        {
            if (string.IsNullOrWhiteSpace(name) || name != name.Trim() || name != Path.GetFileName(name) ||
                name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.Contains('/') || name.Contains('\\') ||
                !name.EndsWith(".es3", StringComparison.OrdinalIgnoreCase))
                return "파일명은 폴더 없이 입력하세요. 예: profile_default.es3";
            string stem = Path.GetFileNameWithoutExtension(name).Split('.')[0].ToUpperInvariant();
            if (new[] { "CON", "PRN", "AUX", "NUL" }.Contains(stem) ||
                (stem.Length == 4 && (stem.StartsWith("COM") || stem.StartsWith("LPT")) && char.IsDigit(stem[3])))
                return "Windows 예약 파일명은 사용할 수 없습니다.";
            if (!unique.Add(name)) return "세 파티션의 파일명은 서로 달라야 합니다.";
        }
        return null;
    }

    // 외부 경로/상위 경로/링크를 통한 탈출은 읽기에서도 거부한다.
    internal static string Resolve(string root, string relative)
    {
        if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative) ||
            relative.Replace('\\', '/').Split('/').Any(p => p == ".." || p == "." || p.Length == 0) || relative.Contains(':'))
            throw new IOException("저장 폴더 바깥 경로는 사용할 수 없습니다.");
        string directory = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string path = Path.GetFullPath(Path.Combine(directory, relative));
        if (!path.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new IOException("Invalid save path.");
        string check = path;
        while (!string.IsNullOrEmpty(check))
        {
            if ((File.Exists(check) || Directory.Exists(check)) && (File.GetAttributes(check) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("링크된 저장 경로는 안전한 파일 작업을 위해 지원하지 않습니다.");
            check = Path.GetDirectoryName(check);
        }
        return path;
    }
    internal static string Fingerprint(string path)
    {
        if (!File.Exists(path)) return "missing";
        using var sha = SHA256.Create();
        using var stream = File.OpenRead(path);
        return Convert.ToBase64String(sha.ComputeHash(stream));
    }
    private static ES3Settings FileSettings(string absolute) => new ES3Settings(absolute, ES3.Location.File);

    internal static Snapshot Read(string root, SaveMakerPartition partition, string relative)
    {
        var result = new Snapshot { Partition = partition, RelativePath = relative };
        try
        {
            result.AbsolutePath = Resolve(root, relative);
            result.Fingerprint = Fingerprint(result.AbsolutePath);
            result.Exists = File.Exists(result.AbsolutePath);
            if (!result.Exists) return result;
            var info = new FileInfo(result.AbsolutePath); result.Size = info.Length; result.Modified = info.LastWriteTime;
            var settings = FileSettings(result.AbsolutePath);
            if (!ES3.KeyExists(Key(partition), settings)) throw new IOException("ES3 키가 없습니다: " + Key(partition));
            result.Json = ES3.Load<string>(Key(partition), settings);
            ValidateJson(partition, result.Json);
            if (Fingerprint(result.AbsolutePath) != result.Fingerprint) throw new IOException("읽는 중 파일이 변경됐습니다. Refresh 하세요.");
            result.Valid = true;
        }
        catch (Exception ex) { result.Error = ex.Message; }
        return result;
    }

    internal static void ValidateJson(SaveMakerPartition partition, string json)
    {
        var obj = JObject.Parse(json ?? "");
        string marker = partition switch { SaveMakerPartition.Settings => "masterVolume", SaveMakerPartition.Profile => "_highestUnlockedStageIndex", _ => "playerRunState" };
        if (obj[marker] == null) throw new IOException("다른 종류이거나 불완전한 세이브입니다: " + marker);
        if (obj["schemaVersion"] != null && obj["schemaVersion"].Type != JTokenType.Integer) throw new IOException("잘못된 schemaVersion입니다.");
        string error;
        bool valid;
        switch (partition)
        {
            case SaveMakerPartition.Settings: valid = SaveMigration.TryMigrateSettings(JsonUtility.FromJson<DeviceSettingsSaveData>(json), out error); break;
            case SaveMakerPartition.Profile: valid = SaveMigration.TryMigrateProfile(JsonUtility.FromJson<Save>(json), out error); break;
            default: valid = SaveMigration.TryMigrateRun(JsonUtility.FromJson<RunSaveData>(json), out error); break;
        }
        if (!valid) throw new IOException(error);
    }

    internal static string FreshProfile(IEnumerable<string> shopIds)
    {
        // -1은 옛 PlayerPrefs 재수입 신호다. 명시적 초기화는 0 + 빈 상점 기록으로 옛 진행 복원을 막는다.
        var profile = new Save { _highestUnlockedStageIndex = 0 };
        foreach (string id in shopIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct())
            profile.ShopUnlockProgress.Add(new ShopUnlockProgressSaveData { configId = id });
        return JsonUtility.ToJson(profile);
    }

    internal static string PatchSettings(string original, DeviceSettingsSaveData draft)
    {
        ValidateJson(SaveMakerPartition.Settings, original);
        draft.EnsureDefaults();
        var obj = JObject.Parse(original);
        // 알려진 편집 필드만 덮어쓴다. 바인딩과 확장 필드는 원문 그대로 보존한다.
        var values = JObject.Parse(JsonUtility.ToJson(draft));
        foreach (var field in values.Properties())
            if (field.Name != "inputBindingOverridesJson") obj[field.Name] = field.Value.DeepClone();
        return obj.ToString(Newtonsoft.Json.Formatting.None);
    }

    internal static string WriteBlockedReason(SaveSystem config = null, bool inspectCache = true)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            return "Play Mode·컴파일·에셋 갱신 중에는 디스크 변경을 할 수 없습니다.";
        if (inspectCache && ((config != null && config.HasLoadedSaveData) || (SaveSystem.Active != null && SaveSystem.Active.HasLoadedSaveData)))
            return "Unity 메모리에 이전 세이브가 로드되어 있습니다. 스크립트 재로드 후 파일을 변경하세요.";
        return null;
    }

    internal static string Archive(string root, IReadOnlyList<string> names, string reason)
    {
        EnsureWritable(root, names);
        string relative = "SaveMakerBackups/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N");
        string folder = Resolve(root, relative);
        Directory.CreateDirectory(folder);
        var manifest = new JObject { ["reason"] = reason, ["createdUtc"] = DateTime.UtcNow.ToString("O") };
        var files = new JArray();
        foreach (string name in names.SelectMany(n => new[] { n, BackupName(n, 1), BackupName(n, 2), BackupName(n, 3) }))
        {
            string source = Resolve(root, name);
            if (!File.Exists(source)) continue;
            string destination = Resolve(folder, name); Directory.CreateDirectory(Path.GetDirectoryName(destination));
            File.Copy(source, destination, false); files.Add(name);
        }
        manifest["files"] = files;
        File.WriteAllText(Resolve(folder, "manifest.json"), manifest.ToString());
        return folder;
    }

    /// <summary>
    /// 원본 예상 해시를 다시 확인하고 모든 임시 파일을 검증한 뒤 교체한다.
    /// 복수 파티션 중 하나라도 실패하면 이미 교체한 원본을 복구한다. 원본 아카이브는 항상 남긴다.
    /// </summary>
    internal static string Write(string root, IReadOnlyList<string> names, IReadOnlyList<Snapshot> expected,
        IReadOnlyDictionary<SaveMakerPartition, string> changes, string reason, Action<int> afterCommit = null)
    {
        EnsureWritable(root, names);
        if (changes.Count == 0) throw new ArgumentException("변경할 파티션이 없습니다.");
        foreach (var change in changes) ValidateJson(change.Key, change.Value);
        void CheckOriginals()
        {
            foreach (var original in expected)
                if (Fingerprint(Resolve(root, original.RelativePath)) != original.Fingerprint)
                    throw new IOException("세이브가 마지막 조회 이후 변경됐습니다. Refresh 후 다시 확인하세요: " + original.RelativePath);
        }
        foreach (var change in changes)
            if (!expected.Any(e => e.Partition == change.Key && e.RelativePath == names[(int)change.Key]))
                throw new IOException("대상 원본 확인 정보가 없습니다.");
        CheckOriginals();
        string archive = Archive(root, names, reason);
        var staged = new Dictionary<string, string>(); var originals = new Dictionary<string, byte[]>();
        var committed = new List<string>();
        try
        {
            foreach (var change in changes)
            {
                string name = names[(int)change.Key], target = Resolve(root, name);
                string temp = Resolve(root, name + ".savemaker-" + Guid.NewGuid().ToString("N") + ".tmp");
                originals[target] = File.Exists(target) ? File.ReadAllBytes(target) : null;
                staged[target] = temp;
                if (File.Exists(target)) File.Copy(target, temp, false);
                // 손상된 원본은 수동 복구/초기화 대상이다. 파싱 불가라면 백업된 원본을 보존하고 새 컨테이너를 만든다.
                if (File.Exists(temp))
                {
                    try { ES3.GetKeys(FileSettings(temp)); }
                    catch { File.Delete(temp); }
                }
                ES3.Save(Key(change.Key), change.Value, FileSettings(temp));
                string roundTrip = ES3.Load<string>(Key(change.Key), FileSettings(temp));
                if (roundTrip != change.Value) throw new IOException("임시 파일 round-trip 검증 실패.");
                ValidateJson(change.Key, roundTrip);
            }
            CheckOriginals();
            foreach (var pair in staged)
            {
                if (File.Exists(pair.Key)) File.Replace(pair.Value, pair.Key, null); else File.Move(pair.Value, pair.Key);
                committed.Add(pair.Key); afterCommit?.Invoke(committed.Count);
            }
            return archive;
        }
        catch (Exception error)
        {
            var rollbackErrors = new List<string>();
            foreach (string target in committed.AsEnumerable().Reverse())
            {
                try
                {
                    byte[] bytes = originals[target];
                    if (bytes == null) File.Delete(target);
                    else
                    {
                        string recovery = Resolve(root, Path.GetFileName(target) + ".restore-" + Guid.NewGuid().ToString("N"));
                        File.WriteAllBytes(recovery, bytes);
                        if (File.Exists(target)) File.Replace(recovery, target, null); else File.Move(recovery, target);
                    }
                }
                catch (Exception ex) { rollbackErrors.Add(ex.Message); }
            }
            throw new IOException(error.Message + "\n원본 백업: " + archive + (rollbackErrors.Count == 0 ? "\n교체한 파일은 원래 상태로 복구했습니다." : "\n복구 확인 필요: " + string.Join("; ", rollbackErrors)), error);
        }
        finally { foreach (string temp in staged.Values) if (File.Exists(temp)) File.Delete(temp); }
    }

    private static void EnsureWritable(string root, IReadOnlyList<string> names)
    {
        // 별도 테스트 폴더 작업은 실제 플레이어 메모리 캐시와 무관하다. Play/컴파일 잠금은 동일하다.
        bool productionRoot = string.Equals(Path.GetFullPath(root).TrimEnd('/', '\\'), Path.GetFullPath(Application.persistentDataPath).TrimEnd('/', '\\'), StringComparison.OrdinalIgnoreCase);
        string error = WriteBlockedReason(inspectCache: productionRoot) ?? ValidateFilenames(names);
        if (error != null) throw new InvalidOperationException(error);
    }
}
