using UnityEngine;

/// <summary>Controls only a pre-authored enclosure. Never creates walls, panels or placement transforms at runtime.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Board Chase/Sector/내부 잠금 전투 구역")]
public sealed class SectorLockedCombatArea : MonoBehaviour
{
    [Header("프리팹에 미리 배치한 오브젝트")]
    [SerializeField] private LootKeyTier _keyTier = LootKeyTier.Normal;
    [Tooltip("내려가는 벽 외형만 지정합니다. 고정 차단 판정/패널/바닥은 이 루트 밖에 둡니다.")]
    [SerializeField] private Transform _barrierRoot;
    [Tooltip("제자리에서 시야/이동/총알/NavMesh를 막는 판정. 외형이 완전히 내려간 순간에만 꺼집니다.")]
    [SerializeField] private GameObject _blockingRoot;
    [SerializeField] private Transform _perimeterEntry;
    [Header("하강 연출")]
    [Min(.1f)] [SerializeField] private float _loweringSeconds = .8f;
    [Min(1f)] [SerializeField] private float _lowerDistance = 14f;
    [Header("작성한 내부 범위 · 스폰 검사에만 사용, 자동 재배치 없음")]
    [Range(.25f, .96f)] [SerializeField] private float _interiorRatio = .9f;
    [Min(.1f)] [SerializeField] private float _spawnInset = 3.2f;

    private SectorRuntime _sector;
    private RaidRuntime _raid;
    private StageRoomNode _room;
    private Vector3 _closedPosition;
    private float _elapsed;
    private bool _lowering;

    public LootKeyTier KeyTier => _room != null ? _room.internalCombatKeyTier : _keyTier;
    public LootKeyTier AuthoredKeyTier => _keyTier;
    public bool IsBound => _room != null;
    public bool IsLocked => _room == null || !_room.internalCombatUnlocked;
    public bool AllowsPerimeterTravel => IsBound && IsLocked;
    public bool CanCompleteEncounter => IsBound && !IsLocked && !_lowering;
    public Transform BarrierRoot => _barrierRoot;
    public bool IsLowering => _lowering;
    public SectorRuntime Sector => _sector != null ? _sector : GetComponentInParent<SectorRuntime>();
    internal StageRoomNode Room => _room;

    public Vector3 PerimeterEntryPosition
    {
        get
        {
            if (_perimeterEntry != null) return _perimeterEntry.position;
            Bounds b = Sector.GetWorldBounds();
            return new Vector3(b.center.x, Sector.transform.position.y,
                Mathf.Max(b.min.z + 2f, b.center.z - b.size.z * _interiorRatio * .5f - 5f));
        }
    }

    public static SectorLockedCombatArea Find(SectorRuntime sector) =>
        sector != null ? sector.GetComponentInChildren<SectorLockedCombatArea>(true) : null;

    public bool HasAuthoredLayout(out string reason)
    {
        reason = string.Empty;
        var sector = Sector;
        if (sector == null || _barrierRoot == null || _blockingRoot == null)
        { reason = "사전 배치한 벽 외형 루트와 고정 차단 판정 루트가 필요합니다. 실행 중 생성하지 않습니다."; return false; }
        if (_barrierRoot == transform || _blockingRoot.transform == transform ||
            !_barrierRoot.IsChildOf(sector.transform) || !_blockingRoot.transform.IsChildOf(sector.transform) ||
            transform.IsChildOf(_barrierRoot) || transform.IsChildOf(_blockingRoot.transform) ||
            _blockingRoot.transform.IsChildOf(_barrierRoot) || _barrierRoot.IsChildOf(_blockingRoot.transform))
        { reason = "외형/고정 판정은 같은 섹터의 별도 루트여야 하며 서로 포함할 수 없습니다."; return false; }
        var vision = _blockingRoot.GetComponent<VisionOccluder>();
        if (vision == null || !vision.enabled || !vision.BlocksVision || !vision.OverridesChildren)
        { reason = "고정 차단 판정 루트에 시야 차단 ON/하위 우선 설정을 미리 넣어야 합니다."; return false; }
        int solids = 0;
        foreach (Collider collider in _blockingRoot.GetComponentsInChildren<Collider>(true))
            if (collider.enabled && !collider.isTrigger) solids++;
        if (solids < 4)
        { reason = "고정 차단 판정에 네 면의 일반 Collider를 미리 배치해야 합니다."; return false; }
        var panels = GetComponentsInChildren<SectorLockPanel>(true);
        if (panels.Length != 4)
        { reason = "네 방향 패널을 미리 배치해야 합니다."; return false; }
        foreach (var panel in panels)
        {
            if (panel.Area != this || panel.transform.IsChildOf(_barrierRoot) ||
                panel.transform.IsChildOf(_blockingRoot.transform))
            { reason = "패널은 외형/차단 루트 밖에 두고 같은 Area를 참조해야 합니다."; return false; }
            bool hasTrigger = false;
            foreach (Collider collider in panel.GetComponentsInChildren<Collider>(true))
                if (collider.enabled && collider.isTrigger) hasTrigger = true;
            if (!hasTrigger)
            { reason = "각 패널에 근접 상호작용 Trigger가 필요합니다."; return false; }
        }
        return true;
    }

    public bool Bind(RaidRuntime raid, SectorRuntime sector, StageRoomNode room)
    {
        _sector = sector;
        if (!HasAuthoredLayout(out string reason))
        {
            Debug.LogError("[Internal combat lock] " + name + ": " + reason, this);
            return false;
        }
        _raid = raid; _room = room;
        room.hasInternalCombatLock = true;
        if (room.isCleared) room.internalCombatUnlocked = true;
        _closedPosition = _barrierRoot.localPosition;
        _lowering = false;
        _barrierRoot.gameObject.SetActive(true);
        _blockingRoot.SetActive(true);
        if (room.internalCombatUnlocked)
        {
            _barrierRoot.localPosition = _closedPosition + Vector3.down * _lowerDistance;
            FinishOpening();
        }
        return true;
    }

    public bool ContainsSpawnPoint(Vector3 position)
    {
        Bounds b = Sector.GetWorldBounds();
        return Mathf.Abs(position.x - b.center.x) < b.size.x * _interiorRatio * .5f - _spawnInset &&
               Mathf.Abs(position.z - b.center.z) < b.size.z * _interiorRatio * .5f - _spawnInset;
    }

    public bool CanUsePanel(SectorLockPanel panel, PlayerInventoryRuntime inventory)
    {
        if (!isActiveAndEnabled || panel == null || panel.Area != this || !IsLocked ||
            _raid == null || !_raid.IsInSector(_sector) || inventory == null ||
            inventory.GetComponent<Damageable>()?.IsDead == true) return false;
        var interactions = EntityComponentResolver.Resolve<InteractionManager>(inventory.transform);
        return interactions != null && interactions.CanInteractWithTarget(panel.gameObject);
    }

    public bool TryOpenPanel(SectorLockPanel panel, Component actor)
    {
        var inventory = actor != null ? EntityComponentResolver.Resolve<PlayerInventoryRuntime>(actor.transform) : null;
        if (!CanUsePanel(panel, inventory)) return false;
        return ShopPurchaseTradePanel.TryOpenKeySubmission(panel, KeyTier, inventory,
            (int slot, KeyItemSO key, out string message) =>
                _raid.TryUnlockInternalCombatArea(this, panel, inventory, slot, key, out message),
            () => this != null && CanUsePanel(panel, inventory), "잠금 해제", "내부 전투 구역 · 열쇠 확인",
            "확인: 열쇠 1회 사용 → 벽 하강·이동 포탈 잠금\n벽이 완전히 내려가면 시야/이동/총알 차단이 함께 해제됩니다. 취소: 소모 없음");
    }

    internal void BeginOpening()
    {
        _elapsed = 0f; _lowering = true;
        foreach (var panel in GetComponentsInChildren<SectorLockPanel>(true)) panel.DisableInteraction();
        // Do not move/disable the fixed blockers during the visual animation.
    }

    private void FinishOpening()
    {
        _barrierRoot.gameObject.SetActive(false);
        _blockingRoot.SetActive(false); // Sight, physics and carving release together, only at the endpoint.
        foreach (var panel in GetComponentsInChildren<SectorLockPanel>(true)) panel.DisableInteraction();
        _lowering = false;
    }

    private void Update()
    {
        if (!_lowering) return;
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / Mathf.Max(.1f, _loweringSeconds));
        _barrierRoot.localPosition = _closedPosition +
            Vector3.down * (_lowerDistance * Mathf.SmoothStep(0, 1, t));
        if (t >= 1f) FinishOpening();
    }
}
