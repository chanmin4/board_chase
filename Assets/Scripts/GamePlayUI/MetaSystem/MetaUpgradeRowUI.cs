using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MetaUpgradeRowUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private TextMeshProUGUI _costText;
    [SerializeField] private Button _purchaseButton;

    [Header("Format")]
    [SerializeField] private string _levelFormat = "Lv {0}/{1}";
    [SerializeField] private string _maxText = "MAX";

    private MetaProgressController _runtime;
    private MetaUpgradeId _upgradeId;

    private void OnEnable()
    {
        if (_purchaseButton != null)
            _purchaseButton.onClick.AddListener(HandlePurchaseClicked);
    }

    private void OnDisable()
    {
        if (_purchaseButton != null)
            _purchaseButton.onClick.RemoveListener(HandlePurchaseClicked);
    }

    public void Bind(MetaUpgradeSnapshot snapshot, MetaProgressController runtime)
    {
        _runtime = runtime;
        _upgradeId = snapshot.id;

        if (_iconImage != null)
        {
            _iconImage.sprite = snapshot.icon;
            _iconImage.enabled = snapshot.icon != null;
        }

        SetText(_nameText, snapshot.displayName);
        SetText(_descriptionText, snapshot.description);
        SetText(_levelText, string.Format(_levelFormat, snapshot.currentLevel, snapshot.maxLevel));
        SetText(_costText, snapshot.isMaxLevel ? _maxText : snapshot.nextCost.ToString());

        if (_purchaseButton != null)
            _purchaseButton.interactable = snapshot.canUpgrade;
    }

    private void HandlePurchaseClicked()
    {
        if (_runtime == null)
            return;

        _runtime.TryPurchase(_upgradeId);
    }

    private static void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
            text.text = value;
    }
}
