/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Visuals
 * Primary responsibility: Holds selectable shooter customization looks for a future enemy spawn pool.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ShooterCustomizePool",
    menuName = "Game/Visual Customization/Shooter Customize Pool")]
public sealed class ShooterCustomizePoolSO : ScriptableObject
{
    [Tooltip("Spawn systems can select one of these complete visual looks. This asset does not change spawn behavior by itself.")]
    [SerializeField] private List<ShooterCustomizeSO> _customizes = new List<ShooterCustomizeSO>();

    public IReadOnlyList<ShooterCustomizeSO> Customizes => _customizes;

    // Purpose: Returns the current random.
    public ShooterCustomizeSO GetRandom()
    {
        return _customizes == null || _customizes.Count == 0
            ? null
            : _customizes[Random.Range(0, _customizes.Count)];
    }
}
