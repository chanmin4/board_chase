using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전체 섹터 해금 상태를 관리하는 매니저.
/// 
/// 역할:
/// 1. 씬의 SectorRuntime들을 coord 기준으로 수집
/// 2. 시작 시 isStartSector만 열기
/// 3. StageSectorUnlockTableSO를 참고해 단계별로 섹터 해금
/// 4. Open Project 스타일 이벤트 채널 발행
/// </summary>
public class SectorStateManager : MonoBehaviour
{
    [Header("Stage Data")]
    [SerializeField] private StageSectorUnlockSO unlockStage;

    [Header("Requests")]
    [Tooltip("외부에서 다음 스테이지 해금을 요청하는 이벤트")]
    [SerializeField] private IntEventChannelSO RequestUnlockNextStageEvent;

    [Tooltip("외부에서 '다음 스테이지로 진행' 요청을 보내는 이벤트")]
    [SerializeField] private VoidEventChannelSO RequestProgressNextStageEvent;

    [Header("Broadcasts")]
    [Tooltip("시작 섹터가 결정되고 초기화가 끝났을 때 발행")]
    [SerializeField] private SectorRuntimeEventChannelSO startSectorReadyEvent=default;

    [Tooltip("섹터 하나가 새로 열릴 때마다 발행")]
    [SerializeField] private SectorRuntimeEventChannelSO sectorOpenedEvent=default;

    [Tooltip("현재 스테이지가 적용됐을 때 발행")]
    [SerializeField] private IntEventChannelSO stageAppliedEvent=default;

    [Header("Options")]
    [SerializeField] private bool openOnlyStartSectorOnBoot = true;
    [SerializeField] private int initialStage = 0;

    /// <summary>
    /// 현재 적용된 스테이지 번호.
    /// </summary>
    public int CurrentStage { get; private set; } = -1;

    /// <summary>
    /// 시작 섹터 캐시.
    /// </summary>
    public SectorRuntime StartSector { get; private set; }

    private readonly Dictionary<Vector2Int, SectorRuntime> _sectorByCoord
        = new Dictionary<Vector2Int, SectorRuntime>();

    private SectorRuntime[] _allSectors;

    private void Awake()
    {
        CacheSectors();
        ResolveStartSector();
    }

    private void OnEnable()
    {
        if (RequestUnlockNextStageEvent != null)
            RequestUnlockNextStageEvent.OnEventRaised += UnlockNextStage;
    }

    private void OnDisable()
    {
        if (RequestUnlockNextStageEvent != null)
            RequestUnlockNextStageEvent.OnEventRaised -= UnlockNextStage;
    }

    private void Start()
    {
        InitializeSectorOpenState();

        if (StartSector != null)
            startSectorReadyEvent.RaiseEvent(StartSector);
    }

    /// <summary>
    /// 씬에 있는 모든 SectorRuntime을 coord 기준으로 캐시한다.
    /// </summary>
    private void CacheSectors()
    {
        _allSectors = FindObjectsByType<SectorRuntime>(FindObjectsSortMode.None);
        _sectorByCoord.Clear();

        for (int i = 0; i < _allSectors.Length; i++)
        {
            SectorRuntime sector = _allSectors[i];
            if (sector == null)
                continue;

            if (_sectorByCoord.ContainsKey(sector.coord))
            {
                Debug.LogWarning(
                    $"[SectorStateManager] Duplicate sector coord found: {sector.coord}");
                continue;
            }

            _sectorByCoord.Add(sector.coord, sector);
        }
    }

    /// <summary>
    /// isStartSector=true 인 섹터를 시작 섹터로 잡는다.
    /// 없으면 첫 번째 섹터를 fallback으로 사용한다.
    /// </summary>
    private void ResolveStartSector()
    {
        StartSector = null;

        for (int i = 0; i < _allSectors.Length; i++)
        {
            if (_allSectors[i] != null && _allSectors[i].isStartSector)
            {
                StartSector = _allSectors[i];
                return;
            }
        }

        if (_allSectors.Length > 0)
            StartSector = _allSectors[0];
    }

    /// <summary>
    /// 시작 시 섹터 열림 상태를 초기화한다.
    /// 옵션이 켜져 있으면 시작 섹터만 열고 나머지는 전부 닫는다.
    /// </summary>
    private void InitializeSectorOpenState()
    {
        for (int i = 0; i < _allSectors.Length; i++)
        {
            SectorRuntime sector = _allSectors[i];
            if (sector == null)
                continue;

            if (openOnlyStartSectorOnBoot)
                sector.isOpened = (sector == StartSector);
        }

        CurrentStage = -1;
    }

    /// <summary>
    /// 외부에서 다음 스테이지로 진행시키고 싶을 때 호출.
    /// </summary>
    public void ProgressNextStage()
    {
        UnlockNextStage(CurrentStage + 1);
    }

    /// <summary>
    /// 특정 스테이지를 적용한다.
    /// 
    /// 이 매니저는 "해금" 개념으로 동작하므로,
    /// 이미 열린 섹터는 유지하고, 새로 열릴 섹터만 추가로 연다.
    /// </summary>
    public void UnlockNextStage(int stageIndex)
    {
        if (unlockStage == null)
        {
            Debug.LogWarning("[SectorStateManager] Unlock table is missing.");
            return;
        }

        if (stageIndex < 0)
            return;

        if (stageIndex <= CurrentStage)
        {
            // 해금형 구조이므로 같은 단계/이전 단계 요청은 무시
            return;
        }

        for (int s = CurrentStage + 1; s <= stageIndex; s++)
        {
            UnlockSingleStageStep(s);
        }

        CurrentStage = stageIndex;

        if (stageAppliedEvent != null)
            stageAppliedEvent.RaiseEvent(CurrentStage);
    }

    /// <summary>
    /// 스테이지 한 단계 분량의 해금 데이터를 적용한다.
    /// </summary>
    private void UnlockSingleStageStep(int stageIndex)
    {
        if (!unlockStage.TryGetStep(stageIndex, out var step))
            return;

        if (step.sectorCoordsToOpen == null)
            return;

        for (int i = 0; i < step.sectorCoordsToOpen.Length; i++)
        {
            OpenSector(step.sectorCoordsToOpen[i]);
        }
    }

    /// <summary>
    /// coord 기준으로 해당 섹터를 연다.
    /// 이미 열려 있으면 아무 일도 하지 않는다.
    /// </summary>
    public bool OpenSector(Vector2Int coord)
    {
        if (!_sectorByCoord.TryGetValue(coord, out SectorRuntime sector))
            return false;

        return OpenSector(sector);
    }

    /// <summary>
    /// SectorRuntime 하나를 직접 연다.
    /// 새로 열린 경우에만 이벤트를 발행한다.
    /// </summary>
    public bool OpenSector(SectorRuntime sector)
    {
        if (sector == null)
            return false;

        if (sector.isOpened)
            return false;

        sector.isOpened = true;

        if (sectorOpenedEvent != null)
            sectorOpenedEvent.RaiseEvent(sector);

        return true;
    }

    /// <summary>
    /// coord 기준으로 섹터를 찾는다.
    /// </summary>
    public bool TryGetSector(Vector2Int coord, out SectorRuntime sector)
    {
        return _sectorByCoord.TryGetValue(coord, out sector);
    }

    /// <summary>
    /// 시작 섹터만 다시 남기고 리셋한다.
    /// 디버그/재시작용.
    /// </summary>
    public void ResetToStartOnly()
    {
        InitializeSectorOpenState();

        if (StartSector != null && startSectorReadyEvent != null)
            startSectorReadyEvent.RaiseEvent(StartSector);
    }
}