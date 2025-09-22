using UnityEngine;
using TMPro;
using System.Text;
using System.Linq;
using System; 
using System.Collections.Generic;
public class ZoneHitHUD : MonoBehaviour
{
     [Header("Refs")]
    public SurvivalDirector director;

    [Header("Texts (assign in Inspector)")]
    public TMP_Text smallText;
    public TMP_Text mediumText;
    public TMP_Text largeText;

    [Header("Labels")]
    public string smallLabel = "small";
    public string mediumLabel = "medium";
    public string largeLabel = "large";

    // 내부 상태: 활성 존 개수/요구치(사이즈별 최소값)
    readonly int[] _count = new int[3];
    readonly int[] _minReq = new int[3] { int.MaxValue, int.MaxValue, int.MaxValue };

    // 존ID → (sizeIdx, req) 매핑 (Expired/Consumed에서 정확히 감소/재계산)
    readonly Dictionary<int, (int sizeIdx, int req)> _zoneInfo = new();

    // 캐시 델리게이트(람다 -= 이슈 회피)
    Action _onZonesResetCached;
    Action<ZoneSnapshot> _onZoneSpawnedCached;
    Action<int> _onZoneExpiredCached;
    Action<int> _onZoneConsumedCached;

    void OnEnable()
    {
        if (!director) director = FindAnyObjectByType<SurvivalDirector>();

        _onZonesResetCached   = HandleZonesReset;
        _onZoneSpawnedCached  = HandleZoneSpawned;
        _onZoneExpiredCached  = HandleZoneRemoved;
        _onZoneConsumedCached = HandleZoneRemoved;

        if (director)
        {
            director.OnZonesReset   += _onZonesResetCached;
            director.OnZoneSpawned  += _onZoneSpawnedCached;   // profileIndex로 사이즈/요구치 산출
            director.OnZoneExpired  += _onZoneExpiredCached;
            director.OnZoneConsumed += _onZoneConsumedCached;
        }

        UpdateTexts();
    }

    void OnDisable()
    {
        if (!director) return;
        if (_onZonesResetCached   != null) director.OnZonesReset   -= _onZonesResetCached;
        if (_onZoneSpawnedCached  != null) director.OnZoneSpawned  -= _onZoneSpawnedCached;
        if (_onZoneExpiredCached  != null) director.OnZoneExpired  -= _onZoneExpiredCached;
        if (_onZoneConsumedCached != null) director.OnZoneConsumed -= _onZoneConsumedCached;
    }

    void HandleZonesReset()
    {
        Array.Clear(_count, 0, _count.Length);
        _minReq[0] = _minReq[1] = _minReq[2] = int.MaxValue;
        _zoneInfo.Clear();
        UpdateTexts();
    }

    void HandleZoneSpawned(ZoneSnapshot s)
    {
        // snapshot.profileIndex → 해당 프로필로 사이즈/요구치 조회
        if (!director || s.profileIndex < 0 || s.profileIndex >= director.zoneProfiles.Count) return;
        var p = director.zoneProfiles[s.profileIndex];
        if (p == null) return;

        int sizeIdx = (int)p.size; // Small=0, Medium=1, Large=2  :contentReference[oaicite:4]{index=4}
        int req = Mathf.Clamp(director.GetEffectiveRequiredHits(p), 0, 999); // :contentReference[oaicite:5]{index=5}

        _count[sizeIdx]++;
        if (req < _minReq[sizeIdx]) _minReq[sizeIdx] = req;

        _zoneInfo[s.id] = (sizeIdx, req);
        UpdateTexts();
    }

    void HandleZoneRemoved(int zoneId)
    {
        if (_zoneInfo.TryGetValue(zoneId, out var info))
        {
            _zoneInfo.Remove(zoneId);
            _count[info.sizeIdx] = Mathf.Max(0, _count[info.sizeIdx] - 1);

            // 해당 사이즈의 최소 요구치 재계산
            int newMin = int.MaxValue;
            foreach (var kv in _zoneInfo.Values)
                if (kv.sizeIdx == info.sizeIdx && kv.req < newMin) newMin = kv.req;
            _minReq[info.sizeIdx] = (_count[info.sizeIdx] > 0) ? newMin : int.MaxValue;

            UpdateTexts();
        }
    }

    void UpdateTexts()
    {
        SetOne(smallText,  0, smallLabel);
        SetOne(mediumText, 1, mediumLabel);
        SetOne(largeText,  2, largeLabel);
    }

    void SetOne(TMP_Text t, int sizeIdx, string label)
    {
        if (!t) return;

        int cnt = _count[sizeIdx];
        if (cnt <= 0)
        {
            t.text = $"{label} zone X";
            return;
        }

        int req = (_minReq[sizeIdx] == int.MaxValue) ? 0 : _minReq[sizeIdx];
        t.text = $"{label} zone count {cnt}\n  require hit {req}";
    }
}
