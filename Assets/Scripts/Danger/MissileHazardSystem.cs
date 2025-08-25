using UnityEngine;

public class MissileHazardSystem : MonoBehaviour
{
    [Header("Refs")]
    public SurvivalDirector director;
    public BoardGrid board;

    [Header("Missile Prefab")]
    public HomingMissile missilePrefab;

    [Header("Timing")]
    [Min(1)] public int triggerEveryN = 3;  // N의 배수 리셋마다 스폰
    [Min(1)] public int firstSpawnAt = 3;   // 첫 스폰이 일어날 '정확한 리셋 번호' (요구: 3)

    public float spawnYOffset = 0f;

    HomingMissile active;

    void Awake()
    {
        if (!director) director = FindAnyObjectByType<SurvivalDirector>();
        if (!board)    board    = FindAnyObjectByType<BoardGrid>();
    }

    void OnEnable()
    {
        if (director != null)
        {
            // 번호를 함께 받는다 → 레이스/초기화 상관없음
            director.OnZonesResetSeq += HandleResetSeq;
        }
    }
    void OnDisable()
    {
        if (director != null)
        {
            director.OnZonesResetSeq -= HandleResetSeq;
        }
    }

    void HandleResetSeq(int seq)
    {
        // 직전 사이클 미사일은 '이번 리셋'에 폭발
        if (active)
        {
            active.Explode();
            active = null;
        }

        // 첫 스폰은 정확히 firstSpawnAt 에서만
        if (seq < firstSpawnAt) return;

        // 이후에는 triggerEveryN의 배수에서만 스폰
        if ((seq - firstSpawnAt) % Mathf.Max(1, triggerEveryN) == 0)
        {
            SpawnMissile();
        }
        Debug.Log($"ResetSeq={seq}, firstSpawnAt={firstSpawnAt}, everyN={triggerEveryN}");
        Debug.Log($"Spawned at seq={seq}");

    }

    void SpawnMissile()
{
    if (!missilePrefab || !board || !director) return;

    Vector3 center = board.origin + new Vector3(board.width * board.tileSize * 0.5f, 0f, board.height * board.tileSize * 0.5f);
    center.y += spawnYOffset;

    active = Instantiate(missilePrefab, center, Quaternion.identity, transform);
    active.enabled = true; // 예방 차원
    // ✅ 수명 + 타겟 + 속도까지 세팅하면서 강제 Enable
    active.Setup(director, director.SetDuration);
}
}
