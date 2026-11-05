/*
 * Role hierarchy: Gameplay > Items > World
 * Primary responsibility: Handles WorldItemPickup interaction, pickup, trade, loot, or storage flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

/// <summary>
/// Shared runtime wrapper for every floor item. The wrapper owns interaction and
/// pickup behaviour; ItemSO only supplies the visual instantiated below VisualRoot.
/// </summary>
[DisallowMultipleComponent]
public sealed class WorldItemPickup : MonoBehaviour
{
    private bool _awardsDiscovery=true; // Authored field items count; spawned player drops explicitly opt out.
    public void SetDiscoveryEligibility(bool eligible)=>_awardsDiscovery=eligible;
    [Header("Visual")]
    [Tooltip("Child transform that receives ItemSO.WorldVisualPrefab. Keep this separate from the trigger collider and prompt anchor.")]
    [SerializeField] private Transform _visualRoot;

    [Tooltip("Disables colliders and rigidbodies found in the spawned visual. Item visuals are display-only inside this shared pickup wrapper.")]
    [SerializeField] private bool _disableVisualPhysics = true;

    [Header("Item Runtime")]
    [SerializeField, ReadOnly] private ItemSO _item;
    [SerializeField, Min(1), ReadOnly] private int _amount = 1;
    [SerializeField, Min(1), ReadOnly] private int _bulletBundleAmount = 20;
    [SerializeField, Range(0f, 1f), ReadOnly] private float _bulletSellPriceRate;
    [SerializeField, ReadOnly] private float _armorDurability = -1f;
    [SerializeField, ReadOnly] private float _armorMaxDurability = -1f;
    [SerializeField, ReadOnly] private float _weaponCurrentDurability = -1f;
    [SerializeField, ReadOnly] private float _weaponMaxDurability = -1f;
    [SerializeField, ReadOnly] private float _gasMaskFilterDurability = -1f;
    [SerializeField, ReadOnly] private float _gasMaskMaxFilterDurability = -1f;

    [Header("Pickup")]
    [SerializeField] private bool _destroyOnPickup = true;
    [SerializeField] private bool _logPickupFailures = true;

    private GameObject _visualInstance;
    private VisionDiscoveredVisibilityTarget _discoveryVisibility;
    private bool _pickedUp;
    private bool _interactionEnabled = true;

    public event Action<WorldItemPickup> PickedUp;

    public ItemSO Item => _item;
    public int Amount => Mathf.Max(1, _amount);
    public float ArmorDurability => _armorDurability;
    public float ArmorMaxDurability => _armorMaxDurability;
    public float WeaponCurrentDurability => _weaponCurrentDurability;
    public float WeaponMaxDurability => _weaponMaxDurability;
    public float GasMaskFilterDurability => _gasMaskFilterDurability;
    public float GasMaskMaxFilterDurability => _gasMaskMaxFilterDurability;
    public bool CanInteract => _interactionEnabled && !_pickedUp && _item != null;
    public bool ShouldShowAmount =>
        _item != null &&
        Amount > 1 &&
        (_item.MaxStack > 1 || _item is CurrencyItemSO);
    public string DisplayLabel => ResolveDisplayLabel();

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        if (_item != null)
            RefreshVisual();
    }

    // Purpose: Initializes initialize for use by this component.
    public void Initialize(ItemSO item)
    {
        Initialize(item, 1, 20, 0f, -1f);
    }

    // Purpose: Initializes initialize for use by this component.
    public void Initialize(
        ItemSO item,
        int amount,
        int bulletBundleAmount,
        float bulletSellPriceRate)
    {
        Initialize(item, amount, bulletBundleAmount, bulletSellPriceRate, -1f);
    }

    // Purpose: Initializes initialize for use by this component.
    public void Initialize(
        ItemSO item,
        int amount,
        int bulletBundleAmount,
        float bulletSellPriceRate,
        float armorDurability)
    {
        Initialize(
            item,
            amount,
            bulletBundleAmount,
            bulletSellPriceRate,
            armorDurability,
            -1f,
            -1f);
    }

    // Purpose: Initializes initialize for use by this component.
    public void Initialize(
        ItemSO item,
        int amount,
        int bulletBundleAmount,
        float bulletSellPriceRate,
        float armorDurability,
        float weaponCurrentDurability,
        float weaponMaxDurability)
    {
        Initialize(
            item,
            amount,
            bulletBundleAmount,
            bulletSellPriceRate,
            armorDurability,
            weaponCurrentDurability,
            weaponMaxDurability,
            -1f);
    }

    // Purpose: Initializes world pickup condition data for armor, weapons, and gas masks.
    public void Initialize(
        ItemSO item,
        int amount,
        int bulletBundleAmount,
        float bulletSellPriceRate,
        float armorDurability,
        float weaponCurrentDurability,
        float weaponMaxDurability,
        float gasMaskFilterDurability)
    {
        Initialize(
            item,
            amount,
            bulletBundleAmount,
            bulletSellPriceRate,
            armorDurability,
            weaponCurrentDurability,
            weaponMaxDurability,
            gasMaskFilterDurability,
            -1f,
            -1f);
    }

    // Purpose: Initializes a world pickup while retaining reduced maximum condition for armor and gas masks.
    public void Initialize(
        ItemSO item,
        int amount,
        int bulletBundleAmount,
        float bulletSellPriceRate,
        float armorDurability,
        float weaponCurrentDurability,
        float weaponMaxDurability,
        float gasMaskFilterDurability,
        float armorMaxDurability,
        float gasMaskMaxFilterDurability)
    {
        _item = PassiveInstanceRuntime.Create(item);
        _amount = Mathf.Max(1, amount);
        _bulletBundleAmount = Mathf.Max(1, bulletBundleAmount);
        _bulletSellPriceRate = Mathf.Clamp01(bulletSellPriceRate);
        if (item is ArmorItemSO armor)
        {
            _armorMaxDurability = Mathf.Clamp(
                armorMaxDurability >= 0f ? armorMaxDurability : armor.MaxDurability,
                0f,
                armor.MaxDurability);
            _armorDurability = Mathf.Clamp(
                armorDurability >= 0f ? armorDurability : _armorMaxDurability,
                0f,
                _armorMaxDurability);
        }
        else
        {
            _armorDurability = -1f;
            _armorMaxDurability = -1f;
        }

        if (item is RangedWeaponSO rangedWeapon)
        {
            _weaponMaxDurability = weaponMaxDurability > 0f
                ? weaponMaxDurability
                : rangedWeapon.MaxDurability;
            _weaponCurrentDurability = Mathf.Clamp(
                weaponCurrentDurability >= 0f ? weaponCurrentDurability : _weaponMaxDurability,
                0f,
                _weaponMaxDurability);
        }
        else if (item is ConsumableItemSO medical && medical.UsesDurability)
        {
            _weaponMaxDurability = weaponMaxDurability > 0f ? Mathf.Min(weaponMaxDurability, medical.MaxDurability) : medical.MaxDurability;
            _weaponCurrentDurability = Mathf.Clamp(weaponCurrentDurability >= 0f ? weaponCurrentDurability : _weaponMaxDurability, 0f, _weaponMaxDurability);
        }
        else
        {
            _weaponCurrentDurability = -1f;
            _weaponMaxDurability = -1f;
        }

        if (item is GasMaskItemSO gasMask)
        {
            _gasMaskMaxFilterDurability = Mathf.Clamp(
                gasMaskMaxFilterDurability >= 0f
                    ? gasMaskMaxFilterDurability
                    : gasMask.MaxFilterDurability,
                0f,
                gasMask.MaxFilterDurability);
            _gasMaskFilterDurability = Mathf.Clamp(
                gasMaskFilterDurability >= 0f
                    ? gasMaskFilterDurability
                    : _gasMaskMaxFilterDurability,
                0f,
                _gasMaskMaxFilterDurability);
        }
        else
        {
            _gasMaskFilterDurability = -1f;
            _gasMaskMaxFilterDurability = -1f;
        }
        _pickedUp = false;
        _interactionEnabled = true;

        RefreshVisual();
    }

    // Purpose: Updates the interaction enabled state.
    public void SetInteractionEnabled(bool enabled)
    {
        _interactionEnabled = enabled;
    }

    // Purpose: Performs the mark discovered workflow for this component.
    public void MarkDiscovered()
    {
        if (_discoveryVisibility == null)
        {
            _discoveryVisibility = GetComponent<VisionDiscoveredVisibilityTarget>() ??
                gameObject.AddComponent<VisionDiscoveredVisibilityTarget>();
            _discoveryVisibility.RefreshRenderers();
        }

        _discoveryVisibility.MarkDiscovered();
    }

    // Purpose: Attempts to pickup and reports whether the request succeeded.
    public bool TryPickup(Component picker)
    {
        if (!CanInteract || picker == null)
            return false;

        PlayerInventoryRuntime inventory =
            EntityComponentResolver.Resolve<PlayerInventoryRuntime>(picker);

        if (inventory == null)
        {
            LogPickupFailure("PlayerInventoryRuntime could not be resolved from the picker entity root.");
            return false;
        }

        bool picked;
        string message;

        BackpackWorldPickupPayload backpackPayload = GetComponent<BackpackWorldPickupPayload>();

        if (backpackPayload != null)
        {
            picked = backpackPayload.TryEquipInto(inventory, out message);
        }
        else if (_item is ArmorItemSO armor && _armorDurability >= 0f)
        {
            picked = inventory.TryPickupArmorLoot(
                armor,
                _armorDurability,
                _armorMaxDurability,
                out message);
        }
        else if (_item is WeaponSO weapon && weapon is RangedWeaponSO)
        {
            picked = inventory.TryPickupWeaponLoot(
                weapon,
                _amount,
                _weaponCurrentDurability,
                _weaponMaxDurability,
                out message);
        }
        else if (_item is ConsumableItemSO medical && medical.UsesDurability)
        {
            picked = inventory.TryPickupMedicalCondition(medical, _amount,
                _weaponCurrentDurability, _weaponMaxDurability, out message);
        }
        else if (_item is GasMaskItemSO gasMask)
        {
            picked = inventory.TryPickupGasMaskLoot(
                gasMask,
                _gasMaskFilterDurability,
                _gasMaskMaxFilterDurability,
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
        if(GameProgressionRuntime.InRaid)
            EntityComponentResolver.Resolve<PlayerShooterInfection>(inventory)?.AddExposure(InfectionAggravationStatusSO.Active?.Settings.itemPickupExposure??0);
        if(_awardsDiscovery&&GameProgressionRuntime.InRaid)
            GameProgressionRuntime.Discover("pickup:"+GameProgressionRuntime.Identity(this),ExperienceRulesSO.Active?.experience.Discovery(_item.Rarity)??0);
        PickedUp?.Invoke(this);

        if (_destroyOnPickup)
            Destroy(gameObject);

        return true;
    }

    // Purpose: Refreshes the displayed or cached visual.
    private void RefreshVisual()
    {
        ClearVisual();

        if (_item == null)
            return;

        if (_visualRoot == null)
        {
            LogPickupFailure("VisualRoot is missing. Create a VisualRoot child on ItemPickup and assign it.");
            return;
        }

        GameObject visualPrefab = _item.WorldVisualPrefab;

        if (visualPrefab == null)
        {
            LogPickupFailure($"World visual prefab is missing. item={_item.name}");
            return;
        }

        _visualInstance = Instantiate(visualPrefab, _visualRoot);
        _visualInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        _visualInstance.transform.localScale = Vector3.one;

        if (_disableVisualPhysics)
            DisableVisualPhysics(_visualInstance);

        if (_discoveryVisibility == null)
        {
            _discoveryVisibility = GetComponent<VisionDiscoveredVisibilityTarget>() ??
                gameObject.AddComponent<VisionDiscoveredVisibilityTarget>();
        }

        _discoveryVisibility.enabled = true;
        _discoveryVisibility.RefreshRenderers();
    }

    // Purpose: Clears the current visual state.
    private void ClearVisual()
    {
        if (_visualInstance == null)
            return;

        if (Application.isPlaying)
            Destroy(_visualInstance);
        else
            DestroyImmediate(_visualInstance);

        _visualInstance = null;
    }

    // Purpose: Disables visual physics for this component.
    private static void DisableVisualPhysics(GameObject visualInstance)
    {
        Collider[] colliders = visualInstance.GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = false;
        }

        Rigidbody[] rigidbodies = visualInstance.GetComponentsInChildren<Rigidbody>(true);

        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Rigidbody body = rigidbodies[i];

            if (body == null)
                continue;

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            body.detectCollisions = false;
        }
    }

    // Purpose: Performs the log pickup failure workflow for this component.
    private void LogPickupFailure(string message)
    {
        if (!_logPickupFailures)
            return;

        Debug.LogWarning($"[WorldItemPickup] {message}", this);
    }

    // Purpose: Resolves the display label needed by this component.
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
