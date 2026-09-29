/*
 * Role hierarchy: Presentation > UI > Gameplay > PanelHub > Inventory
 * Primary responsibility: Manages InventoryPanelUIBase item storage, equipment, transfer state, and related rules.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public abstract class InventoryPanelUIBase : PanelUI
{
}
