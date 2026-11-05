using UnityEngine;
using UnityEngine.EventSystems;

// Tooltip only: intentionally has no inventory-owner, click or drag interface.
public sealed class EnemyPassiveHoverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public EnemyInventoryUI owner;
    public PassiveItemSO item;
    public void OnPointerEnter(PointerEventData data) => owner?.HandleItemTooltipEntered(item, (RectTransform)transform);
    public void OnPointerExit(PointerEventData data) => owner?.HandleItemTooltipExited();
}
