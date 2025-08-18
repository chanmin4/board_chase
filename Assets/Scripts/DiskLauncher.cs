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

    // 이벤트
    /// <summary>타일이 바뀔 때마다(이동 중에도) 계속 호출</summary>
    public event Action<int,int> OnTileChanged;
    /// <summary>정지 후 스냅이 끝난 최종 타일 리포트</summary>
    public event Action<int,int> OnStoppedOnTile;

    Rigidbody rb;
    bool launched;         // 발사 상태
    Vector2Int _lastTile = new Vector2Int(-1,-1);

    void Awake(){
        rb = GetComponent<Rigidbody>();
        if (!board){
            board = FindAnyObjectByType<BoardGrid>();
            if (!board) Debug.LogError("[DiskLauncher] BoardGrid reference missing.");
        }
        // 시작 타일 초기화
        UpdateCurrentTile(forceEvent:true);
    }

    // 외부에서 드래그 방향/세기를 받아서 발사
    public void Launch(Vector3 dir, float pull)
    {
        dir.y = 0f; dir.Normalize();
        rb.linearVelocity = dir * (pull * powerScale);   // ← 기존 코드 컨벤션 유지
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
            OnTileChanged?.Invoke(ix, iy);   // ← 생존영역 시스템은 이 이벤트만 구독해도 실시간 반응 가능
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
        OnStoppedOnTile?.Invoke(ix, iy); // 정지 이벤트 (턴 처리/생존영역 갱신 트리거)
    }
}
