/*
 * Role hierarchy: Gameplay > Progression > Hideout
 * Primary responsibility: Owns manually authored Hideout services and resident reset hooks.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class HomeSector : MonoBehaviour
{
    [Header("Runtime Root")]
    [SerializeField] private SectorRuntime _sectorRuntime;

    [Header("Home Residents")]
    [Tooltip("Explicit Home quest NPC Damageable components reset whenever the player enters the HomeSector. Do not register the player or normal combat entities here.")]
    [SerializeField] private Damageable[] _homeResidents = Array.Empty<Damageable>();

    public SectorRuntime SectorRuntime => _sectorRuntime;
    [Header("HOME 고정 안전 스폰")]
    [SerializeField] private Transform _safeSpawnPoint;
    [SerializeField,Min(.1f)] private float _spawnClearanceRadius=.6f;
    [SerializeField,Min(0)] private float _fallbackSearchRadius=8f;
    private Transform _resolvedSafeSpawn;
    private Transform _authoredSpawn;
    public bool TryPrepareSafeSpawn(out string message)
    {
        message="HOME의 안전한 스폰 지점을 찾지 못했습니다. 고정 스폰 포인트 주변을 비워주세요.";
        if(_sectorRuntime==null)return false;
        Transform authored=_safeSpawnPoint!=null?_safeSpawnPoint:(_authoredSpawn!=null?_authoredSpawn:_authoredSpawn=_sectorRuntime.PlayerStartPoint);
        if(authored==null)return false;
        Vector3 preferred=authored.position;
        if(IsSpawnClear(preferred))return SetSafeSpawn(preferred,out message);
        for(float distance=1;distance<=_fallbackSearchRadius;distance+=1)
            for(int direction=0;direction<8;direction++)
            {
                float angle=direction*Mathf.PI/4;var candidate=preferred+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*distance;
                if(!UnityEngine.AI.NavMesh.SamplePosition(candidate,out var hit,1,UnityEngine.AI.NavMesh.AllAreas)||!IsSpawnClear(hit.position))continue;
                return SetSafeSpawn(hit.position,out message);
            }
        return false;
    }
    private bool IsSpawnClear(Vector3 point)
    {
        bool hasFloor=false;
        foreach(var hit in Physics.RaycastAll(point+Vector3.up*2,Vector3.down,5,~0,QueryTriggerInteraction.Ignore))
            if(hit.collider.GetComponentInParent<PlayerCharacter>()==null&&hit.normal.y>.65f){hasFloor=true;break;}
        if(!hasFloor)return false;
        foreach(var collider in Physics.OverlapCapsule(point+Vector3.up*.7f,point+Vector3.up*1.5f,_spawnClearanceRadius,~0,QueryTriggerInteraction.Ignore))
            if(collider!=null&&collider.GetComponentInParent<PlayerCharacter>()==null)return false;
        return true;
    }
    private bool SetSafeSpawn(Vector3 point,out string message)
    {
        if(_resolvedSafeSpawn==null)
        {
            _resolvedSafeSpawn=new GameObject("HOME 안전 스폰 · Runtime Anchor").transform;
            _resolvedSafeSpawn.SetParent(transform,false);
        }
        _resolvedSafeSpawn.position=point;_sectorRuntime.ConfigurePlayerStartPoint(_resolvedSafeSpawn);
        message="HOME 안전 스폰 확인 완료.";return true;
    }

    // Purpose: Initializes initialize for use by this component.
    public void Initialize(SectorRuntime sectorRuntime)
    {
        _sectorRuntime = sectorRuntime != null ? sectorRuntime : _sectorRuntime;

        if (_sectorRuntime == null)
            TryGetComponent(out _sectorRuntime);
    }

    // Purpose: Performs the reset home residents workflow for this component.
    public void ResetHomeResidents()
    {
        for (int i = 0; i < _homeResidents.Length; i++)
        {
            Damageable resident = _homeResidents[i];
            if (resident == null)
                continue;

            resident.gameObject.SetActive(true);
            resident.ResetHealthFromConfig();
        }

        HomeQuestNpc[] questNpcs = GetComponentsInChildren<HomeQuestNpc>(true);
        for (int i = 0; i < questNpcs.Length; i++)
        {
            if (questNpcs[i] != null)
                questNpcs[i].ResetForHomeVisit();
        }
    }

#if UNITY_EDITOR
    // Purpose: Normalizes Inspector-authored data after it changes in the Editor.
    private void OnValidate()
    {
        if (_sectorRuntime == null)
            TryGetComponent(out _sectorRuntime);
    }
#endif
}
