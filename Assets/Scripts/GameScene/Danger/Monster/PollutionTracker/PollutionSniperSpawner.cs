using UnityEngine;

[DisallowMultipleComponent]
public class PollutionSniperSpawner : MonoBehaviour
{
    [Header("Scene Refs")]
    public BoardGrid board;
    public SurvivalDirector director;
    public Transform player;

    [Header("Anchors (one will be picked at random)")]
    public Transform[] anchors;

    [Header("Sniper Prefab")]
    public PollutionSniper sniperPrefab; // 프리팹 폴더에 보관, 씬 참조는 비움

    [Header("Spawn Timing")]
    public bool spawnAtStart = true;
    [Min(0)] public float spawnDelay = 0f;

    void Reset()
    {
        board = FindAnyObjectByType<BoardGrid>();
        director = FindAnyObjectByType<SurvivalDirector>();
        if (!player && director) player = director.player;
    }

    void Start()
    {
        if (!board) board = FindAnyObjectByType<BoardGrid>();
        if (!director) director = FindAnyObjectByType<SurvivalDirector>();
        if (!player && director) player = director.player;

        if (spawnAtStart) Invoke(nameof(SpawnNow), Mathf.Max(0f, spawnDelay));
    }

    [ContextMenu("Spawn Now")]
    public void SpawnNow()
    {
        if (!sniperPrefab) { Debug.LogError("[SniperSpawner] sniperPrefab 없음"); return; }
        if (anchors == null || anchors.Length == 0) { Debug.LogError("[SniperSpawner] anchors 비어있음"); return; }

        int idx = Random.Range(0, anchors.Length);
        var t = anchors[idx];

        var pos = t.position;
        if (board) pos.y = board.origin.y;   // ← 보드 기준

        var inst = Instantiate(sniperPrefab, pos, t.rotation);
        inst.Setup(board, player, director);
    }

}
