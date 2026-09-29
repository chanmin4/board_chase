/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Core
 * Primary responsibility: Manages ShooterLoadoutTableEntry item storage, equipment, transfer state, and related rules.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[Serializable]
public sealed class ShooterLoadoutTableEntry
{
    public ShooterLoadoutSO loadout;
    [Min(0f)] public float weight = 1f;
}

[CreateAssetMenu(
    fileName = "ShooterLoadoutTable",
    menuName = "Game/Shooter/Loadout Table")]
public class ShooterLoadoutTableSO : ScriptableObject
{
    [SerializeField] private ShooterLoadoutTableEntry[] _entries;

    // Purpose: Attempts to roll and reports whether the request succeeded.
    public bool TryRoll(out ShooterLoadoutSnapshot snapshot)
    {
        ShooterLoadoutSO loadout = PickLoadout();

        if (loadout == null)
        {
            snapshot = default;
            return false;
        }

        snapshot = loadout.Roll();
        return true;
    }

    // Purpose: Performs the pick loadout workflow for this component.
    public ShooterLoadoutSO PickLoadout()
    {
        if (_entries == null || _entries.Length == 0)
            return null;

        float totalWeight = 0f;

        for (int i = 0; i < _entries.Length; i++)
        {
            ShooterLoadoutTableEntry entry = _entries[i];

            if (entry == null || entry.loadout == null || entry.weight <= 0f)
                continue;

            totalWeight += entry.weight;
        }

        if (totalWeight <= 0f)
            return null;

        float roll = UnityEngine.Random.Range(0f, totalWeight);

        for (int i = 0; i < _entries.Length; i++)
        {
            ShooterLoadoutTableEntry entry = _entries[i];

            if (entry == null || entry.loadout == null || entry.weight <= 0f)
                continue;

            roll -= entry.weight;

            if (roll <= 0f)
                return entry.loadout;
        }

        return null;
    }
}
