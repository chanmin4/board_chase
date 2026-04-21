using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerLevelHUD : MonoBehaviour
{
    [Header("Events")]
    [SerializeField] private PlayerLevelSnapshotEventChannelSO _levelSnapshotChannel;
    [SerializeField] private VoidEventChannelSO _requestLevelSnapshotChannel;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private TextMeshProUGUI _xpText;
    [SerializeField] private Image _xpFill;
    [Tooltip("if exp reach cap   set active true")]
    [SerializeField] private GameObject _stageCapRoot;

    private void OnEnable()
    {
        if (_levelSnapshotChannel != null)
            _levelSnapshotChannel.OnEventRaised += OnLevelSnapshotChanged;

        if (_requestLevelSnapshotChannel != null)
            _requestLevelSnapshotChannel.RaiseEvent();
    }

    private void OnDisable()
    {
        if (_levelSnapshotChannel != null)
            _levelSnapshotChannel.OnEventRaised -= OnLevelSnapshotChanged;
    }

    private void OnLevelSnapshotChanged(PlayerLevelSnapshot snapshot)
    {
        if (_levelText != null)
            _levelText.text = $"Lv. {snapshot.level}";

        if (_xpText != null)
            _xpText.text = $"{snapshot.currentXp:F2} / {snapshot.requiredXp:F0}";

        if (_xpFill != null)
            _xpFill.fillAmount = snapshot.progress01;

        if (_stageCapRoot != null)
            _stageCapRoot.SetActive(snapshot.stageXpCapped);
    }
}
