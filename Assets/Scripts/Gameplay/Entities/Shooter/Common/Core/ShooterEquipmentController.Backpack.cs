using UnityEngine;

/// <summary>착용 백팩의 시각화만 담당한다. 실제 장착/탈착/내용물은 PlayerInventoryRuntime이 소유한다.</summary>
public partial class ShooterEquipmentController
{
    [Header("Backpack Equipment Visual")]
    [Tooltip("비워 두면 공통 Visual Rig의 BackAccessorySocket을 사용합니다.")]
    [SerializeField] private Transform _backpackVisualRoot;
    [SerializeField] private bool _renderBackpackVisual = true;
    private GameObject _backpackVisualInstance;
    private ShooterVisualOccupancy _backpackVisualOccupancy;

    public BackpackItemSO CurrentBackpack => _playerInventory != null ? _playerInventory.EquippedBackpack : null;
    public GameObject BackpackVisualInstance => _backpackVisualInstance;

    public void SyncBackpackVisual() => RebuildBackpackVisual(CurrentBackpack);
    private void HandleBackpackChanged(BackpackItemSO item) => RebuildBackpackVisual(item);

    private Transform ResolveBackpackVisualRoot()
    {
        if (_backpackVisualRoot != null) return _backpackVisualRoot;
        var rig = GetComponentInChildren<ShooterVisualRig>(true);
        return (rig != null ? rig.ResolveSocket(ShooterVisualSocketId.Back) : null) ?? FindVisualTransformByName("BackAccessorySocket");
    }

    private void RebuildBackpackVisual(BackpackItemSO item)
    {
        if (_backpackVisualInstance != null)
        {
            // Destroy is deferred in play mode; hide the old model immediately during a swap/drop.
            _backpackVisualInstance.SetActive(false);
            if (Application.isPlaying) Destroy(_backpackVisualInstance);
            else DestroyImmediate(_backpackVisualInstance);
        }
        _backpackVisualInstance = null;
        _backpackVisualOccupancy = ShooterVisualOccupancy.None;
        if (!_renderBackpackVisual || item == null || item.EquippedVisualPrefab == null)
        { SyncVisualOccupancy(); RefreshVisionRendererRegistration(); return; }
        var mount = ResolveBackpackVisualRoot();
        if (mount == null)
        {
            if (_logMissingRefs) Debug.LogWarning("[ShooterEquipmentController] 백팩 등 장착점이 없습니다. BackAccessorySocket 연결을 확인하세요.", this);
            SyncVisualOccupancy(); RefreshVisionRendererRegistration(); return;
        }
        _backpackVisualInstance = Instantiate(item.EquippedVisualPrefab, mount, false);
        _backpackVisualInstance.name = "Equipped_Backpack_" + item.name;
        item.ApplyEquippedVisualTransform(_backpackVisualInstance.transform);
        _backpackVisualOccupancy = item.VisualCoverage;
        RegisterRuntimeVisualWithVision(_backpackVisualInstance.transform);
        SyncVisualOccupancy(); RefreshBackpackVisualVisibility();
    }

    private void RefreshBackpackVisualVisibility()
    { if (_backpackVisualInstance != null) _backpackVisualInstance.SetActive(_visible); }
}
