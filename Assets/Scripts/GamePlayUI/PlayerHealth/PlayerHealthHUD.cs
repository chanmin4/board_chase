using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerHealthHUD : MonoBehaviour
{
    [Serializable]
    private sealed class ReloadKeyHintView
    {
        [Header("Identity")]
        [Tooltip("인스펙터 구분용 이름입니다. 기능에는 직접 사용하지 않습니다. 예: Attack Reload")]
        public string label;

        [Header("Icon")]
        [Tooltip("장전 안내 이미지입니다. 비워두면 기존 Image sprite를 유지합니다.")]
        public Image reloadImage;

        [Tooltip("reloadImage에 넣을 sprite입니다.")]
        public Sprite reloadSprite;

        [Header("Title Localization")]
        [Tooltip("장전 제목 LocalizeStringEvent입니다. 예: Attack Reload / Paint Reload / Special Reload")]
        public LocalizeStringEvent titleLocalizedEvent;

        [Tooltip("LocalizeStringEvent를 쓰지 않을 때 직접 텍스트를 넣을 TMP입니다.")]
        public TextMeshProUGUI fallbackTitleText;

        [Tooltip("String Table Collection 이름입니다. 예: UI_Gameplay")]
        public string titleTableCollectionName = "UI_Gameplay";

        [Tooltip("String Table Entry Key입니다. 예: Reload_AttackTitle")]
        public string titleEntryKey;

        [Tooltip("로컬라이즈가 없을 때 표시할 기본 제목입니다.")]
        public string fallbackTitle = "Reload";

        [Header("Key Binding")]
        [Tooltip("장전 modifier 액션입니다. 예: Gameplay/Reload")]
        public InputActionReference reloadAction;

        [Tooltip("-1이면 액션의 기본 표시 문자열을 사용합니다. 특정 binding을 강제로 표시하려면 binding index를 넣으세요.")]
        public int reloadBindingIndex = -1;

        [Tooltip("실제 발사 액션입니다. Attack=Gameplay/Attack, Paint=Gameplay/Paint, Special=Gameplay/Special 또는 SpecialShot")]
        public InputActionReference shotAction;

        [Tooltip("-1이면 액션의 기본 표시 문자열을 사용합니다. 특정 binding을 강제로 표시하려면 binding index를 넣으세요.")]
        public int shotBindingIndex = -1;

        [Tooltip("키 표시 TMP입니다. 예: R + LMB")]
        public TextMeshProUGUI keyText;

        [Tooltip("reloadAction과 shotAction을 같이 표시할 때 포맷입니다. {0}=Reload, {1}=Shot")]
        public string comboFormat = "{0} + {1}";
    }

    [Header("Events")]
    [SerializeField] private PlayerHealthEventChannelSO _playerHealthChanged;
    [SerializeField] private VoidEventChannelSO _updateHealthUI;

    [Header("Source")]
    [SerializeField] private HealthSO _playerHealthSO;

    [Header("Listening To")]
    [SerializeField] private PlayerInfectionEventChannelSO _playerInfectionReadyChannel;

    [Header("UI")]
    [SerializeField] private Image _healthFill;
    [SerializeField] private Image _infectionFill;
    [SerializeField] private TextMeshProUGUI _healthText;
    [SerializeField] private TextMeshProUGUI _infectionText;

    [Header("Reload Key Hints")]
    [Tooltip("0=Attack, 1=Paint, 2=Special 권장. 각 칸에 이미지/title/key binding을 연결하세요.")]
    [SerializeField] private ReloadKeyHintView[] _reloadKeyHints = new ReloadKeyHintView[3];

    [Tooltip("키 이름 표시 방식입니다. None이면 짧은 표기(LMB 등)를 우선 사용합니다.")]
    [SerializeField] private InputBinding.DisplayStringOptions _bindingDisplayOptions =
    InputBinding.DisplayStringOptions.DontIncludeInteractions;

    [NonSerialized] private PlayerInfection _playerInfection;

    private float _lastInfection;

    private void OnEnable()
    {
        if (_playerHealthChanged != null)
            _playerHealthChanged.OnEventRaised += OnHealthChanged;

        if (_updateHealthUI != null)
            _updateHealthUI.OnEventRaised += RefreshFromHealthSO;

        if (_playerInfectionReadyChannel != null)
        {
            _playerInfectionReadyChannel.OnEventRaised += OnPlayerInfectionChanged;

            if (_playerInfectionReadyChannel.Current != null)
                OnPlayerInfectionChanged(_playerInfectionReadyChannel.Current);
        }

        InputSystem.onActionChange += OnInputActionChanged;

        RefreshReloadHints();
        RefreshFromHealthSO();
    }

    private void OnDisable()
    {
        if (_playerHealthChanged != null)
            _playerHealthChanged.OnEventRaised -= OnHealthChanged;

        if (_updateHealthUI != null)
            _updateHealthUI.OnEventRaised -= RefreshFromHealthSO;

        if (_playerInfectionReadyChannel != null)
            _playerInfectionReadyChannel.OnEventRaised -= OnPlayerInfectionChanged;

        InputSystem.onActionChange -= OnInputActionChanged;
    }

    private void OnHealthChanged(PlayerHealthSnapshot snapshot)
    {
        _lastInfection = snapshot.currentInfection;
        Apply(snapshot.maxHealth, snapshot.currentHealth, snapshot.currentInfection);
    }

    private void OnPlayerInfectionChanged(PlayerInfection playerInfection)
    {
        _playerInfection = playerInfection;
        RefreshFromHealthSO();
    }

    private void RefreshFromHealthSO()
    {
        if (_playerHealthSO == null)
            return;

        float infection = _playerInfection != null
            ? _playerInfection.CurrentInfection
            : _lastInfection;

        Apply(_playerHealthSO.MaxHealth, _playerHealthSO.CurrentHealth, infection);
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