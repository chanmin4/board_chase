using UnityEngine;

/// <summary>같은 살아 있는 섹터를 재방문/재호출해도 배치 루팅을 다시 추첨하지 않습니다.</summary>
[DisallowMultipleComponent, AddComponentMenu("")]
public sealed class SectorContentPopulationState : MonoBehaviour
{
    [System.NonSerialized] public RaidSectorContentSaveData Snapshot;
    [System.NonSerialized] public bool Initialized;
}
