using UnityEngine;

public class MissileHazardSystem : MonoBehaviour
{
    [Header("Refs")]
    public SurvivalDirector director;
    public BoardGrid board;

    [Header("Missile Prefab")]
    public HomingMissile missilePrefab;

    [Header("Timing")]
    [Min(1)] public int triggerEveryN = 3; // 이후 간격 (3,6,9,…)
    [Min(1)] public int firstSpawnAt = 3;  // 첫 스폰 리셋 번호 (정확히 3)

    public float spawnYOffset = 0f;
    public float homingSpeed = 7f;

    HomingMissile active;

    void Awake()
    {
        if (!director) director = FindAnyObjectByType<SurvivalDirector>();
        if (!board)    board    = FindAnyObjectByType<BoardGrid>();
    }

    void OnEnable()
    {
        if (director) director.OnZonesResetSeq += HandleResetSeq;
    }
    void OnDisable()
    {
        if (director) director.OnZonesResetSeq -= HandleResetSeq;
    }

    void HandleResetSeq(int seq)
    {
        // 직전 사이클 미사일 폭발(오염 생성)
        if (active)
        {
            active.Explode();
            active = null;
        }

        if (seq < firstSpawnAt) return;
        if ((seq - firstSpawnAt) % Mathf.Max(1, triggerEveryN) != 0) return;

        SpawnMissile();
    }

    void SpawnMissile()
    {
        if (!missilePrefab || !board || !director) return;

        Vector3 center = board.origin + new Vector3(
            board.width  * board.tileSize * 0.5f,
            0f,
            board.height * board.tileSize * 0.5f
        );
        center.y += spawnYOffset;

        active = Instantiate(missilePrefab, center, Quaternion.identity, transform);

        // 수명(=다음 리셋까지) + 타겟/속도까지 강제 지정
        var target = director ? director.player : null;
        active.Setup(director, director.SetDuration, target, homingSpeed, true);
    }
}
