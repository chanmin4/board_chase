using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
#if UNITY_EDITOR || DEVELOPMENT_BUILD
[DefaultExecutionOrder(99999)] // (선택) 최대한 늦게 갱신
#endif
public class SaveDataDebug : MonoBehaviour
{
    // ★ 싱글턴 가드
    static SaveDataDebug _instance;

    [Header("Read-only mirror of ProgressManager.Data")]
    [SerializeField] int version;
    [SerializeField] int bestScore;
    [SerializeField] int challengeScore = 0;
    [SerializeField] int challengeTimeMs = 0;
    [SerializeField] string equippedSkinId;

    [SerializeField] List<string> unlockedSkins = new();
    [SerializeField] List<string> unlockedAbilities = new();
    [SerializeField] List<string> claimedAchievements = new();

    void Awake()
    {
        // 이미 살아 있는 인스턴스가 있으면 자신을 파괴 (중복 방지)
        if (_instance && _instance != this) { Destroy(gameObject); return; }
        _instance = this;

        // 씬 전환에도 유지
        DontDestroyOnLoad(gameObject);
    }

    void LateUpdate()
    {
        if (!Application.isPlaying) return;

        var pm = ProgressManager.Instance;
        var d  = (pm != null) ? pm.Data : null;
        if (d == null) return;

        version          = d.version;
        bestScore        = d.bestScore;
        challengeScore   = d.challengeScore;
        challengeTimeMs  = d.challengeTimeMs;
        equippedSkinId   = d.equippedSkinId;

        Mirror(d.unlockedSkins,       unlockedSkins);
        Mirror(d.unlockedAbilities,   unlockedAbilities);
        Mirror(d.claimedAchievements, claimedAchievements);
    }

    static void Mirror<T>(List<T> src, List<T> dst)
    {
        if (dst == null) return;
        dst.Clear();
        if (src != null) dst.AddRange(src);
    }
}
