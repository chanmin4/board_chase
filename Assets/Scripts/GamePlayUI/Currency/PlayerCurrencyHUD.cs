using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerCurrencyHUD : MonoBehaviour
{
    [Header("Events")]
    [SerializeField] private PlayerCurrencyChangedEventChannelSO _currencyChangedChannel;
    [SerializeField] private VoidEventChannelSO _requestCurrencySnapshotChannel;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI _roguelikeCurrencyText;
    [SerializeField] private TextMeshProUGUI _runCurrencyText;

    [Header("Format")]
    [SerializeField] private string _roguelikeCurrencyFormat = "{0}";
    [SerializeField] private string _runCurrencyFormat = "{0}";

    private void OnEnable()
    {
        if (_currencyChangedChannel != null)
        {
            _currencyChangedChannel.OnEventRaised += HandleCurrencyChanged;

            if (_currencyChangedChannel.HasCurrent)
                HandleCurrencyChanged(_currencyChangedChannel.Current);
        }

        if (_requestCurrencySnapshotChannel != null)
            _requestCurrencySnapshotChannel.RaiseEvent();
    }

    private void OnDisable()
    {
        if (_currencyChangedChannel != null)
            _currencyChangedChannel.OnEventRaised -= HandleCurrencyChanged;
    }

    private void HandleCurrencyChanged(PlayerCurrencySnapshot snapshot)
    {
        if (_roguelikeCurrencyText != null)
            _roguelikeCurrencyText.text =
                string.Format(_roguelikeCurrencyFormat, snapshot.roguelikeCurrency);

        if (_runCurrencyText != null)
            _runCurrencyText.text =
                string.Format(_runCurrencyFormat, snapshot.runCurrency);
    }
}