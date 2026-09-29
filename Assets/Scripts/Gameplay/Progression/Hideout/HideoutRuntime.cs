/*
 * Role hierarchy: Gameplay > Progression > Hideout
 * Primary responsibility: Marks the persistent player hub that exists outside generated stage maps.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public sealed class HideoutRuntime : MonoBehaviour
{
    [Tooltip("The SectorRuntime used only as the Hideout's spawn, camera, and interaction anchor. It is intentionally not registered as a generated stage sector.")]
    [SerializeField] private SectorRuntime _sector;

    public SectorRuntime Sector => _sector != null ? _sector : GetComponent<SectorRuntime>();

    // Purpose: Initializes initialize for use by this component.
    public void Initialize(SectorRuntime sector)
    {
        _sector = sector;
    }
}
