/*
 * Role hierarchy: Presentation > UI > Gameplay > PlayerInfo
 * Primary responsibility: Presents PlayerInfoHUD state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerInfoHUD : MonoBehaviour
{
    [Serializable]
    private sealed class ReloadKeyHintView
    {
        [Header("Identity")]
        public string label;

        [Header("Icon")]
        public Image reloadImage;
        public Sprite reloadSprite;

        [Header("Title Localization")]
        public LocalizeStringEvent titleLocalizedEvent;
        public TextMeshProUGUI fallbackTitleText;
        public string titleTableCollectionName = "UI_Gameplay";
        public string titleEntryKey;
        public string fallbackTitle = "Reload";

        [Header("Key Binding")]
        public InputActionReference reloadAction;
        public int reloadBindingIndex = -1;
        public InputActionReference shotAction;
        public int shotBindingIndex = -1;
        public TextMeshProUGUI keyText;
        public string comboFormat = "{0} + {1}";
    }

    [Header("Events")]
    [SerializeField] private PlayerHealthEventChannelSO _playerHealthChanged;
    [SerializeField] private VoidEventChannelSO _updateHealthUI;

    [Header("Listening To")]
    [SerializeField] private PlayerInfectionEventChannelSO _playerInfectionReadyChannel;

    [Header("UI")]
    [SerializeField] private Image _healthFill;
    [SerializeField] private Image _infectionFill;
    [SerializeField] private TextMeshProUGUI _healthText;
    [SerializeField] private TextMeshProUGUI _infectionText;

    [Header("Localization")]
    [SerializeField] private string _stringTable = "UI_PlayerInfo";
    [SerializeField] private string _healthFormatKey = "PlayerInfo_HealthFormat";
    [SerializeField] private string _infectionFormatKey = "PlayerInfo_InfectionFormat";
    [SerializeField] private string _gasRatePositiveFormatKey = "PlayerInfo_GasRatePositiveFormat";

    [Header("Status Effects")]
    [Tooltip("Optional icon row near the infection bar. When empty it is created below the infection bar at runtime.")]
    [SerializeField] private RectTransform _statusEffectIconRoot;
    [SerializeField, Min(0.1f)] private float _infectionBlinkSpeed = 5f;

    [Header("Reload Key Hints")]
    [SerializeField] private ReloadKeyHintView[] _reloadKeyHints = new ReloadKeyHintView[3];

    [SerializeField] private InputBinding.DisplayStringOptions _bindingDisplayOptions =
        InputBinding.DisplayStringOptions.DontIncludeInteractions;

    [NonSerialized] private PlayerShooterInfection _playerInfection;
    private StatusEffectIconStripUI _statusEffectIcons;
    private Color _infectionBaseColor = Color.white;

    private PlayerHealthSnapshot _lastSnapshot;
    private Coroutine _localizationRefreshRoutine;

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_infectionFill != null)
            _infectionBaseColor = _infectionFill.color;

        EnsureStatusEffectIcons();
        if (_playerHealthChanged != null)
            _playerHealthChanged.OnEventRaised += OnHealthChanged;

        if (_updateHealthUI != null)
            _updateHealthUI.OnEventRaised += RequestSnapshotRefresh;

        if (_playerInfectionReadyChannel != null)
        {
            _playerInfectionReadyChannel.OnEventRaised += OnPlayerInfectionChanged;

            if (_playerInfectionReadyChannel.Current != null)
                OnPlayerInfectionChanged(_playerInfectionReadyChannel.Current);
        }

        InputSystem.onActionChange += OnInputActionChanged;
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
        RequestLocalizationRefresh();

        RefreshReloadHints();
        RequestSnapshotRefresh();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_playerHealthChanged != null)
            _playerHealthChanged.OnEventRaised -= OnHealthChanged;

        if (_updateHealthUI != null)
            _updateHealthUI.OnEventRaised -= RequestSnapshotRefresh;

        if (_playerInfectionReadyChannel != null)
            _playerInfectionReadyChannel.OnEventRaised -= OnPlayerInfectionChanged;

        InputSystem.onActionChange -= OnInputActionChanged;
        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;

        if (_localizationRefreshRoutine != null)
        {
            StopCoroutine(_localizationRefreshRoutine);
            _localizationRefreshRoutine = null;
        }
        _statusEffectIcons?.Bind(null);
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        RefreshInfectionBlink();
        RefreshInfectionRateText();
    }

    // Purpose: Responds to the health changed callback.
    private void OnHealthChanged(PlayerHealthSnapshot snapshot)
    {
        _lastSnapshot = snapshot;
        Apply(snapshot);
    }

    // Purpose: Responds to the player infection changed callback.
    private void OnPlayerInfectionChanged(PlayerShooterInfection playerInfection)
    {
        _playerInfection = playerInfection;
        EnsureStatusEffectIcons();
        _statusEffectIcons?.Bind(playerInfection != null ? playerInfection.StatusEffects : null);
        RequestSnapshotRefresh();
    }

    // Purpose: Requests snapshot refresh from the owning service.
    private void RequestSnapshotRefresh()
    {
        if (_playerInfection != null)
        {
            _playerInfection.PublishCurrentSnapshot();
            return;
        }

        if (_playerHealthChanged != null && _playerHealthChanged.HasCurrent)
        {
            OnHealthChanged(_playerHealthChanged.Current);
            return;
        }

        Apply(_lastSnapshot);
    }

    // Purpose: Applies the requested apply change.
    private void Apply(PlayerHealthSnapshot snapshot)
    {
        float maxHealth = snapshot.maxHealth;
        float currentHealth = snapshot.currentHealth;
        float currentInfection = snapshot.currentInfection;
        float health01 = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
        float infectionLimit = Mathf.Max(0f, maxHealth + currentInfection);
        float infection01 = infectionLimit > 0f
            ? Mathf.Clamp01(currentInfection / infectionLimit)
            : 0f;

        if (_healthFill != null)
            _healthFill.fillAmount = health01;

        if (_infectionFill != null)
            _infectionFill.fillAmount = infection01;

        if (_healthText != null)
        {
            _healthText.text = UIRuntimeLocalization.Format(
                _stringTable,
                _healthFormatKey,
                "HP {0} / {1}",
                Mathf.CeilToInt(currentHealth),
                Mathf.CeilToInt(maxHealth));
        }

        RefreshInfectionRateText(currentInfection, infectionLimit);
    }

    // Purpose: Displays only the active room-gas pressure, not the retired natural-recovery display value.
    private void RefreshInfectionRateText()
    {
        float infectionLimit = Mathf.Max(0f, _lastSnapshot.maxHealth + _lastSnapshot.currentInfection);
        RefreshInfectionRateText(_lastSnapshot.currentInfection, infectionLimit);
    }

    // Purpose: Applies the Infection total and its live environmental rate to the HUD.
    private void RefreshInfectionRateText(float currentInfection, float infectionLimit)
    {
        float gauge = _playerInfection != null ? _playerInfection.ExposureGauge : 0f;
        float capacity = _playerInfection != null ? _playerInfection.ExposureCapacity : 100f;
        if (_infectionFill != null) _infectionFill.fillAmount = Mathf.Clamp01(gauge / Mathf.Max(1f, capacity));
        if (_infectionText == null) return;

        float gasRate = _playerInfection != null
            ? _playerInfection.CurrentBiochemicalGasInfectionPerSecond
            : 0f;

        string gasRateText = gasRate > 0.001f
            ? UIRuntimeLocalization.Format(
                _stringTable,
                _gasRatePositiveFormatKey,
                "+{0}/s",
                gasRate.ToString("0.#"))
            : "0/s";

        _infectionText.text = $"감염 {Mathf.FloorToInt(gauge)}/{Mathf.FloorToInt(capacity)} · {gasRateText}";
    }

    private void HandleLocaleChanged(UnityEngine.Localization.Locale locale)
    {
        RequestLocalizationRefresh();
    }

    // Purpose: Applies the localized HUD labels after the selected table is ready.
    private void RequestLocalizationRefresh()
    {
        if (_localizationRefreshRoutine != null)
            StopCoroutine(_localizationRefreshRoutine);

        _localizationRefreshRoutine = StartCoroutine(RefreshLocalizedTextWhenReady());
    }

    private IEnumerator RefreshLocalizedTextWhenReady()
    {
        yield return UIRuntimeLocalization.EnsureTableLoaded(_stringTable, _healthFormatKey);

        if (isActiveAndEnabled)
            Apply(_lastSnapshot);

        _localizationRefreshRoutine = null;
    }

    // Purpose: Ensures status effect icons is ready before the workflow continues.
    private void EnsureStatusEffectIcons()
    {
        if (_statusEffectIcons != null)
        {
            _statusEffectIcons.ConfigureAsPlayerHealthGrid();
            return;
        }

        if (_statusEffectIconRoot != null)
        {
            _statusEffectIcons = _statusEffectIconRoot.GetComponent<StatusEffectIconStripUI>();

            if (_statusEffectIcons == null)
                _statusEffectIcons = _statusEffectIconRoot.gameObject.AddComponent<StatusEffectIconStripUI>();
        }

        if (_statusEffectIcons != null)
        {
            _statusEffectIcons.ConfigureAsPlayerHealthGrid();
            return;
        }

        RectTransform parent = transform as RectTransform;

        _statusEffectIcons = StatusEffectIconStripUI.Create(
            parent,
            "StatusEffectIcons",
            new Vector2(0f, 0f),
            new Vector2(12f, 14f),
            24f);
        _statusEffectIconRoot = _statusEffectIcons != null
            ? _statusEffectIcons.transform as RectTransform
            : null;
        _statusEffectIcons?.ConfigureAsPlayerHealthGrid();
    }

    // Purpose: Refreshes the displayed or cached infection blink.
    private void RefreshInfectionBlink()
    {
        if (_infectionFill == null)
            return;

        bool recoveryReduced =
            _playerInfection != null &&
            _playerInfection.InfectionRecoveryMultiplier < 0.999f;

        if (!recoveryReduced)
        {
            _infectionFill.color = _infectionBaseColor;
            return;
        }

        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.Max(0.1f, _infectionBlinkSpeed));
        _infectionFill.color = Color.Lerp(_infectionBaseColor, Color.white, pulse * 0.7f);
    }

    // Purpose: Responds to the input action changed callback.
    private void OnInputActionChanged(object actionOrMap, InputActionChange change)
    {
        if (change == InputActionChange.BoundControlsChanged)
            RefreshReloadHints();
    }

    // Purpose: Refreshes the displayed or cached reload hints.
    private void RefreshReloadHints()
    {
        if (_reloadKeyHints == null)
            return;

        for (int i = 0; i < _reloadKeyHints.Length; i++)
            RefreshReloadHint(_reloadKeyHints[i]);
    }

    // Purpose: Refreshes the displayed or cached reload hint.
    private void RefreshReloadHint(ReloadKeyHintView hint)
    {
        if (hint == null)
            return;

        RefreshReloadIcon(hint);
        RefreshReloadTitle(hint);
        RefreshReloadKeyText(hint);
    }

    // Purpose: Refreshes the displayed or cached reload icon.
    private void RefreshReloadIcon(ReloadKeyHintView hint)
    {
        if (hint.reloadImage == null)
            return;

        if (hint.reloadSprite != null)
            hint.reloadImage.sprite = hint.reloadSprite;

        hint.reloadImage.enabled = hint.reloadImage.sprite != null;
    }

    // Purpose: Refreshes the displayed or cached reload title.
    private void RefreshReloadTitle(ReloadKeyHintView hint)
    {
        if (hint.titleLocalizedEvent != null)
        {
            if (!string.IsNullOrWhiteSpace(hint.titleTableCollectionName))
                hint.titleLocalizedEvent.StringReference.TableReference = hint.titleTableCollectionName;

            if (!string.IsNullOrWhiteSpace(hint.titleEntryKey))
                hint.titleLocalizedEvent.StringReference.TableEntryReference = hint.titleEntryKey;

            hint.titleLocalizedEvent.RefreshString();
            return;
        }

        if (hint.fallbackTitleText != null)
            hint.fallbackTitleText.text = hint.fallbackTitle;
    }

    // Purpose: Refreshes the displayed or cached reload key text.
    private void RefreshReloadKeyText(ReloadKeyHintView hint)
    {
        if (hint.keyText == null)
            return;

        string reloadKey = GetBindingDisplayString(hint.reloadAction, hint.reloadBindingIndex);
        string shotKey = GetBindingDisplayString(hint.shotAction, hint.shotBindingIndex);

        bool hasReloadKey = !string.IsNullOrWhiteSpace(reloadKey);
        bool hasShotKey = !string.IsNullOrWhiteSpace(shotKey);

        if (hasReloadKey && hasShotKey)
            hint.keyText.text = SafeFormat(hint.comboFormat, reloadKey, shotKey);
        else if (hasReloadKey)
            hint.keyText.text = reloadKey;
        else if (hasShotKey)
            hint.keyText.text = shotKey;
        else
            hint.keyText.text = "-";
    }

    // Purpose: Returns the current binding display string.
    private string GetBindingDisplayString(InputActionReference actionReference, int bindingIndex)
    {
        InputAction action = actionReference != null ? actionReference.action : null;

        if (action == null)
            return string.Empty;

        if (bindingIndex >= 0 && bindingIndex < action.bindings.Count)
            return action.GetBindingDisplayString(bindingIndex, _bindingDisplayOptions);

        return action.GetBindingDisplayString(_bindingDisplayOptions);
    }

    // Purpose: Performs the safe format workflow for this component.
    private static string SafeFormat(string format, string value0, string value1)
    {
        if (string.IsNullOrWhiteSpace(format))
            return $"{value0} + {value1}";

        try
        {
            return string.Format(format, value0, value1);
        }
        catch
        {
            return $"{value0} + {value1}";
        }
    }
}
