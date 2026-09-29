/*
 * Role hierarchy: Presentation > UI > Common > ESCMenu
 * Primary responsibility: Collects UISettingKeyBindingComponent input and forwards it as gameplay action requests.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

public class UISettingKeyBindingComponent : MonoBehaviour
{
    private readonly struct DefaultBindingRowSpec
    {
        public readonly string rowObjectName;
        public readonly string displayName;
        public readonly string titleEntryKey;
        public readonly string actionMapName;
        public readonly string actionName;
        public readonly int bindingIndex;
        public readonly bool createIfMissing;

        public DefaultBindingRowSpec(
            string rowObjectName,
            string displayName,
            string titleEntryKey,
            string actionMapName,
            string actionName,
            int bindingIndex,
            bool createIfMissing = false)
        {
            this.rowObjectName = rowObjectName;
            this.displayName = displayName;
            this.titleEntryKey = titleEntryKey;
            this.actionMapName = actionMapName;
            this.actionName = actionName;
            this.bindingIndex = bindingIndex;
            this.createIfMissing = createIfMissing;
        }
    }

    // The prefab already contains most of these rows. Keeping their routing in
    // one table prevents visual row names from becoming undocumented gameplay
    // input. Missing quick-slot rows are created from that same authored row prefab.
    private static readonly DefaultBindingRowSpec[] DefaultBindingRows =
    {
        new("Settings_Item_MoveUP", "Move Up", "Settings_Key_MoveUp", "Gameplay", "Move", 1),
        new("Settings_Item_MoveDown", "Move Down", "Settings_Key_MoveDown", "Gameplay", "Move", 2),
        new("Settings_Item_MoveLeft", "Move Left", "Settings_Key_MoveLeft", "Gameplay", "Move", 3),
        new("Settings_Item_MoveRight", "Move Right", "Settings_Key_MoveRight", "Gameplay", "Move", 4),
        new("Settings_Item_Attack", "Fire", "Settings_Key_Fire", "Gameplay", "Shoot", 0),
        new("Settings_Item_Interact", "Interact", "Settings_Key_Interact", "Gameplay", "Interact", 0),
        new("Settings_Item_Dash", "Dash", "Settings_Key_Dash", "Gameplay", "Dash", 0),
        new("Settings_Item_Shockwave", "Melee", "Settings_Key_Melee", "Gameplay", "MeleeAttack", 0),
        new("Settings_Item_Special_Interact", "Reload", "Settings_Key_Reload", "Gameplay", "Reload", 0),
        new("Settings_Item_Upgrade", "Player Menu", "Settings_Key_PlayerMenu", "Gameplay", "PlayerMenu", 0),
        new("Settings_Item_Paint", "Map", "Settings_Key_Map", "Gameplay", "Map", 0),
        new("Settings_Item_BulletSlot1", "Quick Slot 1", "Settings_Key_QuickSlot1", "Gameplay", "Slot1", 0),
        new("Settings_Item_BulletSlot2", "Quick Slot 2", "Settings_Key_QuickSlot2", "Gameplay", "Slot2", 0),
        new("Settings_Item_QuickSlot3", "Quick Slot 3", "Settings_Key_QuickSlot3", "Gameplay", "Slot3", 0, true),
        new("Settings_Item_QuickSlot4", "Quick Slot 4", "Settings_Key_QuickSlot4", "Gameplay", "Slot4", 0, true),
        new("Settings_Item_QuickSlot5", "Quick Slot 5", "Settings_Key_QuickSlot5", "Gameplay", "Slot5", 0, true)
    };

    [Serializable]
    private sealed class BindingRow
    {
        [Tooltip("인스펙터에서 알아보기 위한 이름입니다. 실제 저장 키로 쓰지는 않습니다.")]
        public string displayName;

        [Tooltip("UI_Settings 테이블에서 표시 이름을 찾을 키입니다. 비어 있으면 영문 표시 이름을 사용합니다.")]
        public string titleEntryKey;

        [Tooltip("리바인딩할 Input Action입니다. 예: Gameplay/Attack, Gameplay/Dash")]
        public InputActionReference action;

        [Tooltip("실제 게임에서 사용하는 Action Map 이름입니다. Input Reader가 연결되면 이 값이 우선 사용됩니다. 예: Gameplay")]
        public string actionMapName = "Gameplay";

        [Tooltip("실제 게임에서 사용하는 Action 이름입니다. 예: Move, Dash, Reload")]
        public string actionName;

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

    [Tooltip("실제 플레이 입력을 가진 Input Reader입니다. 반드시 UI의 authoring asset보다 우선 사용해 리바인드가 즉시 게임에 반영됩니다.")]
    [SerializeField] private InputReader _inputReader;

    [Tooltip("PlayerPrefs에 저장할 binding override JSON 키입니다.")]
    [SerializeField] private string _bindingPrefsKey = "GameInput.BindingOverrides";

    [Header("Localization")]
    [SerializeField] private TableReference _stringTable = "UI_Settings";
    [SerializeField] private string _rebindTitleEntryKey = "Settings_Key_RebindTitle";
    [SerializeField] private string _rebindCancelEntryKey = "Settings_Key_RebindCancel";

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
    private Coroutine _localizedRowsRoutine;
    private Coroutine _localizedRebindOverlayRoutine;

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        LoadBindingOverrides();
        EnsureDefaultRowsRegistered();

        if (_resetDefaultsButton != null)
            _resetDefaultsButton.Clicked += ResetAllToDefaults;

        SubscribeRows();
        RefreshView();
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
        RefreshLocalizedRowTitles();
        SetRebindOverlayVisible(false);
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        CancelActiveRebind();

        if (_resetDefaultsButton != null)
            _resetDefaultsButton.Clicked -= ResetAllToDefaults;

        UnsubscribeRows();
        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;

        if (_localizedRowsRoutine != null)
        {
            StopCoroutine(_localizedRowsRoutine);
            _localizedRowsRoutine = null;
        }

        if (_localizedRebindOverlayRoutine != null)
        {
            StopCoroutine(_localizedRebindOverlayRoutine);
            _localizedRebindOverlayRoutine = null;
        }
    }

    // Purpose: Updates the setup state.
    public void Setup()
    {
        LoadBindingOverrides();
        EnsureDefaultRowsRegistered();
        RefreshView();
        RefreshLocalizedRowTitles();
    }

    /// <summary>
    /// Registers every shipped gameplay binding against the authored Settings
    /// rows. Older panels already contain the base rows but only serialized
    /// four of them, leaving buttons that looked valid but did nothing.
    /// Two new actions are cloned from the same authored row layout at runtime
    /// solely because their visual rows did not exist in the legacy prefab.
    /// </summary>
    private void EnsureDefaultRowsRegistered()
    {
        List<BindingRow> rows = _bindingRows != null
            ? new List<BindingRow>(_bindingRows)
            : new List<BindingRow>();

        Transform template = FindDescendantTransform("Settings_Item_Attack");

        for (int i = 0; i < DefaultBindingRows.Length; i++)
        {
            DefaultBindingRowSpec spec = DefaultBindingRows[i];
            BindingRow existingRow = FindBindingRow(rows, spec);
            if (existingRow != null)
            {
                ApplyDefaultRowLocalization(existingRow, spec);
                continue;
            }

            Transform rowRoot = FindDescendantTransform(spec.rowObjectName);
            if (rowRoot == null && spec.createIfMissing && template != null)
            {
                GameObject clonedRow = Instantiate(template.gameObject, template.parent);
                clonedRow.name = spec.rowObjectName;
                rowRoot = clonedRow.transform;
            }

            BindingRow row = CreateBindingRow(rowRoot, spec);
            if (row != null)
            {
                ApplyDefaultRowLocalization(row, spec);
                rows.Add(row);
            }
            else
                Debug.LogWarning(
                    $"[KeyBinding] Authored row is missing or malformed. row={spec.rowObjectName}, action={spec.actionMapName}/{spec.actionName}",
                    this);
        }

        _bindingRows = rows.ToArray();
    }

    // Purpose: Checks whether one action/binding is already represented in the Settings list.
    private static BindingRow FindBindingRow(
        List<BindingRow> rows,
        DefaultBindingRowSpec spec)
    {
        if (rows == null)
            return null;

        for (int i = 0; i < rows.Count; i++)
        {
            BindingRow row = rows[i];
            if (row != null &&
                string.Equals(row.actionMapName, spec.actionMapName, StringComparison.Ordinal) &&
                string.Equals(row.actionName, spec.actionName, StringComparison.Ordinal) &&
                row.bindingIndex == spec.bindingIndex)
            {
                return row;
            }
        }

        return null;
    }

    // Purpose: Applies the stable localized title to both existing and dynamically created rows.
    private static void ApplyDefaultRowLocalization(BindingRow row, DefaultBindingRowSpec spec)
    {
        if (row == null)
            return;

        row.displayName = string.IsNullOrWhiteSpace(row.displayName)
            ? spec.displayName
            : row.displayName;
        row.titleEntryKey = spec.titleEntryKey;

        if (row.titleText != null)
        {
            LocalizeStringEvent legacyLocalizer = row.titleText.GetComponent<LocalizeStringEvent>();
            if (legacyLocalizer != null)
                legacyLocalizer.enabled = false;
        }
    }

    // Purpose: Builds one runtime binding descriptor from the existing key-row prefab hierarchy.
    private static BindingRow CreateBindingRow(
        Transform rowRoot,
        DefaultBindingRowSpec spec)
    {
        if (rowRoot == null)
            return null;

        TextMeshProUGUI title = FindChildComponent<TextMeshProUGUI>(rowRoot, "Setting_Item_Title");
        TextMeshProUGUI binding = FindChildComponent<TextMeshProUGUI>(rowRoot, "KeyText");
        UIGenericButton reset = FindChildComponent<UIGenericButton>(rowRoot, "ResetButton");
        UIGenericButton rebind = FindChildComponent<UIGenericButton>(rowRoot, "ChangeKeyButton");

        if (title == null || binding == null || reset == null || rebind == null)
            return null;

        return new BindingRow
        {
            displayName = spec.displayName,
            titleEntryKey = spec.titleEntryKey,
            actionMapName = spec.actionMapName,
            actionName = spec.actionName,
            bindingIndex = spec.bindingIndex,
            titleText = title,
            resetButton = reset,
            rebindButton = rebind,
            bindingText = binding
        };
    }

    // Purpose: Finds an inactive or active child row by its authoring object name.
    private Transform FindDescendantTransform(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return null;

        Transform[] transforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate != null && candidate.name == objectName)
                return candidate;
        }

        return null;
    }

    // Purpose: Resolves a named child and component without depending on prefab file IDs.
    private static T FindChildComponent<T>(Transform rowRoot, string objectName)
        where T : Component
    {
        if (rowRoot == null)
            return null;

        Transform[] transforms = rowRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate != null && candidate.name == objectName)
                return candidate.GetComponent<T>();
        }

        return null;
    }

    // Purpose: Subscribes to rows notifications.
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

    // Purpose: Removes the rows notification subscription.
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

    // Purpose: Begins the rebind workflow.
    private void BeginRebind(BindingRow row)
    {
        InputAction action = ResolveAction(row);
        if (action == null)
            return;

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

    // Purpose: Finishes the active rebind workflow.
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

    // Purpose: Checks whether cancel active rebind is currently permitted.
    private void CancelActiveRebind()
    {
        if (_rebindOperation == null)
            return;

        _rebindOperation.Cancel();
    }

    // Purpose: Performs the reset single binding workflow for this component.
    private void ResetSingleBinding(BindingRow row)
    {
        InputAction action = ResolveAction(row);
        if (action == null)
            return;

        if (row.bindingIndex < 0 || row.bindingIndex >= action.bindings.Count)
            return;

        action.RemoveBindingOverride(row.bindingIndex);

        RefreshRow(row);
        SaveBindingOverrides();
    }

    // Purpose: Performs the reset all to defaults workflow for this component.
    private void ResetAllToDefaults()
    {
        InputActionAsset inputActions = ResolveInputActions();
        if (inputActions == null)
            return;

        inputActions.RemoveAllBindingOverrides();

        RefreshView();
        SaveBindingOverrides();
    }

    // Purpose: Refreshes the displayed or cached view.
    private void RefreshView()
    {
        if (_bindingRows == null)
            return;

        for (int i = 0; i < _bindingRows.Length; i++)
            RefreshRow(_bindingRows[i]);
    }

    // Purpose: Refreshes the displayed or cached row.
    private void RefreshRow(BindingRow row)
    {
        if (row == null)
            return;

        if (row.titleText != null)
            row.titleText.text = string.IsNullOrWhiteSpace(row.displayName) ? "-" : row.displayName;

        if (row.bindingText == null)
            return;

        InputAction action = ResolveAction(row);
        if (action == null)
        {
            row.bindingText.text = "-";
            return;
        }

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

    // Purpose: Refreshes key-binding text when the selected UI locale changes.
    private void HandleLocaleChanged(UnityEngine.Localization.Locale locale)
    {
        RefreshLocalizedRowTitles();
        RefreshLocalizedRebindOverlay();
    }

    // Purpose: Starts the asynchronous localization pass for every visible binding row.
    private void RefreshLocalizedRowTitles()
    {
        if (!isActiveAndEnabled)
            return;

        if (_localizedRowsRoutine != null)
            StopCoroutine(_localizedRowsRoutine);

        _localizedRowsRoutine = StartCoroutine(RefreshLocalizedRowTitlesRoutine());
    }

    // Purpose: Resolves the visible row titles from the Settings string table.
    private System.Collections.IEnumerator RefreshLocalizedRowTitlesRoutine()
    {
        yield return LocalizationSettings.InitializationOperation;

        if (_bindingRows != null)
        {
            for (int i = 0; i < _bindingRows.Length; i++)
            {
                BindingRow row = _bindingRows[i];
                if (row == null || row.titleText == null || string.IsNullOrWhiteSpace(row.titleEntryKey))
                    continue;

                AsyncOperationHandle<string> handle =
                    LocalizationSettings.StringDatabase.GetLocalizedStringAsync(
                        _stringTable,
                        row.titleEntryKey);

                yield return handle;

                if (handle.Status == AsyncOperationStatus.Succeeded && !string.IsNullOrWhiteSpace(handle.Result))
                    row.titleText.text = handle.Result;
                else
                    row.titleText.text = string.IsNullOrWhiteSpace(row.displayName) ? "-" : row.displayName;
            }
        }

        _localizedRowsRoutine = null;
    }

    // Purpose: Loads binding overrides into the active runtime state.
    private void LoadBindingOverrides()
    {
        if (_inputReader != null)
        {
            _inputReader.LoadBindingOverrides();
            return;
        }

        if (_inputActions == null)
            return;

        SaveSystem saveSystem = SaveSystem.Active;
        string json = saveSystem != null
            ? saveSystem.GetInputBindingOverrides()
            : PlayerPrefs.GetString(_bindingPrefsKey, string.Empty);

        if (string.IsNullOrWhiteSpace(json))
            return;

        _inputActions.LoadBindingOverridesFromJson(json);
    }

    // Purpose: Saves the current binding overrides state.
    private void SaveBindingOverrides()
    {
        if (_inputReader != null)
        {
            _inputReader.SaveBindingOverrides();
            SaveRequested.Invoke();
            return;
        }

        if (_inputActions == null)
            return;

        string json = _inputActions.SaveBindingOverridesAsJson();

        SaveSystem saveSystem = SaveSystem.Active;
        if (saveSystem != null)
            saveSystem.SetInputBindingOverrides(json);
        else
        {
            PlayerPrefs.SetString(_bindingPrefsKey, json);
            PlayerPrefs.Save();
        }

        SaveRequested.Invoke();
    }

    /// <summary>
    /// Resolves a row against the live InputReader first. The authored action
    /// reference remains a compatibility fallback for older Settings prefabs.
    /// </summary>
    private InputAction ResolveAction(BindingRow row)
    {
        if (row == null)
            return null;

        string mapName = row.actionMapName;
        string actionName = row.actionName;

        if ((string.IsNullOrWhiteSpace(mapName) || string.IsNullOrWhiteSpace(actionName)) &&
            row.action != null && row.action.action != null)
        {
            mapName = row.action.action.actionMap != null
                ? row.action.action.actionMap.name
                : mapName;
            actionName = row.action.action.name;
        }

        if (_inputReader != null)
        {
            InputAction runtimeAction = _inputReader.FindRuntimeAction(mapName, actionName);
            if (runtimeAction != null)
                return runtimeAction;
        }

        if (_inputActions == null || string.IsNullOrWhiteSpace(actionName))
            return null;

        string qualifiedName = string.IsNullOrWhiteSpace(mapName)
            ? actionName
            : $"{mapName}/{actionName}";

        return _inputActions.FindAction(qualifiedName, throwIfNotFound: false);
    }

    /// <summary>
    /// Returns the runtime asset when available, otherwise preserves the old
    /// Settings prefab behaviour through its serialized authoring asset.
    /// </summary>
    private InputActionAsset ResolveInputActions()
    {
        return _inputReader != null && _inputReader.RuntimeInputActions != null
            ? _inputReader.RuntimeInputActions
            : _inputActions;
    }

    // Purpose: Updates the rebind overlay visible state.
    private void SetRebindOverlayVisible(bool visible)
    {
        if (_rebindOverlayGroup == null)
            return;

        _rebindOverlayGroup.alpha = visible ? 1f : 0f;
        _rebindOverlayGroup.interactable = visible;
        _rebindOverlayGroup.blocksRaycasts = visible;
    }

    // Purpose: Updates the rebind overlay text state.
    private void SetRebindOverlayText(BindingRow row)
    {
        if (_rebindTitleText != null)
            _rebindTitleText.text = "Press a key to bind";

        if (_rebindHintText != null)
        {
            string targetName = row != null && !string.IsNullOrWhiteSpace(row.displayName)
                ? row.displayName
                : "selected item";

            _rebindHintText.text = $"{targetName}\nPress Esc to cancel";
        }

        RefreshLocalizedRebindOverlay();
    }

    // Purpose: Starts the asynchronous localization pass for the active rebind prompt.
    private void RefreshLocalizedRebindOverlay()
    {
        if (!isActiveAndEnabled || _activeRow == null)
            return;

        if (_localizedRebindOverlayRoutine != null)
            StopCoroutine(_localizedRebindOverlayRoutine);

        _localizedRebindOverlayRoutine = StartCoroutine(
            RefreshLocalizedRebindOverlayRoutine(_activeRow));
    }

    // Purpose: Resolves the active rebind title, action name, and cancel hint from the Settings table.
    private System.Collections.IEnumerator RefreshLocalizedRebindOverlayRoutine(BindingRow row)
    {
        yield return LocalizationSettings.InitializationOperation;

        if (_activeRow != row)
            yield break;

        string title = "Press a key to bind";
        string actionName = row != null ? row.displayName : "selected item";
        string cancel = "Press Esc to cancel";

        if (!string.IsNullOrWhiteSpace(_rebindTitleEntryKey))
        {
            AsyncOperationHandle<string> handle =
                LocalizationSettings.StringDatabase.GetLocalizedStringAsync(
                    _stringTable,
                    _rebindTitleEntryKey);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded && !string.IsNullOrWhiteSpace(handle.Result))
                title = handle.Result;
        }

        if (row != null && !string.IsNullOrWhiteSpace(row.titleEntryKey))
        {
            AsyncOperationHandle<string> handle =
                LocalizationSettings.StringDatabase.GetLocalizedStringAsync(
                    _stringTable,
                    row.titleEntryKey);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded && !string.IsNullOrWhiteSpace(handle.Result))
                actionName = handle.Result;
        }

        if (!string.IsNullOrWhiteSpace(_rebindCancelEntryKey))
        {
            AsyncOperationHandle<string> handle =
                LocalizationSettings.StringDatabase.GetLocalizedStringAsync(
                    _stringTable,
                    _rebindCancelEntryKey);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded && !string.IsNullOrWhiteSpace(handle.Result))
                cancel = handle.Result;
        }

        if (_activeRow == row)
        {
            if (_rebindTitleText != null)
                _rebindTitleText.text = title;

            if (_rebindHintText != null)
                _rebindHintText.text = $"{actionName}\n{cancel}";
        }

        _localizedRebindOverlayRoutine = null;
    }
}
