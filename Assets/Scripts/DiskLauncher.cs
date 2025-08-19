using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody))]
public class DiskLauncher : MonoBehaviour
{
    [Header("Launch / Stop")]
    public float powerScale   = 0.16f;   // 드래그 게이지→속도 스케일
    public float minStopSpeed = 0.25f;   // 이 속도 미만이면 '멈춤' 판정

    [Header("Grid")]
    [SerializeField] BoardGrid board;    // 반드시 연결(그리드 스냅/좌표 변환)
    public Vector2Int CurrentTile { get; private set; } = new Vector2Int(-1,-1);
    public bool HasValidTile { get; private set; } = false;

    [Header("Bounce Charges")]
    [Tooltip("세트가 시작될 때(또는 게임 시작) 충전되는 기본 횟수")]
    public int baseCharges = 2;
    [Tooltip("보상으로 늘어날 수 있는 최대 한도(0 이하면 제한 없음)")]
    public int maxCharges = 5; // 0 or less => unlimited cap
    public int Charges { get; private set; }

    // 이벤트
    /// <summary>타일이 바뀔 때마다(이동 중에도) 계속 호출</summary>
    public event Action<int,int> OnTileChanged;
    /// <summary>정지 후 스냅이 끝난 최종 타일 리포트</summary>
    public event Action<int,int> OnStoppedOnTile;
    /// <summary>충전량이 바뀔 때마다 (현재, 최대) 보고</summary>
    public event Action<int,int> OnChargesChanged;

    Rigidbody rb;
    bool launched;         // 발사 상태
    Vector2Int _lastTile = new Vector2Int(-1,-1);

    // 외부 참조(세트 시작/소비 이벤트 구독용)
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
            // 세트 시작마다 기본 2회로 초기화
            director.OnZonesReset  += HandleSetReset;
            // 존 성공 진입 시 +1
            director.OnZoneConsumed += HandleZoneConsumed;
        }

        // 시작 타일 초기화
        UpdateCurrentTile(forceEvent:true);

        // 게임 시작 시에도 기본 2회
        ResetChargesToBase();
    }

    void OnDestroy()
    {
        if (director != null)
        {
            director.OnZonesReset   -= HandleSetReset;
            director.OnZoneConsumed -= HandleZoneConsumed;
        }
    }

    // === 충전(횟수) 제어 ===
    void ResetChargesToBase()
    {
        Charges = baseCharges;
        if (maxCharges > 0 && Charges > maxCharges) Charges = maxCharges;
        OnChargesChanged?.Invoke(Charges, EffectiveMax());
    }
    void AddCharge(int amount = 1)
    {
        if (amount <= 0) return;
        if (maxCharges > 0) Charges = Mathf.Min(Charges + amount, maxCharges);
        else Charges += amount;
        OnChargesChanged?.Invoke(Charges, EffectiveMax());
    }
    bool ConsumeCharge()
    {
        if (Charges <= 0) return false;
        Charges--;
        OnChargesChanged?.Invoke(Charges, EffectiveMax());
        return true;
    }
    int EffectiveMax() => maxCharges > 0 ? maxCharges : Mathf.Max(baseCharges, Charges);

    void HandleSetReset()
    {
        // 세트(setRemain) 초기화 시 발사 가능 횟수도 초기화
        ResetChargesToBase();
    }
    void HandleZoneConsumed(int _)
    {
        // 존에 들어갈 때마다 +1
        AddCharge(1);
    }

    // 외부에서 드래그 방향/세기를 받아서 발사
    public void Launch(Vector3 dir, float pull)
    {
        // 발사 가능 횟수 체크
        if (!ConsumeCharge())
        {
            // 필요하다면 여기서 UI/사운드 알림을 호출해도 됨
            // Debug.Log("[DiskLauncher] No charges left.");
            return;
        }

        dir.y = 0f; dir.Normalize();
        rb.linearVelocity = dir * (pull * powerScale);   // 표준 속성 사용
        launched = true;
    }

    void FixedUpdate()
    {
        // 1) 항상 현 타일 계산 (움직이는 중에도)
        UpdateCurrentTile(forceEvent:false);

        // 2) 멈춤 판정 → 스냅 + 최종 리포트
        if (launched && rb.linearVelocity.magnitude < minStopSpeed)
        {
            launched = false;
            rb.linearVelocity = Vector3.zero;
            SnapToTileCenterAndReport();
        }
    }

    // === 내부 유틸 ===

    void UpdateCurrentTile(bool forceEvent)
    {
        if (!board) { HasValidTile = false; return; }

        int ix, iy;
        Vector3 pos = transform.position;
        // 보드 밖이어도 클램프 스냅 기준으로 인덱스 산출
        if (!board.WorldToIndex(pos, out ix, out iy))
        {
            // 인덱스만 보정 (스냅은 멈췄을 때)
            var local = pos - board.origin;
            ix = Mathf.Clamp(Mathf.FloorToInt(local.x / board.tileSize), 0, board.width - 1);
            iy = Mathf.Clamp(Mathf.FloorToInt(local.z / board.tileSize), 0, board.height - 1);
            HasValidTile = false; // 아직 보드 밖이므로 센터 스냅 전
        }
        else HasValidTile = true;

        CurrentTile = new Vector2Int(ix, iy);

        if (forceEvent || CurrentTile != _lastTile)
        {
            _lastTile = CurrentTile;
            OnTileChanged?.Invoke(ix, iy);   // 생존영역 시스템은 이 이벤트만 구독해도 실시간 반응 가능
        }
    }

    void SnapToTileCenterAndReport()
    {
        if (!board) return;

        Vector3 p = transform.position;
        board.SnapToNearest(ref p, out int ix, out int iy);
        transform.position = p;          // 타일 센터로 위치 스냅
        CurrentTile = new Vector2Int(ix, iy);
        _lastTile   = CurrentTile;
        HasValidTile = true;

        OnTileChanged?.Invoke(ix, iy);   // 스냅으로 바뀐 경우도 브로드캐스트
        OnStoppedOnTile?.Invoke(ix, iy); // 정지 이벤트 (턴 처리/세트 갱신 트리거 등)
    }
}
