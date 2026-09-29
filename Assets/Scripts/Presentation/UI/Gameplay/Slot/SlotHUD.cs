/*
 * Role hierarchy: Presentation > UI > Gameplay > Slot
 * Primary responsibility: Presents SlotHUD state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
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

    [Header("Ammo Refresh")]
    [Tooltip("The ammo runtime still publishes this snapshot; the HUD uses it only to refresh generic quick-slot bindings.")]
    [SerializeField] private WeaponAmmoLoadoutEventChannelSO _weaponAmmoLoadoutEventChannel;
    [SerializeField] private VoidEventChannelSO _requestWeaponAmmoLoadoutSnapshotChannel;
    [Header("Stats")]
    [SerializeField] private PlayerStatsChangedEventChannelSO _statsChangedChannel;

    [Header("Slot UI")]
    [SerializeField] private ArmorSlotUI _armorSlot;
    [Tooltip("Shared quick slots 1-2. Existing weapon-style visuals are reused.")]
    [SerializeField] private WeaponSlotUI[] _weaponSlots;
    [Tooltip("Generic quick slots 3-5. Ammo-specific text is shown only while the binding targets ammunition.")]
    [SerializeField] private AmmoSlotUI[] _ammoSlots;

    [Header("Message")]
    [SerializeField] private SystemMessageEventChannelSO _systemMessageChannel;

    private PlayerInventoryRuntime _inventoryRuntime;
    private PlayerStatsRuntime _statsRuntime;
    private EntityEquipmentRuntime _equipmentRuntime;
    private PlayerQuickSlotRuntime _quickSlots;
    private PlayerBulletLoadoutRuntime _bulletLoadout;
    private AmmoCycleListHUD _ammoCycleListHud;
    private readonly List<CompatibleAmmoDisplayEntry> _compatibleAmmoEntries = new();
    private bool _ammoCycleListExpanded;
    private BulletSO _highlightedAmmo;
    private int _lastAmmoCycleInputFrame = -1;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ResolveRefs();
        EnsureAmmoCycleListHUD();
        InitializeSlots();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        ResolveRefs();
        EnsureAmmoCycleListHUD();
        InitializeSlots();
        BindInputReader();
        SubscribeRuntimeEvents();

        if (_requestWeaponAmmoLoadoutSnapshotChannel != null)
            _requestWeaponAmmoLoadoutSnapshotChannel.RaiseEvent();

        RefreshAllSlots();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        UnbindInputReader();
        UnsubscribeRuntimeEvents();
        _ammoCycleListExpanded = false;
        _highlightedAmmo = null;
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        // Old prefabs without an InputReader keep their default T fallback.
        // Bound InputReader instances own this action so rebinding does not
        // cause a second direct-key trigger.
        if (_inputReader == null &&
            Keyboard.current != null &&
            Keyboard.current.tKey.wasPressedThisFrame)
        {
            HandleAmmoSelectionInput();
        }

        if (!_ammoCycleListExpanded)
            return;

        int navigationDirection = 0;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.upArrowKey.wasPressedThisFrame)
                navigationDirection = -1;
            else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
                navigationDirection = 1;
        }

        if (navigationDirection == 0 && Mouse.current != null)
        {
            float scrollY = Mouse.current.scroll.ReadValue().y;

            if (scrollY > 0.01f)
                navigationDirection = -1;
            else if (scrollY < -0.01f)
                navigationDirection = 1;
        }

        if (navigationDirection != 0)
            MoveAmmoSelection(navigationDirection);
    }

    // Purpose: Requests activate quick slot from the owning service.
    public void RequestActivateQuickSlot(int quickSlotIndex)
    {
        CloseAmmoSelection();

        if (_quickSlots == null)
        {
            RaiseMessage("Player quick-slot runtime is missing.");
            return;
        }

        if (!_quickSlots.Activate(quickSlotIndex, out string message))
            RaiseMessage(message);
        else
            RefreshAllSlots();
    }

    // Purpose: Requests activate weapon slot from the owning service.
    public void RequestActivateWeaponSlot(int weaponSlotIndex)
    {
        CloseAmmoSelection();

        if (_equipmentRuntime == null)
        {
            RaiseMessage("Player equipment runtime is missing.");
            return;
        }

        _quickSlots?.DeactivateActiveUsable();
        _equipmentRuntime.SetActiveWeaponSlot(weaponSlotIndex);
        RefreshAllSlots();
    }

    // Purpose: Requests handle weapon slot drop from the owning service.
    public void RequestHandleWeaponSlotDrop(int targetWeaponSlotIndex)
    {
        if (_inventoryRuntime == null ||
            _equipmentRuntime == null ||
            !PlayerInventoryDragContext.HasPayload)
        {
            return;
        }

        string message = string.Empty;
        bool success = false;

        switch (PlayerInventoryDragContext.SourceKind)
        {
            case PlayerInventoryDragSourceKind.Inventory:
                success = UIAudio.ReportResult(_inventoryRuntime.TryEquipInventoryItemToSlot(
                    PlayerInventoryDragContext.InventoryIndex,
                    PlayerInventoryEquipmentSlotKind.Weapon,
                    targetWeaponSlotIndex,
                    out message), UIAudioCueType.Equip);
                break;

            case PlayerInventoryDragSourceKind.Equipment
                when PlayerInventoryDragContext.EquipmentSlotKind ==
                     PlayerInventoryEquipmentSlotKind.Weapon:
                success = UIAudio.ReportResult(_equipmentRuntime.TrySwapWeaponSlots(
                    PlayerInventoryDragContext.EquipmentSlotIndex,
                    targetWeaponSlotIndex), UIAudioCueType.InventoryMove);
                message = success
                    ? "Weapon slots swapped."
                    : "Weapon slots could not be swapped.";
                break;

            case PlayerInventoryDragSourceKind.EnemyInventory:
                success = PlayerInventoryDragContext.EnemyInventoryOwner != null &&
                          PlayerInventoryDragContext.EnemyInventoryOwner
                              .TryTakeSlotToPlayerEquipmentSlot(
                                  PlayerInventoryDragContext.EnemyInventorySlotIndex,
                                  PlayerInventoryEquipmentSlotKind.Weapon,
                                  targetWeaponSlotIndex);
                break;

            default:
                message = "Only weapons can be equipped in weapon slots.";
                break;
        }

        if (!string.IsNullOrWhiteSpace(message))
            RaiseMessage(message);

        if (success)
            RefreshAllSlots();
    }

    // Purpose: Requests clear quick slot from the owning service.
    public void RequestClearQuickSlot(int quickSlotIndex)
    {
        RequestUnequipQuickSlot(quickSlotIndex);
    }

    // Purpose: Requests unequip quick slot from the owning service.
    public void RequestUnequipQuickSlot(int quickSlotIndex, int targetInventoryIndex = -1)
    {
        if (_quickSlots == null)
            return;

        UIAudio.ReportResult(_quickSlots.TryUnequipQuickSlot(
            quickSlotIndex,
            targetInventoryIndex,
            out string message), UIAudioCueType.Unequip);
        RaiseMessage(message);
    }

    // Purpose: Requests bind quick slot from drag from the owning service.
    public void RequestBindQuickSlotFromDrag(int targetQuickSlotIndex)
    {
        if (_quickSlots == null || !PlayerInventoryDragContext.HasPayload)
            return;

        bool success = false;
        string message;

        switch (PlayerInventoryDragContext.SourceKind)
        {
            case PlayerInventoryDragSourceKind.QuickSlot:
                success = UIAudio.ReportResult(_quickSlots.SwapBindings(
                    PlayerInventoryDragContext.QuickSlotIndex,
                    targetQuickSlotIndex,
                    out message), UIAudioCueType.InventoryMove);
                break;

            case PlayerInventoryDragSourceKind.Inventory:
                success = UIAudio.ReportResult(_quickSlots.TryAssignInventoryItem(
                    PlayerInventoryDragContext.InventoryIndex,
                    targetQuickSlotIndex,
                    out message), UIAudioCueType.Equip);
                break;

            case PlayerInventoryDragSourceKind.Equipment:
                message = "Only usable inventory items can be bound to quick slots.";
                break;

            case PlayerInventoryDragSourceKind.EquipmentAmmo:
                message = "Ammo is selected automatically. Press T to choose compatible ammo.";
                break;

            case PlayerInventoryDragSourceKind.EnemyInventory:
                message = "Move the item to the player inventory before binding it.";
                break;

            default:
                message = "This item cannot be bound to a quick slot.";
                break;
        }

        if (!string.IsNullOrWhiteSpace(message))
            RaiseMessage(message);

        if (success)
            RefreshQuickSlots();
    }

    // Purpose: Requests equip inventory item to equipment slot from the owning service.
    public void RequestEquipInventoryItemToEquipmentSlot(
        int inventoryIndex,
        PlayerInventoryEquipmentSlotKind slotKind)
    {
        RequestEquipInventoryItemToEquipmentSlot(inventoryIndex, slotKind, 0);
    }

    // Purpose: Requests equip inventory item to equipment slot from the owning service.
    public void RequestEquipInventoryItemToEquipmentSlot(
        int inventoryIndex,
        PlayerInventoryEquipmentSlotKind slotKind,
        int equipmentSlotIndex)
    {
        if (_inventoryRuntime == null)
            return;

        bool success;
        string message;

        if (slotKind == PlayerInventoryEquipmentSlotKind.Weapon)
        {
            success = UIAudio.ReportResult(_inventoryRuntime.TryEquipInventoryItemToSlot(
                inventoryIndex,
                slotKind,
                equipmentSlotIndex,
                out message), UIAudioCueType.Equip);
        }
        else
        {
            success = UIAudio.ReportResult(_inventoryRuntime.TryEquipInventoryItemToSlot(
                inventoryIndex,
                slotKind,
                out message), UIAudioCueType.Equip);
        }

        RaiseMessage(message);

        if (success)
            RefreshAllSlots();
    }

    // Purpose: Requests unequip equipment slot from the owning service.
    public void RequestUnequipEquipmentSlot(PlayerInventoryEquipmentSlotKind slotKind)
    {
        RequestUnequipEquipmentSlot(slotKind, 0);
    }

    // Purpose: Requests unequip equipment slot from the owning service.
    public void RequestUnequipEquipmentSlot(
        PlayerInventoryEquipmentSlotKind slotKind,
        int equipmentSlotIndex)
    {
        if (_inventoryRuntime == null)
            return;

        bool success;
        string message;

        if (slotKind == PlayerInventoryEquipmentSlotKind.Weapon)
        {
            success = UIAudio.ReportResult(_inventoryRuntime.TryUnequipToInventorySlot(
                slotKind,
                equipmentSlotIndex,
                -1,
                out message), UIAudioCueType.Unequip);
        }
        else
        {
            success = UIAudio.ReportResult(_inventoryRuntime.TryUnequipToInventory(slotKind, out message), UIAudioCueType.Unequip);
        }

        RaiseMessage(message);

        if (success)
            RefreshAllSlots();
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_inventoryRuntime == null)
            _inventoryRuntime = FindAnyObjectByType<PlayerInventoryRuntime>();

        if (_statsRuntime == null)
            _statsRuntime = FindAnyObjectByType<PlayerStatsRuntime>();

        if (_equipmentRuntime == null)
            _equipmentRuntime = FindAnyObjectByType<EntityEquipmentRuntime>();

        if (_quickSlots == null)
            _quickSlots = FindAnyObjectByType<PlayerQuickSlotRuntime>();

        if (_bulletLoadout == null)
            _bulletLoadout = FindAnyObjectByType<PlayerBulletLoadoutRuntime>();
    }

    // Purpose: Initializes slots for use by this component.
    private void InitializeSlots()
    {
        if (_armorSlot != null)
            _armorSlot.Initialize(this);

        if (_weaponSlots != null)
        {
            for (int i = 0; i < _weaponSlots.Length; i++)
                _weaponSlots[i]?.Initialize(this, i);
        }

        if (_ammoSlots == null)
            return;

        int offset = Mathf.Max(0, _ammoSlotInputOffset);

        for (int i = 0; i < _ammoSlots.Length; i++)
            _ammoSlots[i]?.Initialize(this, offset + i);
    }

    // Purpose: Subscribes to runtime events notifications.
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
            _inventoryRuntime.OnChanged += RefreshAllSlots;

        if (_equipmentRuntime != null)
            _equipmentRuntime.OnEquipmentChanged += RefreshAllSlots;

        if (_quickSlots != null)
            _quickSlots.OnChanged += RefreshQuickSlots;
    }

    // Purpose: Removes the runtime events notification subscription.
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
            _inventoryRuntime.OnChanged -= RefreshAllSlots;

        if (_equipmentRuntime != null)
            _equipmentRuntime.OnEquipmentChanged -= RefreshAllSlots;

        if (_quickSlots != null)
            _quickSlots.OnChanged -= RefreshQuickSlots;
    }

    // Purpose: Handles the incoming inventory runtime ready request or event.
    private void HandleInventoryRuntimeReady(PlayerInventoryRuntime runtime)
    {
        if (_inventoryRuntime != null)
            _inventoryRuntime.OnChanged -= RefreshAllSlots;

        if (_equipmentRuntime != null)
            _equipmentRuntime.OnEquipmentChanged -= RefreshAllSlots;

        if (_quickSlots != null)
            _quickSlots.OnChanged -= RefreshQuickSlots;

        _inventoryRuntime = runtime;
        _equipmentRuntime = runtime != null
            ? EntityComponentResolver.Resolve<EntityEquipmentRuntime>(runtime)
            : null;
        _quickSlots = runtime != null
            ? EntityComponentResolver.Resolve<PlayerQuickSlotRuntime>(runtime)
            : null;
        _bulletLoadout = runtime != null
            ? EntityComponentResolver.Resolve<PlayerBulletLoadoutRuntime>(runtime)
            : null;

        if (_inventoryRuntime != null)
            _inventoryRuntime.OnChanged += RefreshAllSlots;

        if (_equipmentRuntime != null)
            _equipmentRuntime.OnEquipmentChanged += RefreshAllSlots;

        if (_quickSlots != null)
            _quickSlots.OnChanged += RefreshQuickSlots;

        RefreshAllSlots();
    }

    // Purpose: Handles the incoming stats runtime ready request or event.
    private void HandleStatsRuntimeReady(PlayerStatsRuntime runtime)
    {
        _statsRuntime = runtime;
        RefreshAllSlots();
    }

    // Purpose: Handles the incoming stats changed request or event.
    private void HandleStatsChanged(PlayerStatsSnapshot _) => RefreshAllSlots();
    // Purpose: Handles the incoming weapon ammo loadout changed request or event.
    private void HandleWeaponAmmoLoadoutChanged(WeaponAmmoLoadoutSnapshot _)
    {
        // Reload completion updates the virtual weapon magazine after the
        // inventory-consumption event has already refreshed the HUD. Refresh
        // the active weapon slot from the published magazine snapshot as well,
        // otherwise it keeps showing 0/reserve until the next unrelated input.
        RefreshWeaponSlots();
        RefreshAmmoCycleList();
    }

    // Purpose: Refreshes the displayed or cached all slots.
    private void RefreshAllSlots()
    {
        RefreshWeaponSlots();
        RefreshUsableQuickSlots();
        RefreshArmorSlot();
        RefreshAmmoCycleList();
    }

    // Purpose: Refreshes the displayed or cached quick slots.
    private void RefreshQuickSlots()
    {
        // Weapon slots 1-2 and usable slots 3-5 share one active hand.
        // Refresh both groups whenever the quick-slot selection changes so a
        // stale weapon highlight can never remain beside an active usable.
        RefreshWeaponSlots();
        RefreshUsableQuickSlots();
    }

    // Purpose: Ensures ammo cycle list hud is ready before the workflow continues.
    private void EnsureAmmoCycleListHUD()
    {
        if (_ammoCycleListHud != null)
            return;

        _ammoCycleListHud = GetComponent<AmmoCycleListHUD>();

        if (_ammoCycleListHud == null)
            _ammoCycleListHud = gameObject.AddComponent<AmmoCycleListHUD>();
    }

    // Purpose: Refreshes the displayed or cached ammo cycle list.
    private void RefreshAmmoCycleList()
    {
        EnsureAmmoCycleListHUD();
        _compatibleAmmoEntries.Clear();

        if (_bulletLoadout != null)
            _bulletLoadout.GetCompatibleInventoryAmmoEntries(_compatibleAmmoEntries);

        RectTransform activeWeaponSlot = null;

        if (_equipmentRuntime != null && _weaponSlots != null)
        {
            int activeIndex = _equipmentRuntime.ActiveWeaponSlotIndex;

            if (activeIndex >= 0 &&
                activeIndex < _weaponSlots.Length &&
                _weaponSlots[activeIndex] != null)
            {
                activeWeaponSlot = _weaponSlots[activeIndex].transform as RectTransform;
            }
        }

        _ammoCycleListHud.Refresh(
            _compatibleAmmoEntries,
            activeWeaponSlot,
            _inputReader != null
                ? _inputReader.CycleAmmoBindingDisplayString
                : "T",
            _ammoCycleListExpanded,
            _ammoCycleListExpanded
                ? ResolveHighlightedAmmoIndex()
                : -1);
    }

    // Purpose: Refreshes the displayed or cached weapon slots.
    private void RefreshWeaponSlots()
    {
        if (_weaponSlots != null)
        {
            for (int i = 0; i < _weaponSlots.Length; i++)
            {
                if (_weaponSlots[i] == null)
                    continue;

                WeaponSO weapon = _equipmentRuntime != null
                    ? _equipmentRuntime.GetWeapon(i)
                    : null;
                bool usableSelected =
                    _quickSlots != null &&
                    _quickSlots.ActiveQuickSlotIndex >= 2;
                bool showMagazine =
                    weapon is RangedWeaponSO &&
                    !usableSelected &&
                    _equipmentRuntime != null &&
                    _equipmentRuntime.ActiveWeaponSlotIndex == i;
                bool hasMagazineAmmo = false;
                int currentMagazineAmmo = 0;
                int reserveAmmo = 0;

                if (showMagazine &&
                    _bulletLoadout != null &&
                    _bulletLoadout.TryGetActiveWeaponMagazineState(
                        out BulletSO magazineBullet,
                        out currentMagazineAmmo))
                {
                    hasMagazineAmmo = true;
                    reserveAmmo = _inventoryRuntime != null && magazineBullet != null
                        ? _inventoryRuntime.GetInventoryItemAmount(magazineBullet)
                        : 0;
                }

                _weaponSlots[i].Bind(
                    weapon,
                    _equipmentRuntime != null ? _equipmentRuntime.GetWeaponAmount(i) : 0,
                    _equipmentRuntime != null ? _equipmentRuntime.GetWeaponCurrentDurability(i) : 0f,
                    _equipmentRuntime != null ? _equipmentRuntime.GetWeaponMaxDurability(i) : 0f,
                    !usableSelected &&
                    _equipmentRuntime != null &&
                    _equipmentRuntime.ActiveWeaponSlotIndex == i,
                    GetGlobalSlotKeyLabel(i),
                    showMagazine,
                    hasMagazineAmmo,
                    currentMagazineAmmo,
                    reserveAmmo);
            }
        }
    }

    // Purpose: Refreshes the displayed or cached usable quick slots.
    private void RefreshUsableQuickSlots()
    {
        if (_ammoSlots == null)
            return;

        int offset = Mathf.Max(0, _ammoSlotInputOffset);

        for (int i = 0; i < _ammoSlots.Length; i++)
        {
            if (_ammoSlots[i] == null)
                continue;

            int quickSlotIndex = offset + i;
            PlayerQuickSlotSnapshot snapshot = GetQuickSlotSnapshot(quickSlotIndex);
            _ammoSlots[i].Bind(snapshot, GetGlobalSlotKeyLabel(quickSlotIndex));
        }
    }

    // Purpose: Returns the current quick slot snapshot.
    private PlayerQuickSlotSnapshot GetQuickSlotSnapshot(int quickSlotIndex)
    {
        return _quickSlots != null && _quickSlots.TryGetSnapshot(quickSlotIndex, out PlayerQuickSlotSnapshot snapshot)
            ? snapshot
            : new PlayerQuickSlotSnapshot(quickSlotIndex, PlayerQuickSlotBindingKind.None, null, 0, false, 0f, 0f, default);
    }

    // Purpose: Refreshes the displayed or cached armor slot.
    private void RefreshArmorSlot()
    {
        if (_armorSlot == null)
            return;

        ArmorItemSO armor = _inventoryRuntime != null ? _inventoryRuntime.EquippedArmor : null;
        float currentDurability = _equipmentRuntime != null ? _equipmentRuntime.CurrentArmorDurability : 0f;
        float maxDurability = _equipmentRuntime != null
            ? _equipmentRuntime.MaxArmorDurability
            : armor != null ? armor.MaxDurability : 0f;
        float finalArmorClass =
            Mathf.Max(0f, _statsRuntime != null ? _statsRuntime.ArmorClass : 0f) +
            Mathf.Max(0f, _equipmentRuntime != null ? _equipmentRuntime.ArmorClass : armor != null ? armor.ArmorClass : 0f);

        _armorSlot.Bind(armor, currentDurability, maxDurability, finalArmorClass);
    }

    // Purpose: Binds input reader to the active runtime or UI target.
    private void BindInputReader()
    {
        if (_inputReader == null)
            return;

        _inputReader.Slot1Event += SelectInputSlot1;
        _inputReader.Slot2Event += SelectInputSlot2;
        _inputReader.Slot3Event += SelectInputSlot3;
        _inputReader.Slot4Event += SelectInputSlot4;
        _inputReader.Slot5Event += SelectInputSlot5;
        _inputReader.CycleAmmoEvent += HandleAmmoSelectionInput;
    }

    // Purpose: Removes the input reader binding.
    private void UnbindInputReader()
    {
        if (_inputReader == null)
            return;

        _inputReader.Slot1Event -= SelectInputSlot1;
        _inputReader.Slot2Event -= SelectInputSlot2;
        _inputReader.Slot3Event -= SelectInputSlot3;
        _inputReader.Slot4Event -= SelectInputSlot4;
        _inputReader.Slot5Event -= SelectInputSlot5;
        _inputReader.CycleAmmoEvent -= HandleAmmoSelectionInput;
    }

    // Purpose: Selects input slot1 for the active workflow.
    private void SelectInputSlot1() => RequestActivateWeaponSlot(0);
    // Purpose: Selects input slot2 for the active workflow.
    private void SelectInputSlot2() => RequestActivateWeaponSlot(1);
    // Purpose: Selects input slot3 for the active workflow.
    private void SelectInputSlot3() => RequestActivateQuickSlot(2);
    // Purpose: Selects input slot4 for the active workflow.
    private void SelectInputSlot4() => RequestActivateQuickSlot(3);
    // Purpose: Selects input slot5 for the active workflow.
    private void SelectInputSlot5() => RequestActivateQuickSlot(4);

    // Purpose: Handles the incoming ammo selection input request or event.
    private void HandleAmmoSelectionInput()
    {
        if (_lastAmmoCycleInputFrame == Time.frameCount)
            return;

        _lastAmmoCycleInputFrame = Time.frameCount;
        ResolveRefs();

        if (_bulletLoadout == null)
        {
            RaiseMessage("Player ammo runtime is missing.");
            Debug.LogWarning(
                "[SlotHUD][AmmoCycle] T input was received, but PlayerBulletLoadoutRuntime could not be resolved.",
                this);
            return;
        }

        if (!_ammoCycleListExpanded)
        {
            OpenAmmoSelection();
            return;
        }

        ConfirmAmmoSelection();
    }

    // Purpose: Opens the ammo selection UI or workflow.
    private void OpenAmmoSelection()
    {
        _ammoCycleListExpanded = true;
        _highlightedAmmo = null;
        RefreshAmmoCycleList();

        if (_compatibleAmmoEntries.Count > 0)
            return;

        _ammoCycleListExpanded = false;
        RaiseMessage("No compatible ammunition is available.");
        RefreshAmmoCycleList();
    }

    // Purpose: Moves ammo selection toward its requested destination.
    private void MoveAmmoSelection(int direction)
    {
        RefreshAmmoCycleList();

        if (_compatibleAmmoEntries.Count == 0)
        {
            CloseAmmoSelection();
            return;
        }

        int currentIndex = ResolveHighlightedAmmoIndex();
        int nextIndex = currentIndex < 0
            ? 0
            : (currentIndex + direction) % _compatibleAmmoEntries.Count;

        if (nextIndex < 0)
            nextIndex += _compatibleAmmoEntries.Count;

        _highlightedAmmo = _compatibleAmmoEntries[nextIndex].Bullet;
        RefreshAmmoCycleList();
    }

    // Purpose: Commits the current ammo selection selection or change.
    private void ConfirmAmmoSelection()
    {
        RefreshAmmoCycleList();
        int highlightedIndex = ResolveHighlightedAmmoIndex();

        if (highlightedIndex < 0 ||
            highlightedIndex >= _compatibleAmmoEntries.Count)
        {
            CloseAmmoSelection();
            RaiseMessage("No compatible ammunition is selected.");
            return;
        }

        BulletSO selectedBullet = _compatibleAmmoEntries[highlightedIndex].Bullet;
        _ammoCycleListExpanded = false;
        _highlightedAmmo = null;

        bool selected = _bulletLoadout.TrySelectCompatibleInventoryAmmo(
            selectedBullet,
            out string message);
        RaiseMessage(message);
        RefreshAmmoCycleList();

        if (!selected)
        {
            Debug.LogWarning(
                $"[SlotHUD][AmmoSelection] Confirmation failed. " +
                $"bullet={(selectedBullet != null ? selectedBullet.name : "null")}, " +
                $"message={message}",
                this);
        }
    }

    // Purpose: Resolves the highlighted ammo index needed by this component.
    private int ResolveHighlightedAmmoIndex()
    {
        if (_compatibleAmmoEntries.Count == 0)
            return -1;

        if (_highlightedAmmo != null)
        {
            for (int i = 0; i < _compatibleAmmoEntries.Count; i++)
            {
                if (_compatibleAmmoEntries[i].Bullet == _highlightedAmmo)
                    return i;
            }
        }

        for (int i = 0; i < _compatibleAmmoEntries.Count; i++)
        {
            if (!_compatibleAmmoEntries[i].IsSelected)
                continue;

            _highlightedAmmo = _compatibleAmmoEntries[i].Bullet;
            return i;
        }

        _highlightedAmmo = _compatibleAmmoEntries[0].Bullet;
        return 0;
    }

    // Purpose: Closes the active ammo selection UI or workflow.
    private void CloseAmmoSelection()
    {
        if (!_ammoCycleListExpanded && _highlightedAmmo == null)
            return;

        _ammoCycleListExpanded = false;
        _highlightedAmmo = null;
        RefreshAmmoCycleList();
    }

    // Purpose: Returns the current global slot key label.
    private string GetGlobalSlotKeyLabel(int globalSlotIndex)
    {
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

    // Purpose: Publishes the message notification.
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
