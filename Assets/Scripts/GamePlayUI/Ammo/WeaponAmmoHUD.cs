using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 화면 하단 탄 슬롯 HUD.
/// PlayerBulletLoadoutRuntime이 방송한 WeaponAmmoLoadoutSnapshot을 받아 슬롯 UI에 표시한다.
/// 슬롯 클릭/드래그/키 입력 요청은 이벤트 채널로 다시 런타임에 전달한다.
/// </summary>
[DisallowMultipleComponent]
public class WeaponAmmoHUD : MonoBehaviour
{
    [Header("Need Ref - Input")]
    [Tooltip("슬롯키 입력 이벤트를 받는 InputReader입니다. Slot1~Slot5 입력을 받아 슬롯 선택 요청으로 변환합니다.")]
    [SerializeField] private InputReader _inputReader;

    [Header("Need Ref - Loadout Events")]
    [Tooltip("PlayerBulletLoadoutRuntime이 현재 탄 슬롯 상태를 방송하는 채널입니다.")]
    [SerializeField] private WeaponAmmoLoadoutEventChannelSO _weaponAmmoLoadoutEventChannel;

    [Tooltip("HUD가 켜질 때 현재 탄 슬롯 상태를 요청하는 채널입니다.")]
    [SerializeField] private VoidEventChannelSO _requestWeaponAmmoLoadoutSnapshotChannel;

    [Tooltip("슬롯 드래그 교체 요청을 PlayerBulletLoadoutRuntime에 보내는 채널입니다.")]
    [SerializeField] private WeaponAmmoSlotSwapRequestEventChannelSO _slotSwapRequestChannel;

    [Tooltip("슬롯 선택 요청을 PlayerBulletLoadoutRuntime에 보내는 채널입니다. 값은 0부터 시작하는 슬롯 인덱스입니다.")]
    [SerializeField] private IntEventChannelSO _slotSelectRequestChannel;

    [Header("Legacy Events")]
    [Tooltip("기존 단일 탄약 HUD 호환용. 새 슬롯 HUD만 쓸 거면 비워도 됩니다.")]
    [SerializeField] private WeaponAmmoEventChannelSO _weaponAmmoEventChannel;

    [Tooltip("기존 단일 탄약 HUD 호환용. 새 슬롯 HUD만 쓸 거면 비워도 됩니다.")]
    [SerializeField] private VoidEventChannelSO _requestWeaponAmmoSnapshotChannel;

    [Header("Slot UI")]
    [Tooltip("1번부터 5번까지 순서대로 넣습니다.")]
    [SerializeField] private WeaponAmmoSlotUI[] _slotUIs;

    [Tooltip("슬롯 선택 키 표시용입니다. GameInput의 Slot1~Slot5 액션을 순서대로 넣습니다. 비워두면 1,2,3... 표시.")]
    [SerializeField] private InputActionReference[] _slotKeyActions;

    [Header("Sell")]
    [SerializeField] private BoolEventChannelSO _ammoSellModeChannel;
    [SerializeField] private WeaponAmmoSellPopupRequestEventChannelSO _sellPopupRequestChannel;

    private bool _sellModeVisible;

    private void OnEnable()
    {
        InitializeSlotUIs();
        BindInputReader();

        if (_weaponAmmoLoadoutEventChannel != null)
            _weaponAmmoLoadoutEventChannel.OnEventRaised += HandleWeaponAmmoLoadoutChanged;

        if (_ammoSellModeChannel != null)
            _ammoSellModeChannel.OnEventRaised += SetSellModeVisible;

        if (_requestWeaponAmmoLoadoutSnapshotChannel != null)
            _requestWeaponAmmoLoadoutSnapshotChannel.RaiseEvent();

        if (_requestWeaponAmmoSnapshotChannel != null)
            _requestWeaponAmmoSnapshotChannel.RaiseEvent();

        if (_weaponAmmoLoadoutEventChannel != null &&
            _weaponAmmoLoadoutEventChannel.Current.slots != null)
        {
            HandleWeaponAmmoLoadoutChanged(_weaponAmmoLoadoutEventChannel.Current);
        }
    }

    private void OnDisable()
    {
        UnbindInputReader();

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

    private void BindInputReader()
    {
        if (_inputReader == null)
            return;

        _inputReader.Slot1Event += SelectSlot1;
        _inputReader.Slot2Event += SelectSlot2;
        _inputReader.Slot3Event += SelectSlot3;
        _inputReader.Slot4Event += SelectSlot4;
        _inputReader.Slot5Event += SelectSlot5;
    }

    private void UnbindInputReader()
    {
        if (_inputReader == null)
            return;

        _inputReader.Slot1Event -= SelectSlot1;
        _inputReader.Slot2Event -= SelectSlot2;
        _inputReader.Slot3Event -= SelectSlot3;
        _inputReader.Slot4Event -= SelectSlot4;
        _inputReader.Slot5Event -= SelectSlot5;
    }

    private void SelectSlot1() => RequestSelectSlot(0);
    private void SelectSlot2() => RequestSelectSlot(1);
    private void SelectSlot3() => RequestSelectSlot(2);
    private void SelectSlot4() => RequestSelectSlot(3);
    private void SelectSlot5() => RequestSelectSlot(4);

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
}