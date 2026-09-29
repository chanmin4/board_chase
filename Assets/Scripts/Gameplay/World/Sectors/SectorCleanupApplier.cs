/*
 * Role hierarchy: Gameplay > World > Sectors
 * Primary responsibility: Manages SectorCleanupApplier stage, sector, navigation, spawn, or spatial progression state.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

// Retains the existing script GUID for old serialized data. The automatic combat
// cleanup/coating system was removed; stage map replacement owns sector lifetime.
[AddComponentMenu("")]
public sealed class SectorCleanupApplier : MonoBehaviour
{
}
