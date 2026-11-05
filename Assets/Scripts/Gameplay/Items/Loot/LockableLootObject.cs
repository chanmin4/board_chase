/*
 * Role hierarchy: Gameplay > Items > Loot
 * Primary responsibility: Gates one authored LootableObject behind one
 * Key Tool tier without duplicating the existing LootHub transfer pipeline.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

/// <summary>
/// Optional lock for an authored <see cref="LootableObject"/>. Add this only
/// to containers intended to be locked; ordinary crates retain their current
/// behaviour. A correct key spends one durability use only after the loot roll
/// is confirmed ready, then opens the normal LootHub.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(LootableObject))]
public sealed class LockableLootObject : MonoBehaviour
{
    [Header("Lock")]
    [SerializeField] private bool _startsLocked = true;
    [SerializeField] private LootKeyTier _requiredKeyTier = LootKeyTier.Normal;
    [SerializeField] private string _lockedDisplayName = "Locked Loot";

    [Header("Locked Loot Reward")]
    [Tooltip("Optional key-tier profile. Slot-based loot uses the box's own filled-slot range and rarity weights; this profile's legacy bonus rolls / rarity multipliers are not applied.")]
    [SerializeField] private LockedLootProfileSO _lockedLootProfile;

    [Header("References")]
    [SerializeField] private LootableObject _lootableObject;

    [Header("Runtime - Read Only")]
    [ReadOnly] [SerializeField] private bool _isUnlocked;

    public bool IsLocked => _startsLocked && !_isUnlocked;
    public void RestoreRaidUnlocked(bool unlocked) => _isUnlocked = unlocked;
    public LootKeyTier RequiredKeyTier => _lockedLootProfile != null
        ? _lockedLootProfile.RequiredKeyTier
        : _requiredKeyTier;
    public LockedLootProfileSO LockedLootProfile => _lockedLootProfile;
    public string LockedDisplayName => string.IsNullOrWhiteSpace(_lockedDisplayName)
        ? name
        : _lockedDisplayName;

    // Purpose: Resolves the paired authored loot component before interaction.
    private void Awake()
    {
        ResolveRefs();
    }

    // Purpose: Keeps the legacy tier field aligned when a reusable profile is assigned in Inspector.
    private void OnValidate()
    {
        if (_lockedLootProfile != null)
            _requiredKeyTier = _lockedLootProfile.RequiredKeyTier;
    }

    // Purpose: Opens the one shared Key Tool overlay for a still-locked object.
    public bool TryInteract(Component actor)
    {
        ResolveRefs();

        if (!IsLocked)
            return _lootableObject != null && _lootableObject.TryOpenLootHub(actor);

        PlayerInventoryRuntime inventory =
            EntityComponentResolver.Resolve<PlayerInventoryRuntime>(actor);
        if (inventory == null)
        {
            Debug.LogWarning(
                $"[LockableLootObject] Key Tool interaction rejected because no player inventory was resolved. object={name}",
                this);
            return false;
        }

        LockedLootKeyPanel.EnsureInstance();
        return LockedLootKeyPanel.TryOpen(this, actor, inventory);
    }

    /// <summary>
    /// Validates one UI key submission and commits its one use exactly once.
    /// The current object is unlocked only for this runtime session; ES3
    /// persistence is intentionally added in the dedicated save stage.
    /// </summary>
    public bool TryUnlockAndOpen(
        Component actor,
        PlayerInventoryRuntime inventory,
        LootKeyTier submittedTier,
        out string message)
    {
        ResolveRefs();

        if (!IsLocked)
        {
            message = "This container is already unlocked.";
            return _lootableObject != null && _lootableObject.TryOpenLootHub(actor);
        }

        if (submittedTier != _requiredKeyTier)
        {
            message = $"Requires a {_requiredKeyTier} Key.";
            return false;
        }

        if (inventory == null || !inventory.HasUsableKey(submittedTier))
        {
            message = $"No usable {submittedTier} Key is stored in the Key Tool.";
            return false;
        }

        // Roll first. A malformed/missing loot source must never burn a key.
        if (_lootableObject == null || !_lootableObject.TryPrepareLoot())
        {
            message = "The container loot is not ready. Key was not used.";
            return false;
        }

        _isUnlocked = true;
        if (!inventory.TryConsumeKeyUse(submittedTier, out message))
        {
            _isUnlocked = false;
            return false;
        }

        if (_lootableObject.TryOpenPreparedLoot(actor))
        {
            message = $"{submittedTier} Key accepted. {LockedDisplayName} unlocked.";
            return true;
        }

        // The preparation check above guarantees this path only occurs when a
        // scene endpoint disappears in the same frame. Keep its committed
        // state visible rather than silently allowing a second key use.
        message = $"{LockedDisplayName} unlocked, but LootHub could not be opened.";
        Debug.LogError($"[LockableLootObject] {message} object={name}", this);
        return false;
    }

    // Purpose: Resolves the required authored loot component.
    private void ResolveRefs()
    {
        if (_lootableObject == null)
            _lootableObject = GetComponent<LootableObject>();
    }
}
