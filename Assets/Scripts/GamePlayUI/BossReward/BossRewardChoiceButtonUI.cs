using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossRewardChoiceButtonUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Button _button;
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _descriptionText;

    private BossRewardOptionSO _reward;
    private Action<BossRewardOptionSO> _clicked;

    private void Reset()
    {
        if (_button == null)
            _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (_button != null)
            _button.onClick.AddListener(HandleClicked);
    }

    private void OnDisable()
    {
        if (_button != null)
            _button.onClick.RemoveListener(HandleClicked);
    }

    public void Bind(BossRewardOptionSO reward, Action<BossRewardOptionSO> clicked)
    {
        _reward = reward;
        _clicked = clicked;

        if (_titleText != null)
            _titleText.text = reward != null ? reward.Title : string.Empty;

        if (_descriptionText != null)
            _descriptionText.text = reward != null ? reward.Description : string.Empty;

        if (_icon != null)
        {
            _icon.sprite = reward != null ? reward.Icon : null;
            _icon.enabled = reward != null && reward.Icon != null;
        }

        if (_button != null)
            _button.interactable = reward != null;
    }

    private void HandleClicked()
    {
        if (_reward == null)
            return;

        _clicked?.Invoke(_reward);
    }
}
