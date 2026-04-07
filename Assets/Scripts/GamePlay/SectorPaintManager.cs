using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 완전 독립적인 페인트 요청 허브.
/// 
/// 이 클래스는 맵, 보드, 그리드, 렌더러를 전혀 모른다.
/// 그냥 "원이 하나 찍혀도 되는지"를 등록된 SectorPaint들에게 물어보고,
/// 가능하면 그쪽으로 요청을 전달하는 역할만 담당한다.
/// </summary>
[DisallowMultipleComponent]
public class SectorPaintManager : MonoBehaviour
{
    /// <summary>
    /// 누가 칠하는지 구분하는 채널.
    /// 플레이어/적 외에 나중에 Neutral, Ally 등으로 확장 가능.
    /// </summary>
    public enum PaintChannel
    {
        Vaccine,
        Virus
    }

    /// <summary>
    /// 원형 페인트 요청 데이터.
    /// BoardPaintManager는 이 데이터만 만들어서 SectorPaint에 넘긴다.
    /// </summary>
    [Serializable]
    public struct CirclePaintRequest
    {
        public PaintChannel channel;
        public Vector3 worldPos;
        public float radiusWorld;
        public int priority;
        public object sender;
    }

    /// <summary>
    /// 현재 매니저에 등록된 모든 SectorPaint 목록.
    /// </summary>
    private readonly List<SectorPaint> _registeredSectors = new List<SectorPaint>();

    /// <summary>
    /// 페인트 요청이 최소 1개 섹터에 의해 수락되었을 때 호출된다.
    /// </summary>
    public event Action<CirclePaintRequest> OnCircleRequestAccepted;

    /// <summary>
    /// 페인트 요청이 어떤 섹터에도 수락되지 않았을 때 호출된다.
    /// </summary>
    public event Action<CirclePaintRequest> OnCircleRequestRejected;

    /// <summary>
    /// SectorPaint가 자신을 이 매니저에 등록할 때 사용한다.
    /// 중복 등록은 막는다.
    /// </summary>
    public void RegisterSector(SectorPaint sector)
    {
        if (sector == null)
            return;

        if (_registeredSectors.Contains(sector))
            return;

        _registeredSectors.Add(sector);
    }

    /// <summary>
    /// SectorPaint가 비활성화/삭제될 때 자신을 등록 해제할 때 사용한다.
    /// </summary>
    public void UnregisterSector(SectorPaint sector)
    {
        if (sector == null)
            return;

        _registeredSectors.Remove(sector);
    }

    /// <summary>
    /// 현재 요청이 "적어도 하나의 섹터"에서 받아들여질 수 있는지만 미리 확인한다.
    /// 
    /// 실제 저장/적용은 하지 않는다.
    /// 진짜로 "여기 찍어도 돼?"만 묻는 용도.
    /// </summary>
    public bool CanRequestCircle(
        PaintChannel channel,
        Vector3 worldPos,
        float radiusWorld,
        object sender = null)
    {
        CirclePaintRequest request = BuildCircleRequest(channel, worldPos, radiusWorld, 0, sender);

        for (int i = 0; i < _registeredSectors.Count; i++)
        {
            SectorPaint sector = _registeredSectors[i];

            if (sector == null)
                continue;

            if (sector.CanAcceptCircle(request))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 실제 원형 페인트 요청을 등록된 모든 SectorPaint에 전달한다.
    /// 
    /// 수락 가능한 SectorPaint만 ApplyCircle을 실행한다.
    /// 하나 이상 수락되면 true를 반환한다.
    /// </summary>
    public bool RequestCircle(
        PaintChannel channel,
        Vector3 worldPos,
        float radiusWorld,
        int priority = 0,
        object sender = null)
    {
        CirclePaintRequest request = BuildCircleRequest(channel, worldPos, radiusWorld, priority, sender);

        bool accepted = false;

        for (int i = 0; i < _registeredSectors.Count; i++)
        {
            SectorPaint sector = _registeredSectors[i];

            if (sector == null)
                continue;

            if (!sector.CanAcceptCircle(request))
                continue;

            sector.ApplyCircle(request);
            accepted = true;
        }

        if (accepted)
            OnCircleRequestAccepted?.Invoke(request);
        else
            OnCircleRequestRejected?.Invoke(request);

        return accepted;
    }

    /// <summary>
    /// 요청 데이터를 만드는 내부 헬퍼 함수.
    /// radius는 최소값 보정만 해준다.
    /// </summary>
    private CirclePaintRequest BuildCircleRequest(
        PaintChannel channel,
        Vector3 worldPos,
        float radiusWorld,
        int priority,
        object sender)
    {
        CirclePaintRequest request = new CirclePaintRequest
        {
            channel = channel,
            worldPos = worldPos,
            radiusWorld = Mathf.Max(0.001f, radiusWorld),
            priority = priority,
            sender = sender
        };

        return request;
    }
}