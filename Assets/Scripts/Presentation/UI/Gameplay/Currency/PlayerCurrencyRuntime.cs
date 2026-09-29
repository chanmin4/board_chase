/*
 * Role hierarchy: WIP > Legacy > NumericCurrency
 * Primary responsibility: Preserves old prefab serialization without participating in active gameplay.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
[DisallowMultipleComponent]
public class PlayerCurrencyRuntime : MonoBehaviour
{
    [Header("WIP Legacy Numeric Currency")]
    [Tooltip("Compatibility-only component. The active economy is CurrencyItemSO stacks in PlayerInventoryRuntime; do not add new references to this component.")]
    [SerializeField, HideInInspector] private int _roguelikeCurrency;
    [SerializeField, HideInInspector] private int _runCurrency;

    /// <summary>
    /// Compatibility-only value for the retired meta-currency system. New
    /// gameplay must use physical CurrencyItemSO stacks through inventory
    /// transactions instead.
    /// </summary>
    public int RoguelikeCurrency => 0;
    public int RunCurrency => 0;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ClearLegacyCurrency();
    }

    // Purpose: Performs the reset run currency for new run workflow for this component.
    public void ResetRunCurrencyForNewRun()
    {
        ClearLegacyCurrency();
    }

    /// <summary>
    /// Restores run currency from the resumable run profile. Roguelike/meta
    /// currency continues to be owned by SaveSystem separately.
    /// </summary>
    public void RestoreRunCurrencyForPersistence(int amount)
    {
        ClearLegacyCurrency();
    }

    // Purpose: Retains the old public API while permanent meta currency is WIP.
    public void LoadRoguelikeCurrencyFromSaveSystem()
    {
        ClearLegacyCurrency();
    }

    // Purpose: Retains the old public API without writing the retired WIP currency.
    public void WriteRoguelikeCurrencyToSaveSystem(bool saveToDisk)
    {
        ClearLegacyCurrency();
    }

    // Purpose: Adds the requested currency to this component.
    public bool AddCurrency(PlayerCurrencyType type, int amount)
    {
        return false;
    }

    // Purpose: Checks whether spend is currently permitted.
    public bool CanSpend(PlayerCurrencyType type, int amount)
    {
        return amount <= 0;
    }

    // Purpose: Attempts to spend currency and reports whether the request succeeded.
    public bool TrySpendCurrency(PlayerCurrencyType type, int amount)
    {
        return amount <= 0;
    }

    // Purpose: Returns the current currency.
    public int GetCurrency(PlayerCurrencyType type)
    {
        return 0;
    }

    // Purpose: Updates the currency state.
    public void SetCurrency(PlayerCurrencyType type, int amount)
    {
        ClearLegacyCurrency();
    }

    // Purpose: Clears retained serialized fields so old profiles cannot revive the retired numeric economy.
    private void ClearLegacyCurrency()
    {
        _roguelikeCurrency = 0;
        _runCurrency = 0;
    }
}
