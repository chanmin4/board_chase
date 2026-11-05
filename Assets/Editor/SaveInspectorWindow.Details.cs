using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>디스크 상태 표시, 기기 설정 초안, 확인 후 실행하는 복구/초기화. 자동 쓰기 없음.</summary>
public sealed partial class SaveInspectorWindow
{
    private readonly HashSet<string> _expanded = new();
    private bool DiskActionsAllowed => SaveMakerStorage.WriteBlockedReason(_saveSystemAsset) == null &&
        SaveMakerStorage.ValidateFilenames(_names) == null && !_configDirty;

    private void DrawPartition(SaveMakerPartition partition)
    {
        var snapshot = _snapshots[partition][0];
        EditorGUILayout.LabelField(SaveMakerStorage.Label(partition) + " · 실제 저장 파일", EditorStyles.boldLabel);
        DrawFileInfo(snapshot);
        if (!snapshot.Valid) return;
        DrawSummary(snapshot);
        if (partition == SaveMakerPartition.Settings) DrawSettingsDraft(snapshot);
        else EditorGUILayout.HelpBox("퀘스트 지급 기록·아이템 GUID·레이드 상태는 서로 연결되어 있어 여기서는 읽기 전용입니다. 복구/초기화는 왼쪽의 전용 페이지에서 실행하세요.", MessageType.None);
        DrawJson(snapshot);
    }
    private void DrawFileInfo(SaveMakerStorage.Snapshot snapshot)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(snapshot.RelativePath + "  ·  " + snapshot.Status, EditorStyles.boldLabel);
            if (!string.IsNullOrEmpty(snapshot.Error)) EditorGUILayout.HelpBox(snapshot.Error, MessageType.Warning);
            if (!snapshot.Exists) { EditorGUILayout.LabelField("게임이 아직 이 파일을 저장하지 않았을 수 있습니다.", EditorStyles.wordWrappedMiniLabel); return; }
            EditorGUILayout.LabelField("마지막 저장", snapshot.Modified.ToString("yyyy-MM-dd HH:mm:ss"));
            EditorGUILayout.LabelField("파일 크기", snapshot.Size.ToString("N0") + " bytes");
            EditorGUILayout.SelectableLabel(snapshot.AbsolutePath ?? "", EditorStyles.textField, GUILayout.Height(22));
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("파일 위치 보기") && snapshot.AbsolutePath != null) EditorUtility.RevealInFinder(snapshot.AbsolutePath);
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(snapshot.Json)))
                    if (GUILayout.Button("JSON 복사")) EditorGUIUtility.systemCopyBuffer = snapshot.Json;
            }
        }
    }
    private void DrawJson(SaveMakerStorage.Snapshot snapshot)
    {
        bool next = EditorGUILayout.Foldout(_jsonView == snapshot.RelativePath, "Advanced: 원본 JSON (읽기 전용)", true);
        if (next) _jsonView = snapshot.RelativePath; else if (_jsonView == snapshot.RelativePath) _jsonView = "";
        if (next)
        {
            string pretty = JObject.Parse(snapshot.Json).ToString();
            using (new EditorGUI.DisabledScope(true)) EditorGUILayout.TextArea(pretty, GUILayout.MinHeight(180));
        }
    }
    private void DrawSummary(SaveMakerStorage.Snapshot snapshot)
    {
        var obj = JObject.Parse(snapshot.Json);
        EditorGUILayout.LabelField("저장 스키마", obj["schemaVersion"]?.ToString() ?? "Legacy / 0");
        if (snapshot.Partition == SaveMakerPartition.Settings)
        {
            var data = JsonUtility.FromJson<DeviceSettingsSaveData>(snapshot.Json);
            EditorGUILayout.LabelField("언어", string.IsNullOrEmpty(data.localeCode) ? "기본 언어" : data.localeCode);
            EditorGUILayout.LabelField("키 바인딩", string.IsNullOrEmpty(data.inputBindingOverridesJson) ? "기본값" : "사용자 바인딩 있음 (아래 설정 편집 시 유지)");
        }
        else if (snapshot.Partition == SaveMakerPartition.Profile)
        {
            var data = JsonUtility.FromJson<Save>(snapshot.Json); data.EnsureRuntimeDefaults();
            EditorGUILayout.LabelField("최고 해금 스테이지 인덱스", data.HighestUnlockedStageIndex.ToString());
            EditorGUILayout.LabelField("계승 창고", data.InheritanceWarehouse.items.Count + " / " + data.InheritanceWarehouse.capacity + "칸");
            DrawRows("profile:quests", "퀘스트 진행", data.QuestProgress.Select(q => q.questId + " · " + (q.completed ? "완료" : q.accepted ? "진행 중" : "미수락") + " · 보상 " + q.rewardGranted.Count(v => v) + "/" + q.rewardGranted.Length));
            DrawRows("profile:shop", "상점 해금", data.ShopUnlockProgress.Select(s => s.configId + " → " + (s.unlockedNodeIds.Count == 0 ? "없음" : string.Join(", ", s.unlockedNodeIds))));
            DrawRows("profile:warehouse", "계승 창고 아이템", data.InheritanceWarehouse.items.Select(i => "칸 " + i.slotIndex + " · " + ItemName(i.itemGuid) + " × " + i.amount));
            var meta = data.MetaUpgrades;
            DrawRows("profile:meta", "영구 강화", new[] { "체력: " + meta.maxHealthLevel, "공격력: " + meta.attackDamageLevel, "탄창: " + meta.magazineSizeLevel, "이동 속도: " + meta.moveSpeedLevel, "재장전: " + meta.reloadDurationLevel, "공격 속도: " + meta.attackSpeedLevel, "페인트 속도: " + meta.paintSpeedLevel });
            EditorGUILayout.LabelField("퀘스트 영구 보상", "스탯 " + data.QuestStatRewards.Count + " / 창고 " + data.QuestWarehouseSlotRewards.Count);
        }
        else
        {
            var data = JsonUtility.FromJson<RunSaveData>(snapshot.Json); data.EnsureDefaults();
            var state = data.PlayerRunState;
            EditorGUILayout.LabelField("플레이어 스냅샷", state.hasSnapshot ? "있음" : "없음");
            EditorGUILayout.LabelField("진행 중 레이드", data.raid.active ? "있음" : "없음");
            EditorGUILayout.LabelField("체력 / 감염", state.currentHealth + " / " + state.currentInfection);
            EditorGUILayout.LabelField("레이드 스테이지 / 남은 시간", (data.raid.layout?.stageIndex.ToString() ?? "-") + " / " + data.raid.remainingSeconds.ToString("0.#") + "초");
            EditorGUILayout.LabelField("레이드 진행", "전력 " + data.raid.generatorPowered + " · 해치 " + data.raid.hatchRevealed + " · 보스 " + data.raid.bossDefeated, EditorStyles.wordWrappedLabel);
            DrawRows("run:inventory", "인벤토리", state.inventorySlots.Select(i => "칸 " + i.slotIndex + " · " + ItemName(i.itemGuid) + " × " + i.amount));
            DrawRows("run:weapons", "장착 무기", state.equippedWeapons.Select(w => "칸 " + w.slotIndex + " · " + ItemName(w.weaponGuid)));
            DrawRows("run:shop", "현재 상점 진열 / 재고", state.shopStates.Select(s => s.shopStateId + " · 탄약 " + s.bulletOffers.Count + " / 상품 " + s.itemOffers.Count + " · 재입고 " + s.restockRevision));
        }
    }
    private static string ItemName(string guid)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid ?? "");
        return string.IsNullOrEmpty(path) ? "Missing: " + guid : Path.GetFileNameWithoutExtension(path);
    }
    private void DrawRows(string key, string title, IEnumerable<string> source)
    {
        var rows = source.ToList(); bool open = _expanded.Contains(key);
        bool next = EditorGUILayout.Foldout(open, title + " (" + rows.Count + ")", true);
        if (next) _expanded.Add(key); else _expanded.Remove(key);
        if (!next) return;
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            foreach (string row in rows.Take(100)) EditorGUILayout.LabelField(row, EditorStyles.wordWrappedLabel);
            if (rows.Count > 100) EditorGUILayout.LabelField("100개까지 표시합니다. 전체는 원본 JSON을 확인하세요.");
        }
    }
    private void DrawSettingsDraft(SaveMakerStorage.Snapshot snapshot)
    {
        if (_settingsDraft == null) return;
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("이 PC의 기기 설정 편집", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("초안을 조정한 뒤 적용을 누르면 settings 파일만 변경합니다. 키 바인딩·프로필·런은 유지합니다. Ctrl+Z 대신 적용 전 원본 백업으로 복구합니다.", MessageType.Info);
        using (new EditorGUI.DisabledScope(!DiskActionsAllowed))
        {
            EditorGUI.BeginChangeCheck();
            _settingsDraft.masterVolume = EditorGUILayout.Slider("마스터 볼륨", _settingsDraft.masterVolume, 0, 1);
            _settingsDraft.musicVolume = EditorGUILayout.Slider("음악 볼륨", _settingsDraft.musicVolume, 0, 1);
            _settingsDraft.sfxVolume = EditorGUILayout.Slider("효과음 볼륨", _settingsDraft.sfxVolume, 0, 1);
            _settingsDraft.localeCode = EditorGUILayout.TextField("언어 코드", _settingsDraft.localeCode);
            _settingsDraft.isFullscreen = EditorGUILayout.Toggle("전체 화면", _settingsDraft.isFullscreen);
            _settingsDraft.useVSync = EditorGUILayout.Toggle("VSync", _settingsDraft.useVSync);
            _settingsDraft.targetFrameRate = EditorGUILayout.IntField("목표 FPS", _settingsDraft.targetFrameRate);
            _settingsDraft.aimSensitivity = EditorGUILayout.FloatField("조준 감도", _settingsDraft.aimSensitivity);
            if (EditorGUI.EndChangeCheck()) _settingsDirty = true;
            using (new EditorGUI.DisabledScope(!_settingsDirty))
                if (GUILayout.Button("기기 설정 적용...", GUILayout.Height(28)) && EditorUtility.DisplayDialog("기기 설정 적용", snapshot.AbsolutePath + "\n\n이 파일만 변경합니다. 현재 키 바인딩은 유지하고 적용 전 원본 백업을 만듭니다.", "백업 후 적용", "취소"))
                    ExecuteWrite(new Dictionary<SaveMakerPartition, string> { [SaveMakerPartition.Settings] = SaveMakerStorage.PatchSettings(snapshot.Json, _settingsDraft) }, "Device settings edit");
            if (GUILayout.Button("기기 설정 초안 되돌리기")) { _settingsDraft = JsonUtility.FromJson<DeviceSettingsSaveData>(snapshot.Json); _settingsDirty = false; }
        }
    }

    private void DrawRecovery()
    {
        EditorGUILayout.LabelField("백업 / 복구", EditorStyles.boldLabel);
        if (_configDirty) EditorGUILayout.HelpBox("먼저 프로젝트 설정 초안을 적용하거나 되돌리세요.", MessageType.Warning);
        using (new EditorGUI.DisabledScope(!DiskActionsAllowed))
            if (GUILayout.Button("현재 파일 전체 백업 만들기 (원본 유지)", GUILayout.Height(30)))
                TryAction(() => { _lastArchive = SaveMakerStorage.Archive(Root, _names, "Manual backup"); _message = "원본을 변경하지 않고 백업했습니다: " + _lastArchive; });
        if (!string.IsNullOrEmpty(_lastArchive))
        {
            EditorGUILayout.SelectableLabel(_lastArchive, EditorStyles.textField, GUILayout.Height(22));
            if (GUILayout.Button("최근 원본 백업 폴더 열기")) EditorUtility.RevealInFinder(_lastArchive);
        }
        _recoveryPartition = EditorGUILayout.Popup("복구할 파티션", _recoveryPartition, new[] { "기기 설정", "영구 진행", "현재 런" });
        var partition = (SaveMakerPartition)_recoveryPartition;
        EditorGUILayout.HelpBox(partition == SaveMakerPartition.Profile
            ? "영구 진행 복구 시 현재 런도 초기화합니다. 다른 시점의 프로필과 런이 섞이는 것을 막기 위한 처리이며, 기기 설정은 유지합니다."
            : "선택한 파티션만 복구합니다. 현재 런 복구는 현재 영구 진행과 시점이 맞는 백업을 선택하세요.", MessageType.Info);
        for (int i = 1; i <= 3; i++)
        {
            var backup = _snapshots[partition][i];
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("자동 백업 " + i + " · " + backup.Status, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(backup.RelativePath, EditorStyles.wordWrappedMiniLabel);
                if (backup.Exists) EditorGUILayout.LabelField(backup.Modified.ToString("yyyy-MM-dd HH:mm:ss") + " · " + backup.Size.ToString("N0") + " bytes");
                if (backup.Error != null) EditorGUILayout.HelpBox(backup.Error, MessageType.Warning);
                using (new EditorGUI.DisabledScope(!DiskActionsAllowed || !backup.Valid))
                    if (GUILayout.Button("이 백업으로 복구...")) ConfirmRestore(backup);
                Observe("backup:" + i, GUILayoutUtility.GetLastRect());
            }
        }
        using (new EditorGUI.DisabledScope(!DiskActionsAllowed))
            if (GUILayout.Button("수동 원본 백업 파일에서 복구..."))
            {
                string folder = SaveMakerStorage.Resolve(Root, "SaveMakerBackups");
                string file = EditorUtility.OpenFilePanel("복구할 " + SaveMakerStorage.Label(partition) + " ES3 파일", Directory.Exists(folder) ? folder : Root, "es3");
                if (!string.IsNullOrEmpty(file)) TryAction(() =>
                {
                    string rel = Path.GetRelativePath(Root, file).Replace('\\', '/');
                    if (!rel.StartsWith("SaveMakerBackups/", StringComparison.OrdinalIgnoreCase)) throw new IOException("SaveMakerBackups 폴더 안의 원본 백업만 선택하세요.");
                    var source = SaveMakerStorage.Read(Root, partition, rel);
                    if (!source.Valid) throw new IOException(source.Error ?? "백업을 읽을 수 없습니다.");
                    ConfirmRestore(source);
                });
            }
        EditorGUILayout.Space(16);
        EditorGUILayout.LabelField("디버그 초기화", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("실제 로컬 진행 데이터를 초기 상태로 덮어씁니다. 원본 아카이브를 먼저 만들며, 프로젝트 에셋과 PlayerPrefs 전체는 삭제하지 않습니다. 확인란에 RESET을 입력해야 활성화됩니다.", MessageType.Warning);
        _confirmation = EditorGUILayout.TextField("확인 입력", _confirmation);
        using (new EditorGUI.DisabledScope(!DiskActionsAllowed || _confirmation != "RESET"))
        {
            if (GUILayout.Button("런만 초기화...")) ConfirmReset(0);
            if (GUILayout.Button("새 게임: 영구 진행 + 런 초기화...")) ConfirmReset(1);
            if (GUILayout.Button("기기 설정 + 키 바인딩 초기화...")) ConfirmReset(2);
        }
    }
    private void ConfirmRestore(SaveMakerStorage.Snapshot source)
    {
        string effect = source.Partition == SaveMakerPartition.Profile ? "영구 진행을 복구하고 현재 런을 초기화합니다." : SaveMakerStorage.Label(source.Partition) + " 파일을 복구합니다.";
        if (!EditorUtility.DisplayDialog("백업 복구", "원본: " + source.AbsolutePath + "\n대상: " + _names[(int)source.Partition] + "\n\n" + effect + "\n현재 파일은 별도 아카이브에 백업합니다. 적용하지 않은 기기 설정 초안은 버립니다.", "백업 후 복구", "취소")) return;
        var changes = new Dictionary<SaveMakerPartition, string> { [source.Partition] = source.Json };
        if (source.Partition == SaveMakerPartition.Profile) changes[SaveMakerPartition.Run] = JsonUtility.ToJson(new RunSaveData());
        ExecuteWrite(changes, "Restore " + source.RelativePath, source);
    }
    private void ConfirmReset(int mode)
    {
        string effect = mode == 0 ? "현재 런의 인벤토리·장비·맵·상점 재고를 초기화합니다. 영구 진행과 기기 설정은 유지합니다." : mode == 1
            ? "영구 진행(퀘스트·상점 해금·강화·계승 창고)과 현재 런을 초기화합니다. 기기 설정은 유지합니다."
            : "기기 설정과 사용자 키 바인딩만 초기화합니다. 영구 진행과 현재 런은 유지합니다.";
        string targets = mode == 1 ? _names[1] + "\n" + _names[2] : _names[mode == 0 ? 2 : 0];
        if (!EditorUtility.DisplayDialog("실제 세이브 초기화", effect + "\n\n대상:\n" + targets + "\n\n기존 파일의 원본 백업을 먼저 만듭니다.", "백업 후 초기화", "취소")) return;
        var changes = new Dictionary<SaveMakerPartition, string>();
        if (mode == 0 || mode == 1) changes[SaveMakerPartition.Run] = JsonUtility.ToJson(new RunSaveData());
        if (mode == 1)
        {
            var ids = AssetDatabase.FindAssets("t:ShopConfigSO").Select(g => AssetDatabase.LoadAssetAtPath<ShopConfigSO>(AssetDatabase.GUIDToAssetPath(g))).Where(s => s != null).Select(s => s.ConfigId);
            changes[SaveMakerPartition.Profile] = SaveMakerStorage.FreshProfile(ids);
        }
        if (mode == 2) changes[SaveMakerPartition.Settings] = JsonUtility.ToJson(new DeviceSettingsSaveData());
        ExecuteWrite(changes, "Explicit reset " + mode); _confirmation = "";
    }
    private void ExecuteWrite(Dictionary<SaveMakerPartition, string> changes, string reason, SaveMakerStorage.Snapshot source = null)
    {
        TryAction(() =>
        {
            if (!DiskActionsAllowed) throw new InvalidOperationException("디스크 변경이 잠겨 있습니다. Play Mode/메모리 캐시/파일 설정을 확인하세요.");
            var expected = _snapshots.Values.Select(s => s[0]).ToList();
            if (source != null) expected.Add(source);
            _lastArchive = SaveMakerStorage.Write(Root, _names, expected, changes, reason);
            _message = "적용 완료: " + string.Join(", ", changes.Keys.Select(SaveMakerStorage.Label)) + " · 다음 게임 로드부터 적용됩니다. 원본 백업: " + _lastArchive;
            RefreshSnapshots();
        });
    }
    private void TryAction(Action action)
    {
        try { action(); }
        catch (Exception ex) { _message = "작업을 완료하지 못했습니다: " + ex.Message; }
    }
}
