using System;
using System.Linq;
using UnityEngine;

public partial class StageProgressionManager
{
    private bool _continuingThroughHatch;
    public string LastHatchTransitionMessage { get; private set; }

    public bool CanAdvanceInRun(out string reason)
        => CanAdvanceInRun(null, out reason, out _);

    public int[] GetUnlockedHatchDestinations() => _flow == null ? Array.Empty<int>() :
        _flow.GetHatchDestinations(_currentStage)
            .Where(stage => IsStageUnlocked(stage.StageIndex) && stage.MapPolicy != StageMapPolicy.Legacy && !stage.UseStartSectorOnly)
            .Select(stage => stage.StageIndex).ToArray();

    private bool CanAdvanceInRun(int? destination, out string reason, out StageDefinitionSO next)
    {
        next = null;
        reason = string.Empty;
        if (!Application.isPlaying) reason = "원정 이동은 Play Mode에서만 가능합니다.";
        else if (_stageTransitionPending || _continuingThroughHatch) reason = "이미 스테이지 이동 중입니다.";
        else if (!_hasRule || _currentStage == null || _flow == null) reason = "현재 스테이지 진행 정보가 없습니다.";
        else if (!_flow.GetHatchDestinations(_currentStage).Any())
            reason = "입장 가능한 다음 스테이지가 없습니다. 마지막 스테이지 또는 미등록 상태입니다.";
        else if (RaidRuntime.Active?.State?.active != true) reason = "활성 원정에서만 이어서 이동할 수 있습니다.";
        else
        {
            var allowed = GetUnlockedHatchDestinations();
            if (allowed.Length == 0) reason = "다음 스테이지 접근 해금 퀘스트를 먼저 완료하세요.";
            else if (destination.HasValue && !allowed.Contains(destination.Value)) reason = "이 해치에서 갈 수 없는 스테이지입니다.";
            else _flow.TryGetStage(destination ?? allowed[0], out next);
        }
        return string.IsNullOrEmpty(reason);
    }

    public bool TryAdvanceInRun(out string reason, Action<bool> onCompleted = null)
        => TryAdvanceInRun(null, out reason, onCompleted);

    public bool TryAdvanceInRun(int? destination, out string reason, Action<bool> onCompleted = null)
    {
        if (!CanAdvanceInRun(destination, out reason, out var next)) { LastHatchTransitionMessage = reason; return false; }
        var loader = SceneLoader.Instance;
        var saves = _saveSystem != null ? _saveSystem : SaveSystem.Active;
        if (loader == null || saves == null) { reason = LastHatchTransitionMessage = "로딩/저장 매니저가 없습니다."; return false; }
        var raid = RaidRuntime.Active;
        // Commit the old map and carried items before any generated geometry is replaced.
        if (!saves.TrySaveRunDataToDisk()) { reason = LastHatchTransitionMessage = "현재 원정 저장에 실패해 이동을 취소했습니다."; return false; }
        raid.Capture(saves.RunData);
        var previous = JsonUtility.FromJson<RaidRunSaveData>(JsonUtility.ToJson(saves.RunData.raid));
        int previousStage = _currentStageIndex;
        bool advanced = true;
        _stageTransitionPending = true;
        _continuingThroughHatch = true;
        bool accepted = loader.RequestStageTransition(next.DisplayName, () =>
        {
            // The player must survive even if an authored SpawnSystem parents it to a generated room.
            var inventory = FindFirstObjectByType<PlayerInventoryRuntime>();
            var root = inventory != null ? EntityComponentResolver.ResolveEntityRoot(inventory) : null;
            if (root != null) root.SetParent(null, true);
            BasicBullet.ClearForStageTransition();
            bool built;
            try { built = BeginStage(next.StageIndex); }
            catch (Exception exception) { Debug.LogException(exception, this); built = false; }
            if (!built)
            {
                advanced = false;
                // A failed destination build must not replace the saved previous raid with an empty map.
                saves.RunData.raid = previous;
                bool restored;
                try { restored = BeginStage(previousStage, previous); }
                catch (Exception exception) { Debug.LogException(exception, this); restored = false; }
                _continuingThroughHatch = false;
                _stageTransitionPending = restored;
                LastHatchTransitionMessage = restored ? "다음 맵 생성 실패: 이전 원정 복구." : "다음 맵 생성/복구 실패. 이전 원정 세이브는 보존됨.";
                Debug.LogError("[Hatch] " + LastHatchTransitionMessage, this);
                if (!restored) onCompleted?.Invoke(false);
                // Successfully restored geometry can pass through the normal readiness/loading gate.
                return restored;
            }
            var current = RaidRuntime.Active.State;
            current.medicalStateInitialized = previous.medicalStateInitialized;
            current.suppressionRemainingSeconds = previous.suppressionRemainingSeconds;
            current.ampouleRemainingSeconds = previous.ampouleRemainingSeconds;
            current.ampouleRecoveryPerSecond = previous.ampouleRecoveryPerSecond;
            current.ampouleExposureMultiplier = previous.ampouleExposureMultiplier;
            _stageTransitionPending = true;
            _continuingThroughHatch = false;
            LastHatchTransitionMessage = "다음 스테이지 준비 중 · 장비/패시브/감염/약효 유지";
            return true;
        }, () =>
        {
            _stageTransitionPending = false;
            bool saved = saves.TrySaveRunDataToDisk();
            LastHatchTransitionMessage = !advanced ? "다음 맵 생성 실패로 이전 원정에 복귀했습니다." :
                saved ? "원정을 유지한 채 다음 스테이지로 이동했습니다." : "이동 완료. 체크포인트 저장 실패: 콘솔을 확인하세요.";
            PublishSnapshot();
            onCompleted?.Invoke(advanced);
        });
        if (!accepted) { _stageTransitionPending = false; _continuingThroughHatch = false; reason = LastHatchTransitionMessage = "로딩 요청이 거절되었습니다."; }
        return accepted;
    }
}
