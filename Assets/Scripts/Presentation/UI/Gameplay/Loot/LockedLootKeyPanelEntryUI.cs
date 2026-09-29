/*
 * Role hierarchy: Presentation > UI > Gameplay > Loot
 * Primary responsibility: Represents one Key Tool tier as a double-click and
 * drag source for the locked-loot key socket.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class LockedLootKeyPanelEntryUI : MonoBehaviour,
    IPointerClickHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    private LockedLootKeyPanel _owner;
    private LootKeyTier _tier;

    // Purpose: Binds this runtime row to its single Key Tool tier.
    public void Configure(LockedLootKeyPanel owner, LootKeyTier tier)
    {
        _owner = owner;
        _tier = tier;
    }

    // Purpose: Selects the tier when the player double-clicks its Key Tool row.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null &&
            eventData.button == PointerEventData.InputButton.Left &&
            eventData.clickCount >= 2)
        {
            _owner?.TrySubmitTier(_tier);
        }
    }

    // Purpose: Starts a tier drag only when that Key Tool compartment is usable.
    public void OnBeginDrag(PointerEventData eventData)
    {
        _owner?.BeginTierDrag(_tier);
    }

    // Purpose: Keeps the event-system drag active until the socket receives it.
    public void OnDrag(PointerEventData eventData)
    {
    }

    // Purpose: Clears a cancelled or completed key-tier drag.
    public void OnEndDrag(PointerEventData eventData)
    {
        _owner?.EndTierDrag();
    }
}
