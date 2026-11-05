/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Visuals
 * Primary responsibility: Selects one complete visual look without consuming gameplay randomness.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ShooterCustomizePool",
    menuName = "Game/Visual Customization/Shooter Customize Pool")]
public sealed class ShooterCustomizePoolSO : ScriptableObject
{
    [Tooltip("Complete looks. Null entries and entries with zero weight are never selected.")]
    [SerializeField] private List<ShooterCustomizeSO> _customizes = new List<ShooterCustomizeSO>();
    // Keep the original reference list intact: old pools have implicit equal weights.
    [SerializeField] private List<float> _weights = new List<float>();
    private static readonly System.Random AppearanceRandom = new System.Random();

    public IReadOnlyList<ShooterCustomizeSO> Customizes => _customizes;

    public float WeightAt(int index)
    {
        if (_customizes == null || index < 0 || index >= _customizes.Count || _customizes[index] == null) return 0;
        float value = _weights != null && index < _weights.Count ? _weights[index] : 1;
        return float.IsNaN(value) || float.IsInfinity(value) ? 0 : Mathf.Max(0, value);
    }

    public double TotalWeight
    {
        get { double total = 0; for (int i = 0; i < (_customizes?.Count ?? 0); i++) total += WeightAt(i); return total; }
    }

    // Appearance has its own random stream: adding a look must not change weapon/loot rolls.
    public ShooterCustomizeSO GetRandom()
    {
        return Pick(AppearanceRandom.NextDouble());
    }

    /// <summary>Pure weighted selection. Exposed for deterministic preview and boundary tests.</summary>
    public ShooterCustomizeSO Pick(double unitSample)
    {
        double total = TotalWeight;
        if (total <= 0 || double.IsNaN(unitSample)) return null;
        double cursor = System.Math.Max(0, System.Math.Min(1, unitSample)) * total;
        ShooterCustomizeSO last = null;
        for (int i = 0; i < _customizes.Count; i++)
        {
            float weight = WeightAt(i);
            if (weight <= 0) continue;
            last = _customizes[i];
            if (cursor < weight) return last;
            cursor -= weight;
        }
        return last;
    }

    public void SetEntries(IReadOnlyList<ShooterCustomizeSO> looks, IReadOnlyList<float> weights)
    {
        _customizes = looks != null ? new List<ShooterCustomizeSO>(looks) : new List<ShooterCustomizeSO>();
        _weights = new List<float>();
        for (int i = 0; i < _customizes.Count; i++)
            _weights.Add(weights != null && i < weights.Count ? weights[i] : 1);
    }
}
