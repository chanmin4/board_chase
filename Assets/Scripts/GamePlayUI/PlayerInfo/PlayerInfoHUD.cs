using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Components;
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

    [Header("Reload Key Hints")]
    [SerializeField] private ReloadKeyHintView[] _reloadKeyHints = new ReloadKeyHintView[3];

    [SerializeField] private InputBinding.DisplayStringOptions _bindingDisplayOptions =
        InputBinding.DisplayStringOptions.DontIncludeInteractions;

    [NonSerialized] private PlayerShooterInfection _playerInfection;

    private PlayerHealthSnapshot _lastSnapshot;

    private void OnEnable()
    {
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

        RefreshReloadHints();
        RequestSnapshotRefresh();
    }

    private void OnDisable()
    {
        if (_playerHealthChanged != null)
            _playerHealthChanged.OnEventRaised -= OnHealthChanged;

        if (_updateHealthUI != null)
            _updateHealthUI.OnEventRaised -= RequestSnapshotRefresh;

        if (_playerInfectionReadyChannel != null)
            _playerInfectionReadyChannel.OnEventRaised -= OnPlayerInfectionChanged;

        InputSystem.onActionChange -= OnInputActionChanged;
    }

    private void OnHealthChanged(PlayerHealthSnapshot snapshot)
    {
        _lastSnapshot = snapshot;
        Apply(snapshot.maxHealth, snapshot.currentHealth, snapshot.currentInfection);
    }

    private void OnPlayerInfectionChanged(PlayerShooterInfection playerInfection)
    {
        _playerInfection = playerInfection;
        RequestSnapshotRefresh();
    }

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

        Apply(_lastSnapshot.maxHealth, _lastSnapshot.currentHealth, _lastSnapshot.currentInfection);
    }

    private void Apply(float maxHealth, float currentHealth, float currentInfection)
    {
        float health01 = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
        float infection01 = maxHealth > 0f ? Mathf.Clamp01(currentInfection / maxHealth) : 0f;

        if (_healthFill != null)
            _healthFill.fillAmount = health01;

        if (_infectionFill != null)
            _infectionFill.fillAmount = infection01;

        if (_healthText != null)
            _healthText.text = $"HP {Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";

        if (_infectionText != null)
            _infectionText.text = $"Infection {Mathf.CeilToInt(currentInfection)} / {Mathf.CeilToInt(currentHealth)}";
    }

    private void OnInputActionChanged(object actionOrMap, InputActionChange change)
    {
        if (change == InputActionChange.BoundControlsChanged)
            RefreshReloadHints();
    }

    private void RefreshReloadHints()
    {
        if (_reloadKeyHints == null)
            return;

        for (int i = 0; i < _reloadKeyHints.Length; i++)
            RefreshReloadHint(_reloadKeyHints[i]);
    }

    private void RefreshReloadHint(ReloadKeyHintView hint)
    {
        if (hint == null)
            return;

        RefreshReloadIcon(hint);
        RefreshReloadTitle(hint);
        RefreshReloadKeyText(hint);
    }

    private void RefreshReloadIcon(ReloadKeyHintView hint)
    {
        if (hint.reloadImage == null)
            return;

        if (hint.reloadSprite != null)
            hint.reloadImage.sprite = hint.reloadSprite;

        hint.reloadImage.enabled = hint.reloadImage.sprite != null;
    }

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

    private string GetBindingDisplayString(InputActionReference actionReference, int bindingIndex)
    {
        InputAction action = actionReference != null ? actionReference.action : null;

        if (action == null)
            return string.Empty;

        if (bindingIndex >= 0 && bindingIndex < action.bindings.Count)
            return action.GetBindingDisplayString(bindingIndex, _bindingDisplayOptions);

        return action.GetBindingDisplayString(_bindingDisplayOptions);
    }

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