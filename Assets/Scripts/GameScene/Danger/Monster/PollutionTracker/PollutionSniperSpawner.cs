using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PollutionSniperSpawner : MonoBehaviour
{
    [Header("Sniper Settings (Spawner controls)")]
    public PollutionSniper.SniperSettings settings = new PollutionSniper.SniperSettings
    {
        maxHP = 3,
        damagePerHit = 1,
        killByLayers = ~0,
        bodyRadius = 0.5f,
        groundY = 0.2f,
        aimTargetFollowSpeed = 6f,
        aimAngularSpeedDeg = 180f,
        aimPreviewTime = 3f,
        fireShowDuration = 0.25f,
        cooldown = 3f,
        beamLength = 100f,
        survivalDamageOnHit = 4f,
        contamStepMeters = 0.6f,
        contamRadiusMeters = 0.7f,
        previewLineWidth = 0.08f,
        fireLineWidth = 0.10f,
        previewColor = new Color(0.2f, 1f, 0.6f, 0.95f),
        fireColor = new Color(1f, 0.2f, 0.2f, 1f)
    };

    [Header("Scene Refs")]
    public BoardGrid board;
    public SurvivalDirector director;
    public Transform player;

    [Header("Anchors (one will be picked at random)")]
    public Transform[] anchors;

    [Header("Sniper Prefab")]
    public PollutionSniper sniperPrefab; // 프리팹 폴더에 보관, 씬 참조는 비움

    [Header("Spawn Timing")]
    [NonSerialized]public bool spawnAtStart = true;
    [NonSerialized][Min(0)] public float spawnDelay = 0f;

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
    }

    [ContextMenu("Spawn Now")]
    public bool SpawnOne()
    {
        if (!sniperPrefab) { Debug.LogError("[SniperSpawner] sniperPrefab 없음"); return false; }
        if (anchors == null || anchors.Length == 0) { Debug.LogError("[SniperSpawner] anchors 비어있음"); return false; }

        int idx = UnityEngine.Random.Range(0, anchors.Length);
        var t = anchors[idx];

        var pos = t.position;
        if (board) pos.y = board.origin.y;   // ← 보드 기준

        var inst = Instantiate(sniperPrefab, pos, t.rotation,transform);
        inst.ApplySettings(settings);
        inst.Setup(board, player, director);
        return inst != null;
    }

}
