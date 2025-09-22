using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Resources/Achievement/* 의 RewardSO들을 로드하여 id→SO 캐시 + 정렬 리스트 제공.
/// </summary>
public static class RewardDB
{
    static bool _loaded;
    static readonly Dictionary<string, RewardSO> _map = new();
    static readonly List<RewardSO> _all = new();

    public static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;

        _map.Clear();
        _all.Clear();

        // 폴더: Resources/Achievement
        var all = Resources.LoadAll<RewardSO>("Achievement");
         Debug.Log($"[RewardDB] Loaded {all.Length} RewardSO from ALL Resources");
        foreach (var r in all)
        {
            if (!r || string.IsNullOrEmpty(r.id)) continue;
            _map[r.id] = r;
            if (!r.hideFromListing)
                _all.Add(r);
        }

        // 요구 포인트 오름차순 정렬(동일값이면 id로 안정 정렬)
        _all.Sort((a, b) =>
        {
            int c = a.requiredBestScore.CompareTo(b.requiredBestScore);
            return c != 0 ? c : string.Compare(a.id, b.id, System.StringComparison.Ordinal);
        });
    }

    /// <summary>정렬된 보상 목록(요구 포인트 오름차순)</summary>
    public static IReadOnlyList<RewardSO> All
    {
        get { EnsureLoaded(); return _all; }
    }

    public static RewardSO Get(string id)
    {
        EnsureLoaded();
        _map.TryGetValue(id, out var so);
        return so;
    }

    public static Sprite GetIcon(string id) => Get(id)?.icon;
    public static string GetTitle(string id) => Get(id)?.title ?? id;

    public static void GrantVisualOrRuntime(string id, ProgressManager pm)
    {
        var so = Get(id);
        if (so != null) so.Grant(pm);
    }

    // 인스펙터 수동 등록 덮어쓰기(옵션)
    public static void SyncFrom(IEnumerable<SkinRewardSO> list)
    {
        if (list == null) return;
        foreach (var so in list)
        {
            if (so == null || string.IsNullOrEmpty(so.id)) continue;
            _map[so.id] = so;
            if (!so.hideFromListing && !_all.Contains(so)) _all.Add(so);
        }
        _all.Sort((a, b) =>
        {
            int c = a.requiredBestScore.CompareTo(b.requiredBestScore);
            return c != 0 ? c : string.Compare(a.id, b.id, System.StringComparison.Ordinal);
        });
    }
}
