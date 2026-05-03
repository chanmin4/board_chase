using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerHealthHUD : MonoBehaviour
{
    [Header("Events")]
    [SerializeField] private PlayerHealthEventChannelSO _playerHealthChanged;
    [SerializeField] private VoidEventChannelSO _updateHealthUI;

    [Header("Source")]
    [SerializeField] private HealthSO _playerHealthSO;
    [Header("Listening To")]
    [SerializeField] private PlayerInfectionEventChannelSO _playerInfectionReadyChannel;
    [NonSerialized] private PlayerInfection _playerInfection;

    [Header("UI")]
    [SerializeField] private Image _healthFill;
    [SerializeField] private Image _infectionFill;
    [SerializeField] private TextMeshProUGUI _healthText;
    [SerializeField] private TextMeshProUGUI _infectionText;

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
            _healthText.text = $"{Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
        if (_infectionText != null)
            _infectionText.text = $"{Mathf.CeilToInt(currentInfection)} / {Mathf.CeilToInt(maxHealth)}";
    }
}
