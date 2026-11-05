using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.Collections;

/// <summary>Opt-in structure durability. No public health bar or extra destruction stages.</summary>
[DisallowMultipleComponent, AddComponentMenu("Board Chase/Sector/파괴 가능한 엄폐")]
public sealed class SectorDestructibleCover : MonoBehaviour
{
    [SerializeField] private bool _destructible = true;
    [SerializeField, Min(1f)] private float _maximumDurability = 500f;
    [SerializeField,Min(0)] private float _armorClass;
    [Tooltip("낮은 엄폐: 이동 충돌은 유지하고 탄환은 관통. 시야는 Sector Maker에서 OFF로 지정하세요.")]
    [SerializeField] private bool _lowCover;
    public bool AllowsBulletPass=>_lowCover;
    public float ArmorClass=>Mathf.Max(0,_armorClass);
    [Tooltip("방 안에서 고유한 배치 ID. 비어 있으면 계층 경로를 사용합니다. 배치 후 ID는 변경하지 마세요.")]
    [SerializeField] private string _placementId;
    private float _remaining;
    private RaidCoverSaveData _saved;
    private SectorRuntime _sector;
    private bool _broken;
    public bool IsDestructible => _destructible && enabled;
    public void Configure(bool enabledForDamage, float durability)
    {
        _destructible = enabledForDamage;
        _maximumDurability = Mathf.Max(1f, durability);
    }
    private void Awake() { _remaining = Mathf.Max(1f, _maximumDurability); }

    public static void BindSector(SectorRuntime sector, RaidRunSaveData raid)
    {
        if (sector == null || raid == null) return;
        foreach (var cover in sector.GetComponentsInChildren<SectorDestructibleCover>(true))
            cover.Bind(sector, raid);
    }
    private void Bind(SectorRuntime sector, RaidRunSaveData raid)
    {
        if (_saved != null || !IsDestructible) return;
        _sector = sector;
        string key = _placementId;
        if (string.IsNullOrWhiteSpace(key))
        {
            key = "";
            for (Transform t = transform; t != null && t != sector.transform; t = t.parent)
                key = t.name + ":" + t.GetSiblingIndex() + "/" + key;
        }
        raid.covers ??= new();
        _saved = raid.covers.Find(x => x != null && x.coord == sector.Coord && x.placementId == key);
        if (_saved == null)
        {
            _saved = new RaidCoverSaveData { coord = sector.Coord, placementId = key, remainingDurability = Mathf.Max(1f, _maximumDurability) };
            raid.covers.Add(_saved);
        }
        _remaining = Mathf.Clamp(_saved.remainingDurability, 0f, Mathf.Max(1f, _maximumDurability));
        if (_remaining <= 0f) { _broken = true; gameObject.SetActive(false); }
    }
    public void ReceiveProjectileDamage(float damage)
    {
        if (_broken || !gameObject.activeInHierarchy || !IsDestructible || damage <= 0f || float.IsNaN(damage) || float.IsInfinity(damage)) return;
        if (_sector == null) _sector = GetComponentInParent<SectorRuntime>();
        if (_saved == null && _sector != null && RaidRuntime.Active?.State != null)
            Bind(_sector, RaidRuntime.Active.State);
        if (_broken) return; // Restored broken covers never replay a destruction effect.
        _remaining = Mathf.Max(0f, _remaining - damage);
        if (_saved != null) _saved.remainingDurability = _remaining;
        if (_remaining > 0f) return;
        _broken = true;
        SectorDestructionEffectPlayer.PlayFor(transform);
        // Disable only the opted-in object: geometry, physical blockers, cover points and LOS registry leave together.
        gameObject.SetActive(false);
        if (_sector != null) SectorCoverNavMeshUpdater.Request(_sector);
    }
    public float ReceiveProjectileDamage(float damage,float penetration)
    {
        float multiplier=ArmorPenetrationDamageCurve.RollMultiplier(ArmorClass,penetration);
        ReceiveProjectileDamage(damage*multiplier);
        return multiplier;
    }
}

/// <summary>Coalesces breaks per sector and asynchronously updates its existing runtime NavMesh.</summary>
public sealed class SectorCoverNavMeshUpdater : MonoBehaviour
{
    private bool _dirty, _running;
    public static void Request(SectorRuntime sector)
    {
        var updater = sector.GetComponent<SectorCoverNavMeshUpdater>();
        if (updater == null) updater = sector.gameObject.AddComponent<SectorCoverNavMeshUpdater>();
        updater._dirty = true;
    }
    private void LateUpdate() { if (_dirty && !_running) StartCoroutine(Rebuild()); }
    private IEnumerator Rebuild()
    {
        _running = true;
        yield return null;
        while (_dirty)
        {
            _dirty = false;
            var surface = GetComponent<NavMeshSurface>();
            if (surface != null && surface.isActiveAndEnabled && surface.navMeshData != null)
            {
                Physics.SyncTransforms();
                yield return surface.UpdateNavMesh(surface.navMeshData);
                foreach (var agent in GetComponentsInChildren<NavMeshAgent>())
                    if (agent.isActiveAndEnabled && agent.isOnNavMesh && agent.hasPath)
                        agent.SetDestination(agent.destination);
            }
        }
        _running = false;
    }
}
