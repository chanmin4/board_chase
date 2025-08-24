using UnityEngine;

public class MissileHazardSystem : MonoBehaviour
{
    [Header("Refs")]
    public SurvivalDirector director;
    public BoardGrid board;

    [Header("Missile Prefab")]
    public HomingMissile missilePrefab;

    [Header("Timing")]
    [Min(1)] public int triggerEveryN = 3;  // 3번째 재생성마다 스폰
    public float spawnYOffset = 0.0f;

    int   regenCount      = 0;      // 시작 0
    bool  seenFirstReset  = false;  // 첫 OnZonesReset은 “오프닝”으로 무시
    HomingMissile active;

    void Awake()
    {
        if (!director) director = FindAnyObjectByType<SurvivalDirector>();
        if (!board) board = FindAnyObjectByType<BoardGrid>();
                // 혹시 씬에 미리 놓인 미사일이 있으면 제거(첫 턴 스폰처럼 보이는 현상 방지)
        active = null;
        regenCount = 0;
        seenFirstReset = false;
    }

    void OnEnable()
    {
        if (director) director.OnZonesReset += HandleZonesReset;
    }
    void OnDisable()
    {
        if (director) director.OnZonesReset -= HandleZonesReset;
    }

    void HandleZonesReset()
    {
        // 씬 시작 직후 Director.Start()에서 오는 첫 호출은 무시
        if (!seenFirstReset)
        {
            seenFirstReset = true;
            regenCount = 0;
            return;
        }

        // 직전 사이클의 미사일을 '이번' 리셋 시점에 폭발
        if (active)
        {
            active.Explode();
            active = null;
        }

        regenCount++;

        if (regenCount % Mathf.Max(1, triggerEveryN) == 0)
            SpawnMissile();
    }

    void SpawnMissile()
    {
        if (!missilePrefab || !board || !director) return;

        Vector3 center = board.origin + new Vector3(board.width * board.tileSize * 0.5f, 0f, board.height * board.tileSize * 0.5f);
        center.y += spawnYOffset;

        active = Instantiate(missilePrefab, center, Quaternion.identity, transform);
        active.Setup(director, director.SetDuration); // 다음 리셋까지의 수명/성장 시간
    }
}
