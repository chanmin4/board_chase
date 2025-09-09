using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resources/Rewards/* 에 있는 모든 RewardSO를 한 번 로드해서 id→SO 캐시.
/// UI에서 payloadId로 아이콘/타이틀을 뽑거나, 필요 시 SO.Grant(pm) 호출에 사용.
/// </summary>
public static class RewardDB
{
    static bool _loaded;
    static readonly Dictionary<string, RewardSO> _map = new();

    public static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;

        // 폴더 구조 예: Resources/Rewards/Skins, Resources/Rewards/Cards
        var all = Resources.LoadAll<RewardSO>("Achievement");
        foreach (var r in all)
        {
            if (r && !_map.ContainsKey(r.id))
                _map.Add(r.id, r);
            // else: 중복 id면 로그 경고 넣어도 좋음
        }
    }

    public static RewardSO Get(string id)
    {
        EnsureLoaded();
        _map.TryGetValue(id, out var so);
        return so;
    }

    public static Sprite GetIcon(string id) => Get(id)?.icon;
    public static string GetTitle(string id) => Get(id)?.title ?? id;

    /// <summary>
    /// 수령 후 후처리 훅을 쓰고 싶을 때 호출.
    /// (TryClaim에서 저장/목록추가는 이미 끝났다고 가정)
    /// </summary>
    public static void GrantVisualOrRuntime(string id, ProgressManager pm)
    {
        var so = Get(id);
        if (so != null) so.Grant(pm);
    }
}
