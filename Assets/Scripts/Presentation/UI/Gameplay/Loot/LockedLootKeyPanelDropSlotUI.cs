/*
 * Role hierarchy: Presentation > UI > Gameplay > Loot
 * Primary responsibility: Receives a Key Tool tier drag into the locked-loot
 * socket without participating in the general inventory drag context.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class LockedLootKeyPanelDropSlotUI : MonoBehaviour, IDropHandler
{
    private LockedLootKeyPanel _owner;

    // Purpose: Binds the one locked-loot overlay that owns this socket.
    public void Configure(LockedLootKeyPanel owner)
    {
        _owner = owner;
    }

    // Purpose: Selects the currently dragged Key Tool tier in the socket.
    public void OnDrop(PointerEventData eventData)
    {
        _owner?.TrySubmitDraggedTier();
    }
}
