using System;
using UnityEngine;

[DisallowMultipleComponent]
public class ItemWorldPickup : MonoBehaviour
{
    [Header("Item")]
    [SerializeField] private ItemSO _item;
    [SerializeField, Min(1)] private int _amount = 1;
    [SerializeField, Min(1)] private int _bulletBundleAmount = 20;
    [SerializeField, Range(0f, 1f)] private float _bulletSellPriceRate = 0f;
    [SerializeField] private float _armorDurability = -1f;

    [Header("Pickup")]
    [SerializeField] private bool _destroyOnPickup = true;
    [SerializeField] private bool _logPickupFailures = true;

    private bool _pickedUp;

    public event Action<ItemWorldPickup> PickedUp;

    public ItemSO Item => _item;
    public int Amount => Mathf.Max(1, _amount);
    public float ArmorDurability => _armorDurability;
    public bool CanInteract => !_pickedUp && _item != null;
    public bool ShouldShowAmount => _item != null && _item.MaxStack > 1 && Amount > 1;
    public string DisplayLabel => ResolveDisplayLabel();

    public void Initialize(ItemSO item)
    {
        Initialize(item, 1, 20, 0f, -1f);
    }

    public void Initialize(
        ItemSO item,
        int amount,
        int bulletBundleAmount,
        float bulletSellPriceRate)
    {
        Initialize(item, amount, bulletBundleAmount, bulletSellPriceRate, -1f);
    }

    public void Initialize(
        ItemSO item,
        int amount,
        int bulletBundleAmount,
        float bulletSellPriceRate,
        float armorDurability)
    {
        _item = item;
        _amount = Mathf.Max(1, amount);
        _bulletBundleAmount = Mathf.Max(1, bulletBundleAmount);
        _bulletSellPriceRate = Mathf.Clamp01(bulletSellPriceRate);
        _armorDurability = item is ArmorItemSO ? armorDurability : -1f;
        _pickedUp = false;
    }

    public bool TryPickup(Component picker)
    {
        if (!CanInteract || picker == null)
            return false;

        PlayerInventoryRuntime inventory =
            picker.GetComponentInParent<PlayerInventoryRuntime>();

        if (inventory == null)
        {
            LogPickupFailure("PlayerInventoryRuntime is missing on picker hierarchy.");
            return false;
        }

        bool picked;
        string message;

        if (_item is ArmorItemSO armor && _armorDurability >= 0f)
        {
            picked = inventory.TryPickupArmorLoot(
                armor,
                _armorDurability,
                out message);
        }
        else
        {
            picked = inventory.TryPickup(
                _item,
                _amount,
                _bulletBundleAmount,
                _bulletSellPriceRate,
                out message);
        }

        if (!picked)
        {
            LogPickupFailure(message);
            return false;
        }

        _pickedUp = true;
        PickedUp?.Invoke(this);

        if (_destroyOnPickup)
            Destroy(gameObject);

        return true;
    }

    private void LogPickupFailure(string message)
    {
        if (!_logPickupFailures)
            return;

        Debug.LogWarning($"[ItemWorldPickup] {message}", this);
    }

    private string ResolveDisplayLabel()
    {
        string itemName = InventoryItemDisplayUtility.ResolveItemName(_item);

        if (string.IsNullOrWhiteSpace(itemName))
            itemName = "Item";

        if (!ShouldShowAmount)
            return itemName;

        return $"{itemName} x{Amount}";
    }
}
