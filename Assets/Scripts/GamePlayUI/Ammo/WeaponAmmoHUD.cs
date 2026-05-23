using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 화면 하단 탄 슬롯 HUD.
/// PlayerBulletLoadoutRuntime이 방송한 WeaponAmmoLoadoutSnapshot을 받아 1~6번 슬롯 UI에 표시한다.
/// 슬롯 클릭/드래그 요청은 이벤트 채널로 다시 런타임에 전달한다.
/// </summary>
[DisallowMultipleComponent]
public class WeaponAmmoHUD : MonoBehaviour
{
    [Header("Loadout Events")]
    [SerializeField] private WeaponAmmoLoadoutEventChannelSO _weaponAmmoLoadoutEventChannel;
    [SerializeField] private VoidEventChannelSO _requestWeaponAmmoLoadoutSnapshotChannel;
    [SerializeField] private WeaponAmmoSlotSwapRequestEventChannelSO _slotSwapRequestChannel;
    [SerializeField] private IntEventChannelSO _slotSelectRequestChannel;

    [Header("Legacy Events")]
    [Tooltip("기존 단일 탄약 HUD 호환용. 새 슬롯 HUD만 쓸 거면 비워도 됨.")]
    [SerializeField] private WeaponAmmoEventChannelSO _weaponAmmoEventChannel;

    [Tooltip("기존 단일 탄약 HUD 호환용. 새 슬롯 HUD만 쓸 거면 비워도 됨.")]
    [SerializeField] private VoidEventChannelSO _requestWeaponAmmoSnapshotChannel;

    [Header("Slot UI")]
    [Tooltip("1번부터 6번까지 순서대로 넣는다.")]
    [SerializeField] private WeaponAmmoSlotUI[] _slotUIs;
    [Header("Sell")]
    [SerializeField] private BoolEventChannelSO _ammoSellModeChannel;
    [SerializeField] private WeaponAmmoSellPopupRequestEventChannelSO _sellPopupRequestChannel;

    [Tooltip("슬롯 선택 키 표시용. GameInput에 Slot1~Slot6 액션 추가 후 순서대로 넣는다. 비워두면 1,2,3... 표시.")]
    [SerializeField] private InputActionReference[] _slotKeyActions;

    
    private bool _sellModeVisible;
    private void OnEnable()
    {
        InitializeSlotUIs();

        if (_weaponAmmoLoadoutEventChannel != null)
            _weaponAmmoLoadoutEventChannel.OnEventRaised += HandleWeaponAmmoLoadoutChanged;


        if (_requestWeaponAmmoLoadoutSnapshotChannel != null)
            _requestWeaponAmmoLoadoutSnapshotChannel.RaiseEvent();

        if (_requestWeaponAmmoSnapshotChannel != null)
            _requestWeaponAmmoSnapshotChannel.RaiseEvent();

        if (_weaponAmmoLoadoutEventChannel != null &&
            _weaponAmmoLoadoutEventChannel.Current.slots != null)
        {
            HandleWeaponAmmoLoadoutChanged(_weaponAmmoLoadoutEventChannel.Current);
        }

        if (_ammoSellModeChannel != null)
            _ammoSellModeChannel.OnEventRaised += SetSellModeVisible;
    }

    private void OnDisable()
    {
        if (_weaponAmmoLoadoutEventChannel != null)
            _weaponAmmoLoadoutEventChannel.OnEventRaised -= HandleWeaponAmmoLoadoutChanged;
        if (_ammoSellModeChannel != null)
            _ammoSellModeChannel.OnEventRaised -= SetSellModeVisible;
    
    }

    public void RequestSelectSlot(int slotIndex)
    {
        if (_slotSelectRequestChannel != null)
            _slotSelectRequestChannel.RaiseEvent(slotIndex);
    }

    public void RequestSwapSlots(int fromSlotIndex, int toSlotIndex)
    {
        if (_slotSwapRequestChannel == null)
            return;

        _slotSwapRequestChannel.RaiseEvent(
            new WeaponAmmoSlotSwapRequest(fromSlotIndex, toSlotIndex));
    }

    private void HandleWeaponAmmoLoadoutChanged(WeaponAmmoLoadoutSnapshot snapshot)
    {
        if (_slotUIs == null || snapshot.slots == null)
            return;

        int count = Mathf.Min(_slotUIs.Length, snapshot.slots.Length);

        for (int i = 0; i < count; i++)
        {
            if (_slotUIs[i] == null)
                continue;

            _slotUIs[i].Bind(snapshot.slots[i], GetSlotKeyLabel(i));
            _slotUIs[i].SetSellModeVisible(_sellModeVisible);
        }

    }


    private void InitializeSlotUIs()
    {
        if (_slotUIs == null)
            return;

        for (int i = 0; i < _slotUIs.Length; i++)
        {
            if (_slotUIs[i] != null)
                _slotUIs[i].Initialize(this);
        }
    }

    private string GetSlotKeyLabel(int slotIndex)
    {
        InputAction action = null;

        if (_slotKeyActions != null &&
            slotIndex >= 0 &&
            slotIndex < _slotKeyActions.Length &&
            _slotKeyActions[slotIndex] != null)
        {
            action = _slotKeyActions[slotIndex].action;
        }

        if (action != null && action.bindings.Count > 0)
        {
            string bindingText = action.GetBindingDisplayString(
                0,
                InputBinding.DisplayStringOptions.DontIncludeInteractions);

            if (!string.IsNullOrWhiteSpace(bindingText))
                return bindingText;
        }

        return (slotIndex + 1).ToString();
    }

    public void SetSellModeVisible(bool visible)
    {
        _sellModeVisible = visible;

        if (_slotUIs == null)
            return;

        for (int i = 0; i < _slotUIs.Length; i++)
        {
            if (_slotUIs[i] != null)
                _slotUIs[i].SetSellModeVisible(visible);
        }
    }

    public void RequestSellSlot(WeaponAmmoSlotSnapshot snapshot)
    {
        if (_sellPopupRequestChannel != null)
            _sellPopupRequestChannel.RaiseEvent(new WeaponAmmoSellPopupRequest(snapshot));
    }
}