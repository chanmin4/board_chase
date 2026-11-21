using System.Collections.Generic;
using UnityEngine;

public partial class EnemyShooterCombatController
{
    private static readonly HashSet<EnemyShooterCombatController> FactionListeners=new();
    private float _nextFactionBroadcast;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetFactionDiscovery()=>FactionListeners.Clear();
    private void RegisterFactionDiscovery()=>FactionListeners.Add(this);
    private void UnregisterFactionDiscovery()=>FactionListeners.Remove(this);
    private SectorRuntime DiscoverySector => EntityComponentResolver.Resolve<Enemy>(this)?.CurrentSector ??
        GetComponentInParent<SectorRuntime>();

    // Called only from a real direct sight or a validated attacker damage report, never from hearing/shared memory.
    private void ShareDetectedTarget(Transform target, bool force=false)
    {
        var rules=FactionDiscoveryRulesSO.Active;
        if(target==null||Config==null||(rules!=null&&!rules.enabled)||
            (!force&&Time.time<_nextFactionBroadcast)||_damageable?.IsDead==true)return;
        _nextFactionBroadcast=Time.time+Mathf.Max(.05f,rules?.visibleRefreshSeconds??.5f);
        float radius=rules?.radius??45f;
        var faction=ShooterFactionUtility.Resolve(this);
        var sourceSector=DiscoverySector;
        // Snapshot avoids registry mutation during callbacks.
        foreach(var receiver in new List<EnemyShooterCombatController>(FactionListeners))
        {
            if(receiver==null||receiver==this||!receiver.isActiveAndEnabled||receiver.Config==null||
                receiver._damageable?.IsDead==true||ShooterFactionUtility.Resolve(receiver)!=faction||
                !FactionDiscoveryRulesSO.InRange(transform.position,receiver.transform.position,radius))continue;
            if((rules?.sameSectorOnly??false)&&!LandmarkPrefab.SameGroup(sourceSector,receiver.DiscoverySector))continue;
            receiver.ReceiveFactionDiscovery(target,target.position);
        }
    }
    private void ReceiveFactionDiscovery(Transform target,Vector3 lastConfirmedPosition)
    {
        // Do not interrupt a different directly visible opponent or reinitialize the HFSM.
        if(CanSeeTarget&&_target!=target)return;
        bool alreadyVisible=_target==target&&_canSeeTarget;
        SetTarget(target);
        _isAlerted=true;
        _canSeeTarget=_vision==null||_vision.CanSeeTarget(target);
        _hasSeenTargetDuringCurrentEngagement=true;
        _hasSoundInvestigation=false;
        _lastSeenTime=Time.time;
        SetLastKnownPosition(lastConfirmedPosition);
        if(_canSeeTarget)
        {
            if(!alreadyVisible)BeginDirectSightReaction(false);
            RefreshTargetLock();
        }
        else
        {
            // SetTarget normally opens a lock which follows the live transform through walls.
            // An indirect report supplies a location, not that extra live-position privilege.
            _targetLockUntilTime=-1f;
            ClearActiveReaction();
        }
        RefreshGoal();
    }
}
