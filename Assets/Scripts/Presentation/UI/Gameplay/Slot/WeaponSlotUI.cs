/*
 * Role hierarchy: Presentation > UI > Gameplay > Slot
 * Primary responsibility: Presents WeaponSlotUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

// WeaponSlotUI.cs
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class WeaponSlotUI : SlotUI
{
    [Header("Slot")]
    [SerializeField, Min(0)] private int _weaponSlotIndex;

    [Header("Magazine HUD")]
    [Tooltip("Shared Magazine prefab used by ammo slots. It is shown only for the active ranged weapon.")]
    [SerializeField] private GameObject _magazinePrefab;

    private GameObject _runtimeMagazine;
    private TextMeshProUGUI _currentMagazineText;
    private TextMeshProUGUI _reserveMagazineText;

    public int WeaponSlotIndex => _weaponSlotIndex;

    // Purpose: Initializes initialize for use by this component.
    public void Initialize(SlotHUD owner, int weaponSlotIndex)
    {
        base.Initialize(owner);
        _weaponSlotIndex = Mathf.Max(0, weaponSlotIndex);
    }

    // Purpose: Binds bind to the active runtime or UI target.
    public void Bind(
        WeaponSO weapon,
        int amount,
        float currentDurability,
        float maxDurability,
        bool isSelected,
        string keyLabel,
        bool showMagazine,
        bool hasMagazineAmmo,
        int currentMagazineAmmo,
        int reserveAmmo)
    {
        SetItemVisual(weapon);
        SetStaticLabel(BuildWeaponLabel(
            weapon,
            amount,
            currentDurability,
            maxDurability));
        SetKeyLabel(keyLabel);
        SetSelected(isSelected);
        RefreshMagazine(
            showMagazine,
            hasMagazineAmmo,
            currentMagazineAmmo,
            reserveAmmo);
    }

    // Purpose: Refreshes the displayed or cached magazine.
    private void RefreshMagazine(
        bool visible,
        bool hasMagazineAmmo,
        int currentMagazineAmmo,
        int reserveAmmo)
    {
        if (!visible)
        {
            if (_runtimeMagazine != null)
                _runtimeMagazine.SetActive(false);

            return;
        }

        EnsureMagazine();

        if (_runtimeMagazine == null)
            return;

        _runtimeMagazine.SetActive(true);

        if (_currentMagazineText != null)
        {
            _currentMagazineText.text = hasMagazineAmmo
                ? Mathf.Max(0, currentMagazineAmmo).ToString()
                : "-";
        }

        if (_reserveMagazineText != null)
        {
            _reserveMagazineText.text = hasMagazineAmmo
                ? Mathf.Max(0, reserveAmmo).ToString()
                : "-";
        }
    }

    // Purpose: Ensures magazine is ready before the workflow continues.
    private void EnsureMagazine()
    {
        if (_runtimeMagazine != null || _magazinePrefab == null)
            return;

        _runtimeMagazine = Instantiate(_magazinePrefab, transform);
        _runtimeMagazine.name = "WeaponMagazine";

        if (_runtimeMagazine.transform is RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -4f);
            rect.localScale = Vector3.one;
        }

        TextMeshProUGUI[] texts = _runtimeMagazine.GetComponentsInChildren<TextMeshProUGUI>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            TextMeshProUGUI text = texts[i];

            if (text.name == "CurrentMagazineText")
                _currentMagazineText = text;
            else if (text.name == "ReserveMagazineText")
                _reserveMagazineText = text;
        }
    }

    // Purpose: Builds the weapon label from the current authored data.
    private static string BuildWeaponLabel(
        WeaponSO weapon,
        int amount,
        float currentDurability,
        float maxDurability)
    {
        if (weapon == null)
            return string.Empty;

        string itemName = ResolveItemName(weapon);

        if (weapon is RangedWeaponSO && maxDurability > 0.001f)
        {
            return $"{itemName}\n{Mathf.CeilToInt(currentDurability)}/" +
                   $"{Mathf.CeilToInt(maxDurability)}";
        }

        return amount > 1 ? $"{itemName}\nx{amount}" : itemName;
    }

    // Purpose: Responds to the pointer click callback.
    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null)
            return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (eventData.clickCount >= 2 && HasItem)
                Owner?.RequestUnequipEquipmentSlot(
                    PlayerInventoryEquipmentSlotKind.Weapon,
                    _weaponSlotIndex);
            else
                Owner?.RequestActivateWeaponSlot(_weaponSlotIndex);
        }
        else if (eventData.button == PointerEventData.InputButton.Right && HasItem)
        {
            Owner?.RequestUnequipEquipmentSlot(
                PlayerInventoryEquipmentSlotKind.Weapon,
                _weaponSlotIndex);
        }
    }

    // Purpose: Responds to the begin drag callback.
    public override void OnBeginDrag(PointerEventData eventData)
    {
        if (!HasItem)
            return;

        PlayerInventoryDragContext.BeginEquipmentDrag(
            PlayerInventoryEquipmentSlotKind.Weapon,
            _weaponSlotIndex);
    }

    // Purpose: Responds to the end drag callback.
    public override void OnEndDrag(PointerEventData eventData)
    {
        PlayerInventoryDragContext.Clear();
    }

    // Purpose: Responds to the drop callback.
    public override void OnDrop(PointerEventData eventData)
    {
        if (!PlayerInventoryDragContext.HasPayload)
            return;

        Owner?.RequestHandleWeaponSlotDrop(_weaponSlotIndex);

        PlayerInventoryDragContext.Clear();
    }
}
