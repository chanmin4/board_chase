// DiskLauncher.cs (기존 파일 수정)
using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody))]
public class DiskLauncher : MonoBehaviour
{
    [Header("Launch / Stop")]
    public float powerScale   = 1.5f;
    //public float minStopSpeed = 0.25f;

    [Header("Grid")]
    [SerializeField] BoardGrid board;
    public Vector2Int CurrentTile { get; private set; } = new Vector2Int(-1,-1);
    public bool HasValidTile { get; private set; } = false;

    // ==== 쿨타임 모드 추가 ====
    [Header("Cooldown Mode")]
    public bool  useCooldown      = true;   // ← 쿨타임 모드 활성화
    public float cooldownSeconds  = 3f;     //
    public float CooldownRemain { get; private set; } = 0f;
    public bool  CanLaunchNow => useCooldown ? (CooldownRemain <= 0f) : (Charges > 0);
    public event Action<float,float> OnCooldownChanged; // (remain, duration)

    [Header("Cooldown Bonus On Wall Bounce")]
    [Tooltip("벽에 튕길 때마다 남은 쿨다운을 줄입니다 (useCooldown이 true일 때만 동작).")]
    public bool cooldownBonusOnBounce = true;

    [Tooltip("한 번 튕길 때 줄일 쿨다운(초).")]
    [Min(0f)] public float cooldownReducePerBounce = 0.5f;

    [Header("Bounce Charges")] //구기능 충전형 튕기기
    public int baseCharges = 2;
    public int maxCharges  = 5; // 0 or less => unlimited cap
    public int Charges { get; private set; }
    
    // 내부 상태
    int _lastWallHitsForBonus = 0;

    // 이벤트
    public event Action<int, int> OnTileChanged;
    public event Action<int,int> OnStoppedOnTile;
    public event Action<int,int> OnChargesChanged;

    Rigidbody rb;
    //bool launched;
    Vector2Int _lastTile = new Vector2Int(-1,-1);

    SurvivalDirector director;

    void Awake(){
        rb = GetComponent<Rigidbody>();
        if (!board){
            board = FindAnyObjectByType<BoardGrid>();
            if (!board) Debug.LogError("[DiskLauncher] BoardGrid reference missing.");
        }
        director = FindAnyObjectByType<SurvivalDirector>();
        if (director != null)
        {
            director.OnZonesReset += HandleSetReset;
            director.OnZoneConsumed += HandleZoneConsumed;
            director.OnWallHitsChanged += HandleWallHitsChanged_Bonus;
        }

        UpdateCurrentTile(forceEvent:true);

        // 시작값
        if (!useCooldown) ResetChargesToBase();
        else NotifyCooldown(); // HUD 초기화
    }

    void OnDestroy()
    {
        if (director != null)
        {
            director.OnZonesReset -= HandleSetReset;
            director.OnZoneConsumed -= HandleZoneConsumed;
            director.OnWallHitsChanged -= HandleWallHitsChanged_Bonus;
        }
    }

    // ==== 쿨타임 틱 ====
    void Update()
    {
        if (!useCooldown) return;
        if (CooldownRemain > 0f)
        {
            CooldownRemain = Mathf.Max(0f, CooldownRemain - Time.deltaTime);
            NotifyCooldown();
        }
    }
    void StartCooldown()
    {
        CooldownRemain = Mathf.Max(0.0001f, cooldownSeconds);
        NotifyCooldown();
    }
    void NotifyCooldown() => OnCooldownChanged?.Invoke(CooldownRemain, cooldownSeconds);

    // === 충전(횟수) 제어 (쿨타임 모드에선 비활성) ===
    void ResetChargesToBase()
    {
        Charges = baseCharges;
        if (maxCharges > 0 && Charges > maxCharges) Charges = maxCharges;
        OnChargesChanged?.Invoke(Charges, EffectiveMax());
    }
    void AddCharge(int amount = 1)
    {
        if (useCooldown) return; // ← 쿨타임 모드에선 무시
        if (amount <= 0) return;
        if (maxCharges > 0) Charges = Mathf.Min(Charges + amount, maxCharges);
        else Charges += amount;
        OnChargesChanged?.Invoke(Charges, EffectiveMax());
    }
    bool ConsumeCharge()
    {
        if (useCooldown) return true; // ← 쿨타임 모드면 소비하지 않음
        if (Charges <= 0) return false;
        Charges--;
        OnChargesChanged?.Invoke(Charges, EffectiveMax());
        return true;
    }
    int EffectiveMax() => maxCharges > 0 ? maxCharges : Mathf.Max(baseCharges, Charges);

    void HandleSetReset()
    {
        if (!useCooldown) ResetChargesToBase(); // 쿨타임 모드면 리셋 시 아무 것도 안 함
    }
    void HandleZoneConsumed(int _)
    {
        if (!useCooldown) AddCharge(1); // 쿨타임 모드면 보상 무시
    }

    // 외부에서 드래그 방향/세기를 받아서 발사
    public void Launch(Vector3 dir, float pull)
    {
        // ① 발사 가능 체크
        if (useCooldown)
        {
            if (CooldownRemain > 0f) return;
        }
        else
        {
            if (!ConsumeCharge()) return;
        }

        // ② 실제 발사
        dir.y = 0f; dir.Normalize();
        rb.linearVelocity = dir * (pull * powerScale);
        //launched = true;

        // ③ 쿨타임 시작
        if (useCooldown) StartCooldown();
    }
    // 벽 튕김수 변경 시 호출됨
void HandleWallHitsChanged_Bonus(int hitsNow)
{
    if (!useCooldown) return;                  // 쿨타임 모드일 때만
    if (!cooldownBonusOnBounce) return;        // 토글 Off면 무시

    int delta = Mathf.Max(0, hitsNow - _lastWallHitsForBonus);
    _lastWallHitsForBonus = hitsNow;
    if (delta <= 0) return;

    ReduceCooldown(delta * cooldownReducePerBounce);
}

// 남은 쿨다운을 줄이는 유틸
void ReduceCooldown(float seconds)
{
    if (seconds <= 0f) return;
    CooldownRemain = Mathf.Max(0f, CooldownRemain - seconds);
    NotifyCooldown(); // HUD 갱신
}
/*
        void FixedUpdate()
        {
            UpdateCurrentTile(forceEvent:false);

            if (launched && rb.linearVelocity.magnitude < minStopSpeed)
            {
                launched = false;
                rb.linearVelocity = Vector3.zero;
                SnapToTileCenterAndReport();
            }
        }
    */
    // === 내부 유틸 ===
    void UpdateCurrentTile(bool forceEvent)
    {
        if (!board) { HasValidTile = false; return; }

        int ix, iy;
        Vector3 pos = transform.position;
        if (!board.WorldToIndex(pos, out ix, out iy))
        {
            var local = pos - board.origin;
            ix = Mathf.Clamp(Mathf.FloorToInt(local.x / board.tileSize), 0, board.width - 1);
            iy = Mathf.Clamp(Mathf.FloorToInt(local.z / board.tileSize), 0, board.height - 1);
            HasValidTile = false;
        }
        else HasValidTile = true;

        CurrentTile = new Vector2Int(ix, iy);

        if (forceEvent || CurrentTile != _lastTile)
        {
            _lastTile = CurrentTile;
            OnTileChanged?.Invoke(ix, iy);
        }
    }

    void SnapToTileCenterAndReport()
    {
        if (!board) return;

        Vector3 p = transform.position;
        board.SnapToNearest(ref p, out int ix, out int iy);
        transform.position = p;
        CurrentTile = new Vector2Int(ix, iy);
        _lastTile   = CurrentTile;
        HasValidTile = true;

        OnTileChanged?.Invoke(ix, iy);
        OnStoppedOnTile?.Invoke(ix, iy);
    }
}
