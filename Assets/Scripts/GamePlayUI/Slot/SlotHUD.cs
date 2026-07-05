// SlotHUD.cs
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class SlotHUD : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private InputActionReference[] _slotKeyActions;
    [SerializeField, Min(0)] private int _ammoSlotInputOffset = 2;

    [Header("Runtime Ready")]
    [SerializeField] private PlayerInventoryRuntimeReadyEventChannelSO _inventoryRuntimeReadyChannel;
    [SerializeField] private PlayerStatsRuntimeReadyEventChannelSO _statsRuntimeReadyChannel;

    [Header("Ammo Events")]
    [SerializeField] private WeaponAmmoLoadoutEventChannelSO _weaponAmmoLoadoutEventChannel;
    [SerializeField] private VoidEventChannelSO _requestWeaponAmmoLoadoutSnapshotChannel;
    [SerializeField] private WeaponAmmoSlotSwapRequestEventChannelSO _slotSwapRequestChannel;
    [SerializeField] private IntEventChannelSO _slotSelectRequestChannel;

    [Header("Stats")]
    [SerializeField] private PlayerStatsChangedEventChannelSO _statsChangedChannel;



    [Header("Slot UI")]
    [SerializeField] private ArmorSlotUI _armorSlot;
    [SerializeField] private WeaponSlotUI _weaponSlot;
    [SerializeField] private AmmoSlotUI[] _ammoSlots;

    [Header("Message")]
    [SerializeField] private SystemMessageEventChannelSO _systemMessageChannel;
    private PlayerInventoryRuntime _inventoryRuntime;
    private PlayerStatsRuntime _statsRuntime;
    private EntityEquipmentRuntime _equipmentRuntime;

    private void Awake()
    {
        ResolveRefs();
        InitializeSlots();
    }

    private void OnEnable()
    {
        ResolveRefs();
        InitializeSlots();
        BindInputReader();
        SubscribeRuntimeEvents();

        if (_requestWeaponAmmoLoadoutSnapshotChannel != null)
            _requestWeaponAmmoLoadoutSnapshotChannel.RaiseEvent();

        if (_weaponAmmoLoadoutEventChannel != null &&
            _weaponAmmoLoadoutEventChannel.Current.slots != null)
        {
            HandleWeaponAmmoLoadoutChanged(_weaponAmmoLoadoutEventChannel.Current);
        }

        RefreshEquipmentSlots();
    }

    private void OnDisable()
    {
        UnbindInputReader();
        UnsubscribeRuntimeEvents();
    }

    public void RequestSelectAmmoSlot(int ammoSlotIndex)
    {
        if (_slotSelectRequestChannel != null)
            _slotSelectRequestChannel.RaiseEvent(ammoSlotIndex);
    }

    public void RequestSwapAmmoSlots(int fromAmmoSlotIndex, int toAmmoSlotIndex)
    {
        if (_slotSwapRequestChannel == null)
            return;

        _slotSwapRequestChannel.RaiseEvent(
            new WeaponAmmoSlotSwapRequest(fromAmmoSlotIndex, toAmmoSlotIndex));
    }

    public void RequestEquipInventoryBulletToAmmoSlot(int inventoryIndex, int ammoSlotIndex)
    {
        if (_inventoryRuntime == null)
            return;

        if (_inventoryRuntime.TryEquipInventoryBulletToAmmoSlot(
                inventoryIndex,
                ammoSlotIndex,
                out string message))
        {
            RaiseMessage(message);
        }
        else
        {
            RaiseMessage(message);
        }
    }

    public void RequestEquipEnemyInventoryAmmoToAmmoSlot(
        EnemyInventoryUI enemyInventory,
        int enemyInventorySlotIndex,
        int ammoSlotIndex)
    {
        if (enemyInventory == null)
            return;

        enemyInventory.TryTakeAmmoToPlayerAmmoSlot(
            enemyInventorySlotIndex,
            ammoSlotIndex);
    }

    public void RequestEquipInventoryItemToEquipmentSlot(
        int inventoryIndex,
        PlayerInventoryEquipmentSlotKind slotKind)
    {
        if (_inventoryRuntime == null)
            return;

        if (_inventoryRuntime.TryEquipInventoryItemToSlot(
                inventoryIndex,
                slotKind,
                out string message))
        {
            RaiseMessage(message);
        }
        else
        {
            RaiseMessage(message);
        }
    }

    public void RequestUnequipEquipmentSlot(PlayerInventoryEquipmentSlotKind slotKind)
    {
        if (_inventoryRuntime == null)
            return;

        if (_inventoryRuntime.TryUnequipToInventory(slotKind, out string message))
            RaiseMessage(message);
        else
            RaiseMessage(message);
    }

    private void ResolveRefs()
    {
        if (_inventoryRuntime == null)
            _inventoryRuntime = FindAnyObjectByType<PlayerInventoryRuntime>();

        if (_statsRuntime == null)
            _statsRuntime = FindAnyObjectByType<PlayerStatsRuntime>();

        if (_equipmentRuntime == null)
            _equipmentRuntime = FindAnyObjectByType<EntityEquipmentRuntime>();
    }

    private void InitializeSlots()
    {
        if (_armorSlot != null)
            _armorSlot.Initialize(this);

        if (_weaponSlot != null)
            _weaponSlot.Initialize(this);

        if (_ammoSlots == null)
            return;

        for (int i = 0; i < _ammoSlots.Length; i++)
        {
            if (_ammoSlots[i] == null)
                continue;

            _ammoSlots[i].Initialize(this);
        }
    }

    private void SubscribeRuntimeEvents()
    {
        if (_weaponAmmoLoadoutEventChannel != null)
            _weaponAmmoLoadoutEventChannel.OnEventRaised += HandleWeaponAmmoLoadoutChanged;

        if (_inventoryRuntimeReadyChannel != null)
        {
            _inventoryRuntimeReadyChannel.OnEventRaised += HandleInventoryRuntimeReady;

            if (_inventoryRuntimeReadyChannel.HasCurrent)
                HandleInventoryRuntimeReady(_inventoryRuntimeReadyChannel.Current);
        }

        if (_statsRuntimeReadyChannel != null)
        {
            _statsRuntimeReadyChannel.OnEventRaised += HandleStatsRuntimeReady;

            if (_statsRuntimeReadyChannel.HasCurrent)
                HandleStatsRuntimeReady(_statsRuntimeReadyChannel.Current);
        }

        if (_statsChangedChannel != null)
            _statsChangedChannel.OnEventRaised += HandleStatsChanged;

        if (_inventoryRuntime != null)
            _inventoryRuntime.OnChanged += RefreshEquipmentSlots;

        if (_equipmentRuntime != null)
            _equipmentRuntime.OnEquipmentChanged += RefreshEquipmentSlots;
    }

    private void UnsubscribeRuntimeEvents()
    {
        if (_weaponAmmoLoadoutEventChannel != null)
            _weaponAmmoLoadoutEventChannel.OnEventRaised -= HandleWeaponAmmoLoadoutChanged;

        if (_inventoryRuntimeReadyChannel != null)
            _inventoryRuntimeReadyChannel.OnEventRaised -= HandleInventoryRuntimeReady;

        if (_statsRuntimeReadyChannel != null)
            _statsRuntimeReadyChannel.OnEventRaised -= HandleStatsRuntimeReady;

        if (_statsChangedChannel != null)
            _statsChangedChannel.OnEventRaised -= HandleStatsChanged;

        if (_inventoryRuntime != null)
            _inventoryRuntime.OnChanged -= RefreshEquipmentSlots;

        if (_equipmentRuntime != null)
            _equipmentRuntime.OnEquipmentChanged -= RefreshEquipmentSlots;
    }

    private void HandleInventoryRuntimeReady(PlayerInventoryRuntime runtime)
    {
        if (_inventoryRuntime == runtime)
        {
            RefreshEquipmentSlots();
            return;
        }

        if (_inventoryRuntime != null)
            _inventoryRuntime.OnChanged -= RefreshEquipmentSlots;

        if (_equipmentRuntime != null)
            _equipmentRuntime.OnEquipmentChanged -= RefreshEquipmentSlots;

        _inventoryRuntime = runtime;
        _equipmentRuntime = runtime != null
            ? runtime.GetComponent<EntityEquipmentRuntime>()
            : null;

        if (_inventoryRuntime != null)
            _inventoryRuntime.OnChanged += RefreshEquipmentSlots;

        if (_equipmentRuntime != null)
            _equipmentRuntime.OnEquipmentChanged += RefreshEquipmentSlots;

        RefreshEquipmentSlots();
    }

    private void HandleStatsRuntimeReady(PlayerStatsRuntime runtime)
    {
        _statsRuntime = runtime;
        RefreshEquipmentSlots();
    }

    private void HandleStatsChanged(PlayerStatsSnapshot snapshot)
    {
        RefreshEquipmentSlots();
    }

    private void RefreshEquipmentSlots()
    {
        WeaponSO weapon = _inventoryRuntime != null
            ? _inventoryRuntime.EquippedWeapon
            : null;

        ArmorItemSO armor = _inventoryRuntime != null
            ? _inventoryRuntime.EquippedArmor
            : null;

        if (_weaponSlot != null)
            _weaponSlot.Bind(weapon);

        if (_armorSlot != null)
        {
            float currentDurability = _equipmentRuntime != null
                ? _equipmentRuntime.CurrentArmorDurability
                : 0f;

            float maxDurability = _equipmentRuntime != null
                ? _equipmentRuntime.MaxArmorDurability
                : armor != null ? armor.MaxDurability : 0f;

            int finalArmorClass =
                Mathf.Max(0, _statsRuntime != null ? _statsRuntime.ArmorClass : 0) +
                Mathf.Max(0, _equipmentRuntime != null ? _equipmentRuntime.ArmorClass : armor != null ? armor.ArmorClass : 0);

            _armorSlot.Bind(
                armor,
                currentDurability,
                maxDurability,
                finalArmorClass);
        }
    }

    private void HandleWeaponAmmoLoadoutChanged(WeaponAmmoLoadoutSnapshot snapshot)
    {
        if (_ammoSlots == null || snapshot.slots == null)
            return;

        int count = Mathf.Min(_ammoSlots.Length, snapshot.slots.Length);

        for (int i = 0; i < count; i++)
        {
            if (_ammoSlots[i] == null)
                continue;

            _ammoSlots[i].Bind(snapshot.slots[i], GetAmmoSlotKeyLabel(i));
        }

        for (int i = count; i < _ammoSlots.Length; i++)
        {
            if (_ammoSlots[i] == null)
                continue;

            _ammoSlots[i].Clear(i, GetAmmoSlotKeyLabel(i));
        }
    }

    private void BindInputReader()
    {
        if (_inputReader == null)
            return;

        _inputReader.Slot1Event += SelectInputSlot1;
        _inputReader.Slot2Event += SelectInputSlot2;
        _inputReader.Slot3Event += SelectInputSlot3;
        _inputReader.Slot4Event += SelectInputSlot4;
        _inputReader.Slot5Event += SelectInputSlot5;
    }

    private void UnbindInputReader()
    {
        if (_inputReader == null)
            return;

        _inputReader.Slot1Event -= SelectInputSlot1;
        _inputReader.Slot2Event -= SelectInputSlot2;
        _inputReader.Slot3Event -= SelectInputSlot3;
        _inputReader.Slot4Event -= SelectInputSlot4;
        _inputReader.Slot5Event -= SelectInputSlot5;
    }

    private void SelectInputSlot1() => TrySelectAmmoSlotFromGlobalSlot(0);
    private void SelectInputSlot2() => TrySelectAmmoSlotFromGlobalSlot(1);
    private void SelectInputSlot3() => TrySelectAmmoSlotFromGlobalSlot(2);
    private void SelectInputSlot4() => TrySelectAmmoSlotFromGlobalSlot(3);
    private void SelectInputSlot5() => TrySelectAmmoSlotFromGlobalSlot(4);

    private void TrySelectAmmoSlotFromGlobalSlot(int globalSlotIndex)
    {
        int ammoSlotIndex = globalSlotIndex - Mathf.Max(0, _ammoSlotInputOffset);

        if (_ammoSlots == null ||
            ammoSlotIndex < 0 ||
            ammoSlotIndex >= _ammoSlots.Length)
        {
            return;
        }

        RequestSelectAmmoSlot(ammoSlotIndex);
    }

    private string GetAmmoSlotKeyLabel(int ammoSlotIndex)
    {
        int globalSlotIndex = ammoSlotIndex + Mathf.Max(0, _ammoSlotInputOffset);
        InputAction action = null;

        if (_slotKeyActions != null &&
            globalSlotIndex >= 0 &&
            globalSlotIndex < _slotKeyActions.Length &&
            _slotKeyActions[globalSlotIndex] != null)
        {
            action = _slotKeyActions[globalSlotIndex].action;
        }

        if (action != null && action.bindings.Count > 0)
        {
            string bindingText = action.GetBindingDisplayString(
                0,
                InputBinding.DisplayStringOptions.DontIncludeInteractions);

            if (!string.IsNullOrWhiteSpace(bindingText))
                return bindingText;
        }

        return (globalSlotIndex + 1).ToString();
    }

    private void RaiseMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (_systemMessageChannel != null)
            _systemMessageChannel.RaiseEvent(message);
        else
            Debug.Log($"[SlotHUD] {message}", this);
    }
    
}
