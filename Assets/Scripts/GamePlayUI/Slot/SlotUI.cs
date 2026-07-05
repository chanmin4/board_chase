// SlotUI.cs
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SlotUI : MonoBehaviour,
    IPointerClickHandler,
    IBeginDragHandler,
    IEndDragHandler,
    IDropHandler
{
    [Header("Common")]
    [SerializeField] protected Image _iconImage;
    [SerializeField] protected TextMeshProUGUI _keyText;
    [SerializeField] protected TextMeshProUGUI _labelText;
    [SerializeField] protected Outline _selectedOutline;

    protected SlotHUD Owner { get; private set; }
    protected ItemSO CurrentItem { get; private set; }

    public bool HasItem => CurrentItem != null;

    public virtual void Initialize(SlotHUD owner)
    {
        Owner = owner;
    }

    public virtual void ClearVisual()
    {
        CurrentItem = null;

        if (_iconImage != null)
        {
            _iconImage.sprite = null;
            _iconImage.enabled = false;
        }

        if (_labelText != null)
            _labelText.text = string.Empty;

        SetSelected(false);
    }

    protected void SetItemVisual(ItemSO item)
    {
        SetItemVisual(
            item,
            item != null ? item.PreviewImage : null,
            ResolveItemName(item));
    }

    protected void SetItemVisual(ItemSO item, Sprite icon, string label)
    {
        CurrentItem = item;

        if (_iconImage != null)
        {
            _iconImage.sprite = icon;
            _iconImage.enabled = icon != null;
        }

        if (_labelText != null)
            _labelText.text = label ?? string.Empty;

    }

    protected void SetKeyLabel(string keyLabel)
    {
        if (_keyText != null)
            _keyText.text = keyLabel ?? string.Empty;
    }

    protected void SetSelected(bool selected)
    {
        if (_selectedOutline != null)
            _selectedOutline.enabled = selected;
    }



    public virtual void OnPointerClick(PointerEventData eventData)
    {
    }

    public virtual void OnBeginDrag(PointerEventData eventData)
    {
    }

    public virtual void OnEndDrag(PointerEventData eventData)
    {
    }

    public virtual void OnDrop(PointerEventData eventData)
    {
    }

    protected static string ResolveItemName(ItemSO item)
    {
        if (item == null)
            return string.Empty;

        if (item is BulletSO bullet)
            return bullet.DisplayName;

        if (item is WeaponSO weapon)
            return weapon.DisplayName;

        if (item is ArmorItemSO armor)
            return armor.DisplayName;

        if (item.Name != null)
        {
            string localized = item.Name.GetLocalizedString();

            if (!string.IsNullOrWhiteSpace(localized))
                return localized;
        }

        return item.name;
    }
}