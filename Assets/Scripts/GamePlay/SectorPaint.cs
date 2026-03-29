using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 특정 구역(섹터)의 페인트 판정 및 저장 담당.
/// 
/// BoardPaintManager가 요청을 넘기면,
/// 이 클래스가 "우리 구역에 적용 가능한가?"를 판단하고,
/// 가능하면 자기 내부 상태에 저장한다.
/// 
/// 실제 마스크 렌더링을 나중에 붙일 위치도 여기다.
/// </summary>
[DisallowMultipleComponent]
public class SectorPaint : MonoBehaviour
{
    /// <summary>
    /// 현재 섹터 내부에 저장된 실제 페인트 기록 1개.
    /// 지금은 단순히 원형 스탬프 이력만 저장한다.
    /// 나중에 텍스처 마스크나 타일 점유 계산으로 바꿔도 된다.
    /// </summary>
    [Serializable]
    public struct StoredCirclePaint
    {
        public BoardPaintManager.PaintChannel channel;
        public Vector3 worldPos;
        public float radiusWorld;
        public int priority;
        public float appliedTime;
        public object sender;
    }

    [Header("Refs")]
    [SerializeField] private BoardPaintManager paintManager;
    [SerializeField] private SectorRuntime runtime;
    [SerializeField] private MaskRenderer maskRenderer;


    [Header("Options")]
    [SerializeField] private bool allowPaintWhenClosed = false;
    [SerializeField] private float boundsPadding = 0.1f;

    /// <summary>
    /// 플레이어가 이 섹터에 남긴 페인트 기록 목록.
    /// </summary>
    private readonly List<StoredCirclePaint> _playerPaints = new List<StoredCirclePaint>();

    /// <summary>
    /// 적이 이 섹터에 남긴 페인트 기록 목록.
    /// </summary>
    private readonly List<StoredCirclePaint> _enemyPaints = new List<StoredCirclePaint>();

    /// <summary>
    /// 이 섹터가 요청을 실제로 적용했을 때 호출된다.
    /// </summary>
    public event Action<SectorPaint, StoredCirclePaint> OnCircleApplied;

    /// <summary>
    /// 현재 섹터에 저장된 플레이어 페인트 개수.
    /// </summary>
    public int PlayerPaintCount => _playerPaints.Count;

    /// <summary>
    /// 현재 섹터에 저장된 적 페인트 개수.
    /// </summary>
    public int EnemyPaintCount => _enemyPaints.Count;

    private void Reset()
    {
        if (!runtime)
            runtime = GetComponent<SectorRuntime>();


        if (!paintManager)
            paintManager = FindAnyObjectByType<BoardPaintManager>();
    }

    private void Awake()
    {
        if (!runtime)
            runtime = GetComponent<SectorRuntime>();


        if (!paintManager)
            paintManager = FindAnyObjectByType<BoardPaintManager>();
    }

    private void OnEnable()
    {
        if (paintManager != null)
            paintManager.RegisterSector(this);
    }

    private void OnDisable()
    {
        if (paintManager != null)
            paintManager.UnregisterSector(this);
    }

    /// <summary>
    /// 이 섹터가 현재 해당 원형 요청을 받아들일 수 있는지 검사한다.
    /// 
    /// 여기서 하는 일:
    /// - 섹터가 닫혀있는데 칠하기 금지인지
    /// - 원형이 이 섹터 bounds와 겹치는지
    /// 
    /// BoardPaintManager는 이 함수 결과만 보고 전달 여부를 결정한다.
    /// </summary>
    public bool CanAcceptCircle(BoardPaintManager.CirclePaintRequest request)
    {
        if (!CanPaintNow())
            return false;

        return IntersectsCircle(request.worldPos, request.radiusWorld + boundsPadding);
    }

    /// <summary>
    /// 실제로 이 섹터가 원형 요청을 받아서 자기 내부 기록에 저장한다.
    /// 
    /// 지금은 "저장만" 한다.
    /// 나중에 여기에 실제 마스크 텍스처 갱신,
    /// 점유율 계산, 타일 반영 같은 로직을 붙이면 된다.
    /// </summary>
  

public void ApplyCircle(BoardPaintManager.CirclePaintRequest request)
{
    if (maskRenderer == null)
        return;

    switch (request.channel)
    {
        case BoardPaintManager.PaintChannel.Player:
            maskRenderer.StampVaccineCircle(request.worldPos, request.radiusWorld, true);
            break;

        case BoardPaintManager.PaintChannel.Enemy:
            maskRenderer.StampVirusCircle(request.worldPos, request.radiusWorld, true);
            break;
    }
}

    /// <summary>
    /// 특정 월드 위치가 이 섹터 내부에 들어오는지 검사한다.
    /// radius 없이 점 하나만 확인하는 용도.
    /// </summary>
    public bool ContainsPoint(Vector3 worldPos)
    {
        Bounds bounds = runtime.GetWorldBounds();

        return worldPos.x >= bounds.min.x &&
               worldPos.x <= bounds.max.x &&
               worldPos.z >= bounds.min.z &&
               worldPos.z <= bounds.max.z;
    }

    /// <summary>
    /// 특정 원형이 이 섹터 bounds와 겹치는지 검사한다.
    /// 
    /// 지금은 AABB 기준 단순 판정이다.
    /// 정밀 판정이 필요하면 나중에 collider 기반으로 강화하면 된다.
    /// </summary>
    public bool IntersectsCircle(Vector3 worldPos, float radiusWorld)
    {
        Bounds bounds = runtime.GetWorldBounds();

        float closestX = Mathf.Clamp(worldPos.x, bounds.min.x, bounds.max.x);
        float closestZ = Mathf.Clamp(worldPos.z, bounds.min.z, bounds.max.z);

        float dx = worldPos.x - closestX;
        float dz = worldPos.z - closestZ;

        float sqrDist = (dx * dx) + (dz * dz);
        float sqrRadius = radiusWorld * radiusWorld;

        return sqrDist <= sqrRadius;
    }


    /// <summary>
    /// 현재 이 섹터가 칠하기 가능한 상태인지 검사한다.
    /// 
    /// SectorRuntime이 없으면 일단 허용하고,
    /// runtime이 있으면 isOpened 여부를 참고한다.
    /// </summary>
    public bool CanPaintNow()
    {
        if (allowPaintWhenClosed)
            return true;

        if (runtime == null)
            return true;

        return runtime.isOpened;
    }

    /// <summary>
    /// 플레이어/적 채널별로 저장된 페인트 기록 리스트를 반환한다.
    /// 읽기 전용으로만 쓰는 걸 권장.
    /// </summary>
    public IReadOnlyList<StoredCirclePaint> GetStoredPaints(BoardPaintManager.PaintChannel channel)
    {
        if (channel == BoardPaintManager.PaintChannel.Player)
            return _playerPaints;

        return _enemyPaints;
    }

    /// <summary>
    /// 현재 섹터에 저장된 모든 페인트 기록을 삭제한다.
    /// 
    /// 실제 마스크 텍스처가 생기면 그쪽 초기화도 여기서 같이 하면 된다.
    /// </summary>
    public void ClearAllStoredPaint()
    {
        _playerPaints.Clear();
        _enemyPaints.Clear();
    }

    /// <summary>
    /// 특정 채널의 페인트 기록만 삭제한다.
    /// </summary>
    public void ClearStoredPaint(BoardPaintManager.PaintChannel channel)
    {
        if (channel == BoardPaintManager.PaintChannel.Player)
            _playerPaints.Clear();
        else
            _enemyPaints.Clear();
    }
}