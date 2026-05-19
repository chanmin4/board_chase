using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class UISettingKeyBindingComponent : MonoBehaviour
{
    [Serializable]
    private sealed class BindingRow
    {
        [Tooltip("인스펙터에서 알아보기 위한 이름입니다. 실제 저장 키로 쓰지는 않습니다.")]
        public string displayName;

        [Tooltip("리바인딩할 Input Action입니다. 예: Gameplay/Attack, Gameplay/Dash")]
        public InputActionReference action;

        [Tooltip("해당 Action 안에서 바꿀 Binding Index입니다. WASD 같은 Composite는 Up/Down/Left/Right 각각의 binding index를 직접 넣어야 합니다.")]
        public int bindingIndex;

        [Tooltip("화면에 표시할 항목 이름 텍스트입니다. 비워도 기능은 동작합니다.")]
        public TextMeshProUGUI titleText;


        [Tooltip("이 항목 하나만 기본값으로 되돌리는 버튼입니다. 없으면 행별 초기화는 생략됩니다.")]
        public UIGenericButton resetButton;
        [Tooltip("이 버튼을 누르면 해당 키 리바인딩을 시작합니다.")]
        public UIGenericButton rebindButton;
        [Tooltip("현재 바인딩 값을 표시할 텍스트입니다. 예: W, Space, LMB")]
        public TextMeshProUGUI bindingText;



        [NonSerialized] public UnityAction rebindHandler;
        [NonSerialized] public UnityAction resetHandler;
    }

    [Header("Input")]
    [Tooltip("키 설정을 저장/불러올 Input Action Asset입니다. GameInput.inputactions 에셋을 넣으면 됩니다.")]
    [SerializeField] private InputActionAsset _inputActions;

    [Tooltip("PlayerPrefs에 저장할 binding override JSON 키입니다.")]
    [SerializeField] private string _bindingPrefsKey = "GameInput.BindingOverrides";

    [Header("Rows")]
    [Tooltip("키 설정 UI에 표시할 행 목록입니다. 사진의 상/하/좌/우/발사/조준 같은 항목을 여기에 하나씩 등록합니다.")]
    [SerializeField] private BindingRow[] _bindingRows;

    [Header("Reset")]
    [Tooltip("전체 키 설정을 기본값으로 되돌리는 버튼입니다. 키 설정 탭에서는 이 버튼만 유지합니다.")]
    [SerializeField] private UIGenericButton _resetDefaultsButton;

    [Header("Rebind Overlay")]
    [Tooltip("키 입력 대기 중 화면 중앙에 띄울 안내 패널입니다. 없어도 리바인딩은 동작합니다.")]
    [SerializeField] private CanvasGroup _rebindOverlayGroup;

    [Tooltip("입력 대기 패널의 제목 텍스트입니다. 예: 설정할 키를 누르세요")]
    [SerializeField] private TextMeshProUGUI _rebindTitleText;

    [Tooltip("입력 대기 패널의 보조 텍스트입니다. 예: esc 키를 눌러 취소하세요")]
    [SerializeField] private TextMeshProUGUI _rebindHintText;

    [Tooltip("리바인딩 중 마우스 움직임/휠 같은 축 입력을 무시합니다.")]
    [SerializeField] private bool _excludeMousePointerControls = true;

    public event UnityAction SaveRequested = delegate { };

    private InputActionRebindingExtensions.RebindingOperation _rebindOperation;
    private BindingRow _activeRow;

    private void OnEnable()
    {
        LoadBindingOverrides();

        if (_resetDefaultsButton != null)
            _resetDefaultsButton.Clicked += ResetAllToDefaults;

        SubscribeRows();
        RefreshView();
        SetRebindOverlayVisible(false);
    }

    private void OnDisable()
    {
        CancelActiveRebind();

        if (_resetDefaultsButton != null)
            _resetDefaultsButton.Clicked -= ResetAllToDefaults;

        UnsubscribeRows();
    }

    public void Setup()
    {
        LoadBindingOverrides();
        RefreshView();
    }

    private void SubscribeRows()
    {
        if (_bindingRows == null)
            return;

        for (int i = 0; i < _bindingRows.Length; i++)
        {
            BindingRow row = _bindingRows[i];
            if (row == null)
                continue;

            row.rebindHandler = () => BeginRebind(row);
            row.resetHandler = () => ResetSingleBinding(row);

            if (row.rebindButton != null)
                row.rebindButton.Clicked += row.rebindHandler;

            if (row.resetButton != null)
                row.resetButton.Clicked += row.resetHandler;
        }
    }

    private void UnsubscribeRows()
    {
        if (_bindingRows == null)
            return;

        for (int i = 0; i < _bindingRows.Length; i++)
        {
            BindingRow row = _bindingRows[i];
            if (row == null)
                continue;

            if (row.rebindButton != null && row.rebindHandler != null)
                row.rebindButton.Clicked -= row.rebindHandler;

            if (row.resetButton != null && row.resetHandler != null)
                row.resetButton.Clicked -= row.resetHandler;

            row.rebindHandler = null;
            row.resetHandler = null;
        }
    }

    private void BeginRebind(BindingRow row)
    {
        if (row == null || row.action == null || row.action.action == null)
            return;

        InputAction action = row.action.action;

        if (row.bindingIndex < 0 || row.bindingIndex >= action.bindings.Count)
        {
            Debug.LogWarning($"[KeyBinding] Invalid binding index. action={action.name}, index={row.bindingIndex}", this);
            return;
        }

        CancelActiveRebind();

        _activeRow = row;

        bool wasEnabled = action.enabled;
        action.Disable();

        SetRebindOverlayVisible(true);
        SetRebindOverlayText(row);

        _rebindOperation = action.PerformInteractiveRebinding(row.bindingIndex)
            .WithCancelingThrough("<Keyboard>/escape")
            .OnCancel(operation => FinishRebind(action, wasEnabled, false))
            .OnComplete(operation => FinishRebind(action, wasEnabled, true));

        if (_excludeMousePointerControls)
        {
            _rebindOperation
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .WithControlsExcluding("<Mouse>/scroll");
        }

        _rebindOperation.Start();
    }

    private void FinishRebind(InputAction action, bool wasEnabled, bool completed)
    {
        _rebindOperation?.Dispose();
        _rebindOperation = null;

        if (wasEnabled)
            action.Enable();

        SetRebindOverlayVisible(false);

        BindingRow row = _activeRow;
        _activeRow = null;

        if (row != null)
            RefreshRow(row);

        if (completed)
            SaveBindingOverrides();
    }

    private void CancelActiveRebind()
    {
        if (_rebindOperation == null)
            return;

        _rebindOperation.Cancel();
    }

    private void ResetSingleBinding(BindingRow row)
    {
        if (row == null || row.action == null || row.action.action == null)
            return;

        InputAction action = row.action.action;

        if (row.bindingIndex < 0 || row.bindingIndex >= action.bindings.Count)
            return;

        action.RemoveBindingOverride(row.bindingIndex);

        RefreshRow(row);
        SaveBindingOverrides();
    }

    private void ResetAllToDefaults()
    {
        if (_inputActions == null)
            return;

        _inputActions.RemoveAllBindingOverrides();

        RefreshView();
        SaveBindingOverrides();
    }

    private void RefreshView()
    {
        if (_bindingRows == null)
            return;

        for (int i = 0; i < _bindingRows.Length; i++)
            RefreshRow(_bindingRows[i]);
    }

    private void RefreshRow(BindingRow row)
    {
        if (row == null)
            return;

        if (row.titleText != null)
            row.titleText.text = string.IsNullOrWhiteSpace(row.displayName) ? "-" : row.displayName;

        if (row.bindingText == null)
            return;

        if (row.action == null || row.action.action == null)
        {
            row.bindingText.text = "-";
            return;
        }

        InputAction action = row.action.action;

        if (row.bindingIndex < 0 || row.bindingIndex >= action.bindings.Count)
        {
            row.bindingText.text = "Invalid";
            return;
        }

        string display = action.GetBindingDisplayString(
            row.bindingIndex,
            InputBinding.DisplayStringOptions.DontUseShortDisplayNames);

        row.bindingText.text = string.IsNullOrWhiteSpace(display) ? "-" : display;
    }

    private void LoadBindingOverrides()
    {
        if (_inputActions == null)
            return;

        if (!PlayerPrefs.HasKey(_bindingPrefsKey))
            return;

        string json = PlayerPrefs.GetString(_bindingPrefsKey);

        if (string.IsNullOrWhiteSpace(json))
            return;

        _inputActions.LoadBindingOverridesFromJson(json);
    }

    private void SaveBindingOverrides()
    {
        if (_inputActions == null)
            return;

        string json = _inputActions.SaveBindingOverridesAsJson();

        PlayerPrefs.SetString(_bindingPrefsKey, json);
        PlayerPrefs.Save();

        SaveRequested.Invoke();
    }

    private void SetRebindOverlayVisible(bool visible)
    {
        if (_rebindOverlayGroup == null)
            return;

        _rebindOverlayGroup.alpha = visible ? 1f : 0f;
        _rebindOverlayGroup.interactable = visible;
        _rebindOverlayGroup.blocksRaycasts = visible;
    }

    private void SetRebindOverlayText(BindingRow row)
    {
        if (_rebindTitleText != null)
            _rebindTitleText.text = "Press the key to set";

        if (_rebindHintText != null)
        {
            string targetName = row != null && !string.IsNullOrWhiteSpace(row.displayName)
                ? row.displayName
                : "selected item";

            _rebindHintText.text = $"{targetName} / Press the Esc key to cancel.";
        }
    }
}