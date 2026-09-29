/*
 * Role hierarchy: Presentation > UI > Gameplay > Slot
 * Primary responsibility: Presents SlotUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

// SlotUI.cs
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
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
    private LocalizedString _boundItemName;
    private string _boundItemNameFallback;

    public bool HasItem => CurrentItem != null;

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    protected virtual void OnEnable()
    {
        BindDisplayName(CurrentItem, ResolveItemName(CurrentItem));
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    protected virtual void OnDisable()
    {
        UnbindDisplayName();
    }

    // Purpose: Initializes initialize for use by this component.
    public virtual void Initialize(SlotHUD owner)
    {
        Owner = owner;
    }

    // Purpose: Clears the current visual state.
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

        UnbindDisplayName();

        SetSelected(false);
    }

    // Purpose: Updates the item visual state.
    protected void SetItemVisual(ItemSO item)
    {
        SetItemVisual(
            item,
            item != null ? item.PreviewImage : null,
            ResolveItemName(item));
    }

    // Purpose: Updates the item visual state.
    protected void SetItemVisual(ItemSO item, Sprite icon, string label)
    {
        CurrentItem = item;

        if (_iconImage != null)
        {
            _iconImage.sprite = icon;
            _iconImage.enabled = icon != null;
        }

        BindDisplayName(item, label);

    }

    // Purpose: Updates the key label state.
    protected void SetKeyLabel(string keyLabel)
    {
        if (_keyText != null)
            _keyText.text = keyLabel ?? string.Empty;
    }

    /// <summary>
    /// Replaces the localized item-name binding with a runtime-composed label.
    /// Generic quick slots use this for ammo counts, usable stacks, and durability.
    /// </summary>
    protected void SetStaticLabel(string label)
    {
        UnbindDisplayName();

        if (_labelText != null)
            _labelText.text = label ?? string.Empty;
    }

    // Purpose: Updates the selected state.
    protected void SetSelected(bool selected)
    {
        if (_selectedOutline != null)
            _selectedOutline.enabled = selected;
    }



    // Purpose: Responds to the pointer click callback.
    public virtual void OnPointerClick(PointerEventData eventData)
    {
    }

    // Purpose: Responds to the begin drag callback.
    public virtual void OnBeginDrag(PointerEventData eventData)
    {
    }

    // Purpose: Responds to the end drag callback.
    public virtual void OnEndDrag(PointerEventData eventData)
    {
    }

    // Purpose: Responds to the drop callback.
    public virtual void OnDrop(PointerEventData eventData)
    {
    }

    // Purpose: Resolves the item name needed by this component.
    protected static string ResolveItemName(ItemSO item)
    {
        return InventoryItemDisplayUtility.ResolveItemName(item);
    }

    // Purpose: Binds display name to the active runtime or UI target.
    private void BindDisplayName(ItemSO item, string fallbackLabel)
    {
        UnbindDisplayName();

        if (_labelText == null)
            return;

        _boundItemNameFallback = fallbackLabel ?? InventoryItemDisplayUtility.ResolveItemName(item);
        _labelText.text = _boundItemNameFallback;

        if (!InventoryItemDisplayUtility.TryGetItemNameReference(item, out _boundItemName))
            return;

        _boundItemName.StringChanged += HandleDisplayNameChanged;
        _boundItemName.RefreshString();
    }

    // Purpose: Removes the display name binding.
    private void UnbindDisplayName()
    {
        if (_boundItemName != null)
            _boundItemName.StringChanged -= HandleDisplayNameChanged;

        _boundItemName = null;
    }

    // Purpose: Handles the incoming display name changed request or event.
    private void HandleDisplayNameChanged(string value)
    {
        if (_labelText == null)
            return;

        _labelText.text = string.IsNullOrWhiteSpace(value)
            ? _boundItemNameFallback ?? string.Empty
            : value;
    }
}
