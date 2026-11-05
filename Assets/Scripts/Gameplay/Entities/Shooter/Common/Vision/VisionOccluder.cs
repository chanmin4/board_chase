using System.Collections.Generic;
using UnityEngine;

/// <summary>Explicit sight blocking, independent of movement/projectile physics layers.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Board Chase/Vision/시야 차단 설정")]
public sealed class VisionOccluder : MonoBehaviour
{
    [Tooltip("기본 OFF. ON일 때만 이 물체의 일반 콜라이더가 뒤쪽 시야를 막습니다. 이동/총알 충돌은 변경하지 않습니다.")]
    [SerializeField] private bool _blocksVision;
    [Tooltip("배치별 덮어쓰기용. 켜면 이 물체 아래의 개별 시야 설정보다 이 설정을 우선합니다.")]
    [SerializeField] private bool _overrideChildren;

    private static readonly HashSet<VisionOccluder> Active = new();
    public static int Revision { get; private set; }
    private static int _activeLayerMask;
    private static bool _registryDirty;
    public static int ActiveLayerMask
    {
        get
        {
            if (_registryDirty) RebuildLayerMask();
            return _activeLayerMask;
        }
    }
    public bool BlocksVision => _blocksVision;
    public bool OverridesChildren => _overrideChildren;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry()
    {
        Active.Clear(); _activeLayerMask = 0; _registryDirty = false; Revision = 0;
    }

    private void OnEnable() { Active.Add(this); RefreshRegistry(); }
    private void OnDisable() { Active.Remove(this); RefreshRegistry(); }
    private void OnTransformChildrenChanged() { if (isActiveAndEnabled) RefreshRegistry(); }
    private void OnValidate() { RefreshRegistry(); }

    public void Configure(bool blocksVision, bool overrideChildren = false)
    {
        _blocksVision = blocksVision;
        _overrideChildren = overrideChildren;
        RefreshRegistry();
    }

    private static void RefreshRegistry()
    {
        Revision++;
        _registryDirty = true;
    }

    // Lazy main-thread rebuild: loading many prefabs does not scan all markers
    // once per OnEnable, and OnValidate never performs Unity physics/object calls.
    private static void RebuildLayerMask()
    {
        _registryDirty = false;
        int mask = 0;
        foreach (VisionOccluder setting in Active)
        {
            if (setting == null || !setting.isActiveAndEnabled || !setting._blocksVision) continue;
            foreach (Collider collider in setting.GetComponentsInChildren<Collider>(true))
                if (collider != null) mask |= 1 << collider.gameObject.layer;
        }
        _activeLayerMask = mask;
    }

    public static VisionOccluder ResolveSetting(Transform target)
    {
        VisionOccluder result = null;
        for (Transform node = target; node != null; node = node.parent)
        {
            VisionOccluder setting = node.GetComponent<VisionOccluder>();
            if (setting != null && (result == null || setting._overrideChildren)) result = setting;
        }
        return result;
    }

    public static bool BlocksSight(Collider collider)
    {
        if (collider == null || collider.isTrigger) return false;
        // Searchable objects/characters never become walls merely because their
        // prefab is nested under a room wall hierarchy.
        if (collider.GetComponentInParent<LootableObject>() != null ||
            collider.GetComponentInParent<WorldItemPickup>() != null ||
            collider.GetComponentInParent<EnemyLootInventoryRuntime>() != null ||
            collider.GetComponentInParent<SectorTreasure>() != null ||
            collider.GetComponentInParent<Enemy>() != null ||
            collider.GetComponentInParent<PlayerCharacter>() != null) return false;
        VisionOccluder setting = ResolveSetting(collider.transform);
        return setting != null && setting.isActiveAndEnabled && setting._blocksVision;
    }
}
