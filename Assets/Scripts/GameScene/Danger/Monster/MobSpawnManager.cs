using System.Collections;
using UnityEngine;

public enum MobType { Ghost, Sniper,InkEater ,Gunner, Boss, }

[DisallowMultipleComponent]
public class MobSpawnManager : MonoBehaviour
{
    public static MobSpawnManager Instance { get; private set; }

    [Header("Targets")]
    public DiskInkLeveler targetLeveler;      // 비워두면 자동 탐색

    [Header("Global Toggle")]
    public bool manageGhost  = true;
    public bool manageSniper = true;
    public bool manageInkEater = true;
    public bool manageGunner = true;
    public bool manageBoss = true;

    [Header("Ghost Spawn")]
    public PollutionGhostSpawner ghostSpawner; // 인스펙터 연결
    public float ghostFirstDelay = 1.0f;
    public float ghostInterval   = 5.0f;
    public int ghostMaxAlive = 4;
    public int   ghostBatch      = 2;  
    public float ghostKillXP = 20f;

    [Header("Sniper Spawn")]
    public PollutionSniperSpawner sniperSpawner;
    public float sniperFirstDelay = 4.0f;
    public float sniperInterval   = 8.0f;
    public int   sniperMaxAlive   = 1;
    public int   sniperBatch      = 1;
    public float sniperKillXP = 35f;
    [Header("InkEater Spawn")]
    public PollutionInkEaterSpawner inkEaterSpawner;  // ★ ADD
    public float inkEaterFirstDelay = 6.0f;          // ★
    public float inkEaterInterval   = 7.0f;          // ★
    public int   inkEaterMaxAlive   = 2;             // ★
    public int   inkEaterBatch      = 1;             // ★
    public float inkEaterKillXP     = 25f;           // ★ (킬 가능 시)
    [Header("Gunner Spawn")]
    public PollutionGunnerSpawner gunnerSpawner;
    public float gunnerFirstDelay = 6.0f;
    public float gunnerInterval = 7.0f;          // ★
    public int gunnerMaxAlive = 2;  
    public int gunnerBatch = 1;
    public float gunnerKillXP = 25f;

    [Header("Boss Spawn")]
    public PollutionBossSpawner bossSpawner;
    public float bossFirstDelay = 30f;
    public float bossInterval   = 9999f; // 한 번만이면 크게
    public int   bossMaxAlive   = 1;
    public int   bossBatch      = 1;
    public float bossKillXP = 150f;
    


    // 내부 카운트
    int aliveGhost, aliveSniper, aliveInkEater,aliveGunner,aliveBoss;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (!targetLeveler) targetLeveler = FindAnyObjectByType<DiskInkLeveler>();
        if (!ghostSpawner)  ghostSpawner  = FindAnyObjectByType<PollutionGhostSpawner>();
        if (!sniperSpawner) sniperSpawner = FindAnyObjectByType<PollutionSniperSpawner>();
        if (!inkEaterSpawner) inkEaterSpawner = FindAnyObjectByType<PollutionInkEaterSpawner>(); // ★
        if (!gunnerSpawner) gunnerSpawner = FindAnyObjectByType<PollutionGunnerSpawner>();
        if (!bossSpawner) bossSpawner = FindAnyObjectByType<PollutionBossSpawner>();
    }

    void OnEnable()
    {
        if (manageGhost && ghostSpawner) StartCoroutine(RunSpawner(MobType.Ghost));
        if (manageSniper && sniperSpawner) StartCoroutine(RunSpawner(MobType.Sniper));
        if (manageInkEater&&inkEaterSpawner) StartCoroutine(RunSpawner(MobType.InkEater));
        if (manageGunner && gunnerSpawner) StartCoroutine(RunSpawner(MobType.Gunner));
        if (manageBoss && bossSpawner) StartCoroutine(RunSpawner(MobType.Boss));

    }

      IEnumerator RunSpawner(MobType type)
    {
        float first = FirstDelay(type);
        float every = Interval(type);

        if (first > 0) yield return new WaitForSeconds(first);

        var wait = new WaitForSeconds(every > 0 ? every : 0.1f);

        while (true)
        {
            int cap     = GetCap(type);
            int alive   = GetAlive(type);
            int batch   = GetBatch(type);
            int canMake = Mathf.Max(0, cap - alive);
            int toMake  = Mathf.Min(batch, canMake);

            for (int i = 0; i < toMake; i++)
            {
                if (!TrySpawn(type)) break; // 스폰 실패 시 루프 중단(위치 부족 등)
                IncAlive(type, +1);
            }

            // interval 변경 가능성 대비 매 루프 갱신
            every = Interval(type);
            wait  = new WaitForSeconds(every > 0 ? every : 0.1f);
            yield return wait;
        }
    }


    bool TrySpawn(MobType type)
    {
        switch (type)
        {
            case MobType.Ghost:  return ghostSpawner  && ghostSpawner.SpawnOne();
            case MobType.Sniper: return sniperSpawner && sniperSpawner.SpawnOne();
            case MobType.InkEater:return inkEaterSpawner && inkEaterSpawner.SpawnOne();
            case MobType.Gunner:return gunnerSpawner && gunnerSpawner.SpawnOne();
            case MobType.Boss:   return bossSpawner   && bossSpawner.SpawnOne();
        }
        return false;
    }

    int GetAlive(MobType t) =>
    t == MobType.Ghost ? aliveGhost :
    t == MobType.Sniper ? aliveSniper :
    t == MobType.InkEater ? aliveInkEater :
    t == MobType.Gunner ? aliveGunner:
    aliveBoss;
    int  GetCap  (MobType t) =>
    t ==MobType.Ghost?ghostMaxAlive:
    t ==MobType.Sniper?sniperMaxAlive:
    t == MobType.InkEater ?inkEaterMaxAlive :
    t == MobType.Gunner ? gunnerMaxAlive:
    bossMaxAlive;
    int  GetBatch(MobType t) =>
    t ==MobType.Ghost?ghostBatch :
    t ==MobType.Sniper?sniperBatch :
    t ==MobType.InkEater?inkEaterBatch :
    t == MobType.Gunner ? gunnerBatch:
    bossBatch;
    float FirstDelay(MobType t)=>
    t ==MobType.Ghost?ghostFirstDelay:
    t ==MobType.Sniper?sniperFirstDelay:
    t == MobType.InkEater ? inkEaterFirstDelay :
    t == MobType.Gunner ? gunnerFirstDelay:
    bossFirstDelay;
    float Interval  (MobType t)=>
    t ==MobType.Ghost?ghostInterval  :
    t ==MobType.Sniper?sniperInterval  :
    t == MobType.InkEater ?inkEaterInterval :
    t == MobType.Gunner ? gunnerFirstDelay:
    bossInterval;

    void IncAlive(MobType t, int d)
    {
        if (t==MobType.Ghost) aliveGhost  = Mathf.Max(0, aliveGhost+d);
        else if (t==MobType.Sniper) aliveSniper = Mathf.Max(0, aliveSniper+d);
        else if (t==MobType.InkEater)   aliveInkEater   = Mathf.Max(0, aliveInkEater+d);
        else if (t==MobType.Gunner)   aliveGunner   = Mathf.Max(0, aliveGunner+d);
        else aliveBoss = Mathf.Max(0, aliveBoss + d);
    }

    // ====== 몹 사망 보고 (몹 스크립트에서 호출) ======
    public void ReportMobKilled(MobType type)
    {
        IncAlive(type, -1);
        float xp = (type==MobType.Ghost)?ghostKillXP:
        (type == MobType.Sniper) ?sniperKillXP:
        (type == MobType.InkEater) ?inkEaterKillXP:
        (type==MobType.Gunner)?gunnerKillXP:
        bossKillXP;
        AwardXP(xp, "kill-"+type.ToString());
    }

    // 공통 XP 지급
    public void AwardXP(float amount, string reason="misc")
    {
        if (amount <= 0f) return;
        if (!targetLeveler)
        {
            targetLeveler = FindAnyObjectByType<DiskInkLeveler>();
            if (!targetLeveler) return;
        }
        targetLeveler.GrantXP(amount, reason);
    }
}
