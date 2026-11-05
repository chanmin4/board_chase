/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player > Inventory
 * Primary responsibility: Coordinates the PlayerConsumableUseRuntime subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PlayerConsumableUseRuntime : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private PlayerInventoryRuntime _inventoryRuntime;
    [SerializeField] private PlayerQuickSlotRuntime _quickSlotRuntime;
    [SerializeField] private PlayerStatsRuntime _playerStats;
    [SerializeField] private Damageable _damageable;
    [SerializeField] private EntityPaintMarkController _markRuntime;
    [SerializeField] private PlayerAimController _aimController;
    [SerializeField] private PlayerShooterInfection _playerInfection;

    [Header("UI Events")]
    [SerializeField] private InteractionCastBarEventChannelSO _castBarChannel;
    [SerializeField] private UIOverlayRequestEventChannelSO _overlayRequestChannel;
    [SerializeField] private SystemMessageEventChannelSO _systemMessageChannel;

    [Header("Interruption")]
    [Tooltip("Minimum delay after use begins before RMB may cancel it. This prevents the context-menu click that started the item from immediately cancelling it.")]
    [SerializeField, Min(0f)] private float _rightClickCancelGuardSeconds = 0.2f;

    private Coroutine _useRoutine;
    private ConsumableItemSO _activeItem;
    private int _activeSlotIndex = -1;
    private int _activeQuickSlotIndex = -1;
    private bool _consumeFromQuickSlot;
    private float _rightClickCancelAllowedAt;

    public bool IsUsing => _useRoutine != null;

    /// <summary>
    /// Raised after an item has passed every validation and its use cast begins.
    /// Presentation listeners use this only for feedback; it never controls use logic.
    /// </summary>
    public event Action<ConsumableItemSO> UseStarted;

    /// <summary>
    /// Raised after the item was consumed and its gameplay effect resolved successfully.
    /// </summary>
    public event Action<ConsumableItemSO> UseCompleted;

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        ResolveRefs();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ResolveRefs();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        ResolveRefs();
        SubscribeInterruptions();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        UnsubscribeInterruptions();
        CancelActiveUse(null, false);
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        // RightClickEvent belongs to the UI action map, which may be disabled
        // during gameplay. Physical polling keeps cancellation reliable.
        if (_useRoutine != null &&
            Mouse.current != null &&
            Mouse.current.rightButton.wasPressedThisFrame)
        {
            HandleRightClickCancel();
        }
    }

    // Purpose: Attempts to begin use and reports whether the request succeeded.
    public bool TryBeginUse(
        int inventorySlotIndex,
        ConsumableItemSO item,
        out string message)
    {
        ResolveRefs();
        if(!GameProgressionRuntime.CanUse(item,out message))return false;

        if (_useRoutine != null)
        {
            message = "Another item is already being used.";
            return false;
        }

        if (_inventoryRuntime == null || _damageable == null)
        {
            message = "Consumable runtime references are missing.";
            return false;
        }

        if (item == null || !item.HasUseEffect)
        {
            message = "This consumable has no usable effect.";
            return false;
        }

        if (_damageable.IsDead)
        {
            message = "A dead character cannot use an item.";
            return false;
        }

        if (IsActionBlocked())
        {
            message = "Cannot use items while stunned.";
            return false;
        }

        if (!CanApplyAnyEffect(item))
        {
            message = ResolveNoApplicableEffectMessage(item);
            return false;
        }

        if (!HasExpectedStack(inventorySlotIndex, item))
        {
            message = "The consumable is no longer in that slot.";
            return false;
        }

        _consumeFromQuickSlot = false;
        if (!HasMedicalSupply(item, out message)) return false;
        _activeQuickSlotIndex = -1;
        _activeSlotIndex = inventorySlotIndex;
        _activeItem = item;
        _rightClickCancelAllowedAt =
            Time.unscaledTime + Mathf.Max(0f, _rightClickCancelGuardSeconds);

        _overlayRequestChannel?.CloseAll();
        UseStarted?.Invoke(item);
        _useRoutine = StartCoroutine(UseRoutine());
        message = $"Using {ResolveItemName(item)} ({item.UseDurationSeconds:0.#}s).";
        return true;
    }

    // Purpose: Attempts to begin use and reports whether the request succeeded.
    public bool TryBeginUse(ConsumableItemSO item, out string message)
    {
        ResolveRefs();

        if (_inventoryRuntime == null ||
            !_inventoryRuntime.TryFindInventoryItemSlot(item, out int inventorySlotIndex))
        {
            message = "The consumable is not in the inventory.";
            return false;
        }

        return TryBeginUse(inventorySlotIndex, item, out message);
    }

    // Purpose: Attempts to begin use from quick slot and reports whether the request succeeded.
    public bool TryBeginUseFromQuickSlot(
        int quickSlotIndex,
        ConsumableItemSO item,
        out string message)
    {
        ResolveRefs();
        if(!GameProgressionRuntime.CanUse(item,out message))return false;

        if (_useRoutine != null)
        {
            message = "Another item is already being used.";
            return false;
        }

        if (_quickSlotRuntime == null || _damageable == null)
        {
            message = "Consumable runtime references are missing.";
            return false;
        }

        if (item == null || !item.HasUseEffect)
        {
            message = "This consumable has no usable effect.";
            return false;
        }

        if (_damageable.IsDead)
        {
            message = "A dead character cannot use an item.";
            return false;
        }

        if (IsActionBlocked())
        {
            message = "Cannot use items while stunned.";
            return false;
        }

        if (!CanApplyAnyEffect(item))
        {
            message = ResolveNoApplicableEffectMessage(item);
            return false;
        }

        if (!HasExpectedQuickSlotStack(quickSlotIndex, item))
        {
            message = "The consumable is no longer in that quick slot.";
            return false;
        }

        _consumeFromQuickSlot = true;
        if (!HasMedicalSupply(item, out message)) return false;
        _activeQuickSlotIndex = quickSlotIndex;
        _activeSlotIndex = -1;
        _activeItem = item;
        _rightClickCancelAllowedAt =
            Time.unscaledTime + Mathf.Max(0f, _rightClickCancelGuardSeconds);

        _overlayRequestChannel?.CloseAll();
        UseStarted?.Invoke(item);
        _useRoutine = StartCoroutine(UseRoutine());
        message = $"Using {ResolveItemName(item)} ({item.UseDurationSeconds:0.#}s).";
        return true;
    }

    // Purpose: Performs the use routine workflow for this component.
    private IEnumerator UseRoutine()
    {
        ConsumableItemSO item = _activeItem;
        float duration = item != null ? item.UseDurationSeconds : 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (!CanContinueUse(item, out string interruptionMessage))
            {
                FinishRoutine(interruptionMessage, true);
                yield break;
            }

            elapsed += Time.deltaTime;
            PublishProgress(duration > 0f ? elapsed / duration : 1f, duration);
            yield return null;
        }

        CompleteUse(item);
    }

    // Purpose: Performs the complete use workflow for this component.
    private void CompleteUse(ConsumableItemSO item)
    {
        int slotIndex = _activeSlotIndex;

        if (!CanContinueUse(item, out string failureMessage))
        {
            FinishRoutine(failureMessage, true);
            return;
        }

        if (!CanApplyAnyEffect(item))
        {
            FinishRoutine(
                $"{ResolveNoApplicableEffectMessage(item)} The item was not consumed.",
                true);
            return;
        }

        bool consumed;

        if (IsInfectionMedicine(item))
        {
            consumed = TryConsumeMedicalDose(item);
        }
        else if (item.UsesDurability)
        {
            consumed = _consumeFromQuickSlot
                ? _quickSlotRuntime != null && _quickSlotRuntime.TrySpendConsumableDurability(item, out _)
                : _inventoryRuntime != null && _inventoryRuntime.TrySpendConsumableDurability(slotIndex, item, out _);
        }

        else if (_consumeFromQuickSlot)
        {
            consumed =
                _quickSlotRuntime != null &&
                _quickSlotRuntime.TryConsumeUsable(
                    item,
                    1,
                    out _);
        }
        else
        {
            consumed =
                _inventoryRuntime != null &&
                _inventoryRuntime.TryRemoveInventoryItem(
                    slotIndex,
                    1,
                    out PlayerInventoryRemovedStack removed,
                    out _) &&
                removed.HasItem &&
                removed.item == item;
        }

        if (!consumed)
        {
            FinishRoutine("The consumable could not be removed from its slot.", true);
            return;
        }

        float before = _damageable.CurrentHealth;
        _damageable.Heal(item.HealAmount);
        float healed = Mathf.Max(0f, _damageable.CurrentHealth - before);
        float markReduced = _markRuntime != null
            ? _markRuntime.ReduceMark(item.MarkRecoveryAmount)
            : 0f;
        int injuriesCured = _playerStats != null
            ? _playerStats.ApplyConsumableStatusEffect(item)
            : 0;
        if (item is InfectionCureItemSO infectionCure && _playerInfection != null)
        {
            if (infectionCure.IsSyringe) _playerInfection.TryUseInfectionSyringe(infectionCure.InfectionRecoveryAmount);
            else _playerInfection.TryStartAmpoule(infectionCure.InfectionRecoveryPerSecond,
                infectionCure.InfectionRecoveryDurationSeconds, infectionCure.ExposureGainMultiplier);
        }
        if (item.InfectionSuppressionSeconds > 0f && _playerInfection != null)
            _playerInfection.ApplySuppression(item.InfectionSuppressionSeconds);

        string result = $"{ResolveItemName(item)} used.";
        if (item is InfectionCureItemSO ampoule)
            result = ampoule.IsSyringe ? $"최대 체력 잠식 {ampoule.InfectionRecoveryAmount:0.#} 회복 (현재 HP는 별도 치료)." :
                $"앰플: 감염 유입 ×{ampoule.ExposureGainMultiplier:0.##}, 대기 후 게이지 회복 +{ampoule.InfectionRecoveryPerSecond:0.#}/초, {ampoule.InfectionRecoveryDurationSeconds:0.#}초간.";
        else if (item.InfectionSuppressionSeconds > 0f)
            result = $"억제제 투여 완료: {item.InfectionSuppressionSeconds:0}초간 억제 유지.";

        if (healed > 0.001f)
            result += $" HP +{healed:0.#}.";

        if (markReduced > 0.001f)
            result += $" Mark -{markReduced:0.#}.";

        if (injuriesCured > 0)
            result += $" Injuries cured x{injuriesCured}.";

        if (IsInfectionMedicine(item))
        {
            _quickSlotRuntime?.PublishMedicalItemChange();
            _inventoryRuntime?.PublishMedicalItemChange();
        }

        UseCompleted?.Invoke(item);
        FinishRoutine(result, true);
    }

    // Purpose: Checks whether continue use is currently permitted.
    private bool CanContinueUse(ConsumableItemSO item, out string message)
    {
        if (item == null || _damageable == null)
        {
            message = "Item use was interrupted because required references disappeared.";
            return false;
        }

        if (_damageable.IsDead)
        {
            message = "Item use was interrupted by death.";
            return false;
        }

        bool hasExpectedSource = _consumeFromQuickSlot
            ? HasExpectedQuickSlotStack(_activeQuickSlotIndex, item)
            : HasExpectedStack(_activeSlotIndex, item);

        if (!hasExpectedSource)
        {
            message = "Item use was interrupted because the inventory changed.";
            return false;
        }

        return HasMedicalSupply(item, out message);
    }

    private static bool IsInfectionMedicine(ConsumableItemSO item) =>
        item is InfectionCureItemSO || (item != null && item.InfectionSuppressionSeconds > 0f);

    private bool HasMedicalSupply(ConsumableItemSO item, out string message)
    {
        message = null;
        if (!IsInfectionMedicine(item)) return true;
        int required = item is InfectionCureItemSO ? 1 : _playerInfection != null ? _playerInfection.MedicalDose : 1;
        int available = (_inventoryRuntime != null ? _inventoryRuntime.GetInventoryItemAmount(item) : 0) +
            (_quickSlotRuntime != null ? _quickSlotRuntime.GetUsableItemAmount(item) : 0);
        message = available < required
            ? $"약물 수량이 부족합니다. 이번 스테이지에서 {required}개가 필요합니다. (보유 {available}개)" : null;
        return message == null;
    }

    private bool TryConsumeMedicalDose(ConsumableItemSO item)
    {
        if (!HasMedicalSupply(item, out _)) return false;
        int required = item is InfectionCureItemSO ? 1 : _playerInfection != null ? _playerInfection.MedicalDose : 1;
        int quickAmount = _quickSlotRuntime != null ? _quickSlotRuntime.GetUsableItemAmount(item) : 0;
        int inventoryAmount = _inventoryRuntime != null ? _inventoryRuntime.GetInventoryItemAmount(item) : 0;
        int fromQuick = _consumeFromQuickSlot ? Mathf.Min(required, quickAmount) : Mathf.Max(0, required - inventoryAmount);
        int fromInventory = required - fromQuick;
        if (fromInventory > 0 && !_inventoryRuntime.TryConsumeMedicalItemAmount(item, fromInventory)) return false;
        return fromQuick == 0 || _quickSlotRuntime.TryConsumeUsableItemAmount(item, fromQuick, out _, notify: false);
    }

    // Purpose: Checks whether this component has expected stack.
    private bool HasExpectedStack(int slotIndex, ConsumableItemSO item)
    {
        if (_inventoryRuntime == null || _inventoryRuntime.Items == null ||
            slotIndex < 0 || slotIndex >= _inventoryRuntime.Items.Count)
        {
            return false;
        }

        PlayerInventoryItemStack stack = _inventoryRuntime.Items[slotIndex];
        return stack != null && !stack.IsEmpty && stack.Item == item && stack.Amount > 0;
    }

    // Purpose: Checks whether this component has expected quick slot stack.
    private bool HasExpectedQuickSlotStack(
        int quickSlotIndex,
        ConsumableItemSO item)
    {
        return _quickSlotRuntime != null &&
               _quickSlotRuntime.TryGetSnapshot(
                   quickSlotIndex,
                   out PlayerQuickSlotSnapshot snapshot) &&
               snapshot.kind == PlayerQuickSlotBindingKind.Usable &&
               snapshot.item == item &&
               snapshot.amount > 0;
    }

    // Purpose: Handles the incoming action input request or event.
    private void HandleActionInput()
    {
        CancelActiveUse("Item use was interrupted by another action.", true);
    }

    // Purpose: Handles the incoming right click cancel request or event.
    private void HandleRightClickCancel()
    {
        if (_useRoutine == null || Time.unscaledTime < _rightClickCancelAllowedAt)
            return;

        _aimController?.BlockAimUntilRightMouseReleased();
        CancelActiveUse("Item use cancelled.", true);
    }

    // Purpose: Handles the incoming damaged request or event.
    private void HandleDamaged(Damageable _, float __)
    {
        if (_ != null && _.IsPeriodicStatusDamage) return;
        CancelActiveUse("Item use was interrupted by damage.", true);
    }

    // Purpose: Checks whether cancel active use is currently permitted.
    private void CancelActiveUse(string message, bool notify)
    {
        if (_useRoutine == null)
            return;

        Coroutine routine = _useRoutine;
        ClearActiveState();
        StopCoroutine(routine);

        if (notify && !string.IsNullOrWhiteSpace(message))
            _systemMessageChannel?.RaiseEvent(message);
    }

    // Purpose: Finishes the active routine workflow.
    private void FinishRoutine(string message, bool notify)
    {
        ClearActiveState();

        if (notify && !string.IsNullOrWhiteSpace(message))
            _systemMessageChannel?.RaiseEvent(message);
    }

    // Purpose: Clears the current active state state.
    private void ClearActiveState()
    {
        _useRoutine = null;
        _activeItem = null;
        _activeSlotIndex = -1;
        _activeQuickSlotIndex = -1;
        _consumeFromQuickSlot = false;
        _castBarChannel?.Clear();
    }

    // Purpose: Publishes the progress notification.
    private void PublishProgress(float progress01, float duration)
    {
        if (_castBarChannel == null)
            return;

        Transform actor = ResolveActor();
        _castBarChannel.RaiseEvent(new InteractionCastBarSnapshot(
            true,
            InteractionType.Consumable,
            actor,
            actor,
            progress01,
            duration));
    }

    // Purpose: Subscribes to interruptions notifications.
    private void SubscribeInterruptions()
    {
        if (_inputReader != null)
        {
            _inputReader.MeleeAttackEvent += HandleActionInput;
            _inputReader.DashEvent += HandleActionInput;
            _inputReader.InteractEvent += HandleActionInput;
            _inputReader.ReloadEvent += HandleActionInput;
            _inputReader.RightClickEvent += HandleRightClickCancel;
        }

        if (_damageable != null)
            _damageable.OnDamaged += HandleDamaged;
    }

    // Purpose: Removes the interruptions notification subscription.
    private void UnsubscribeInterruptions()
    {
        if (_inputReader != null)
        {
            _inputReader.MeleeAttackEvent -= HandleActionInput;
            _inputReader.DashEvent -= HandleActionInput;
            _inputReader.InteractEvent -= HandleActionInput;
            _inputReader.ReloadEvent -= HandleActionInput;
            _inputReader.RightClickEvent -= HandleRightClickCancel;
        }

        if (_damageable != null)
            _damageable.OnDamaged -= HandleDamaged;
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_inventoryRuntime == null)
            _inventoryRuntime = EntityComponentResolver.Resolve<PlayerInventoryRuntime>(this);

        if (_quickSlotRuntime == null)
            _quickSlotRuntime = EntityComponentResolver.Resolve<PlayerQuickSlotRuntime>(this);

        if (_playerStats == null)
            _playerStats = EntityComponentResolver.Resolve<PlayerStatsRuntime>(this);

        if (_damageable == null)
            _damageable = EntityComponentResolver.Resolve<Damageable>(this);

        if (_markRuntime == null)
            _markRuntime = EntityComponentResolver.Resolve<EntityPaintMarkController>(this);

        if (_aimController == null)
            _aimController = EntityComponentResolver.Resolve<PlayerAimController>(this);

        if (_playerInfection == null)
            _playerInfection = EntityComponentResolver.Resolve<PlayerShooterInfection>(this);

    }

    // Purpose: Checks whether apply any effect is currently permitted.
    private bool CanApplyAnyEffect(ConsumableItemSO item)
    {
        if (item == null)
            return false;
        if (item is InfectionCureItemSO medicine)
            return _playerInfection != null && !_playerInfection.IsDead &&
                (medicine.IsSyringe ? _playerInfection.CurrentInfection > .001f :
                    GameProgressionRuntime.InRaid && !_playerInfection.IsAmpouleActive);
        if (item.InfectionSuppressionSeconds > 0f)
            return _playerInfection != null && GameProgressionRuntime.InRaid;

        bool canHeal =
            item.HealAmount > 0f &&
            _damageable != null &&
            _damageable.CurrentHealth < _damageable.MaxHealth - 0.001f;

        bool canReduceMark =
            item.MarkRecoveryAmount > 0f &&
            _markRuntime != null &&
            _markRuntime.CurrentMarkAmount > 0.001f;

        bool canApplyStatus = item.AppliedStatusEffect != null;
        bool canCureInjuries = item.CuresInjuries &&
                               _playerStats != null &&
                               _playerStats.StatusEffects != null &&
                               _playerStats.StatusEffects.HasAny;
        bool canReduceInfection = item is InfectionCureItemSO infectionCure &&
                                  infectionCure.InfectionRecoveryAmount > 0f &&
                                  _playerInfection != null &&
                                  _playerInfection.CurrentInfection > 0.001f;

        bool canTreatBleeding = item.BleedingStacksRemoved > 0 && _playerStats?.StatusEffects?.BleedingStacks > 0;
        return canHeal || canReduceMark || canApplyStatus || canCureInjuries || canReduceInfection || canTreatBleeding;
    }

    // Purpose: Checks whether action blocked is currently true.
    private bool IsActionBlocked()
    {
        var stamina = EntityComponentResolver.Resolve<ShooterStaminaRuntime>(this);
        if (stamina != null && stamina.IsSprinting) return true;
        return _playerStats != null &&
               _playerStats.StatusEffects != null &&
               _playerStats.StatusEffects.BlocksActions;
    }

    // Purpose: Resolves the no applicable effect message needed by this component.
    private string ResolveNoApplicableEffectMessage(ConsumableItemSO item)
    {
        bool restoresHealth = item != null && item.HealAmount > 0f;
        bool restoresMark = item != null && item.MarkRecoveryAmount > 0f;

        if (restoresHealth && restoresMark)
            return "Health and Mark are already fully recovered.";

        if (restoresMark)
            return "Mark is already fully recovered.";

        if (item is InfectionCureItemSO medicine)
            return medicine.IsSyringe ? "회복할 최대 체력 잠식이 없습니다." :
                !GameProgressionRuntime.InRaid ? "앰플은 레이드에서 사용합니다." : "이미 감염 방호 앰플이 작용 중입니다.";
        if (item != null && item.InfectionSuppressionSeconds > 0f)
            return "안전가옥에서는 투여 장치가 억제 상태를 유지합니다.";

        return "Health is already full.";
    }

    // Purpose: Resolves the actor needed by this component.
    private Transform ResolveActor()
    {
        Transform root = EntityComponentResolver.ResolveEntityRoot(this);
        return root != null ? root : transform;
    }

    // Purpose: Resolves the item name needed by this component.
    private static string ResolveItemName(ItemSO item)
    {
        return item != null ? item.name : "Consumable";
    }
}
