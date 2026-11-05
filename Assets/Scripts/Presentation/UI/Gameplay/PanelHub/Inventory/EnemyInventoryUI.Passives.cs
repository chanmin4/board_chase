using UnityEngine;
using UnityEngine.UI;

public partial class EnemyInventoryUI
{
    [SerializeField] private PassiveInventorySlotUI[] _passiveInspectSlots;
    private RectTransform _passiveModuleRow;
    private void ResolveAuthoredPassiveRow()
    {
        _passiveModuleRow??=transform.Find("PassiveSection/PassiveSection") as RectTransform;
        if(_passiveInspectSlots==null||_passiveInspectSlots.Length==0)
            _passiveInspectSlots=_passiveModuleRow!=null?_passiveModuleRow.GetComponentsInChildren<PassiveInventorySlotUI>(true):System.Array.Empty<PassiveInventorySlotUI>();
    }
    private void RefreshPassiveModules()
    {
        ResolveAuthoredPassiveRow();
        var passives=_activeInventory!=null&&_activeInventory.ShowsEquipment?
            EntityComponentResolver.Resolve<EnemyShooterStatsRuntime>(_activeInventory)?.PassiveModules:null;
        var bySlot = new PassiveItemSO[8];
        if (passives != null)
            foreach (var passive in passives)
            {
                if (passive == null) continue;
                for (int slot = (int)passive.Category; slot < bySlot.Length; slot += 2)
                    if (bySlot[slot] == null) { bySlot[slot] = passive; break; }
            }
        for(int i=0;i<_passiveInspectSlots.Length;i++)
        {
            var view=_passiveInspectSlots[i];if(view==null)continue;
            var item=i<bySlot.Length?bySlot[i]:null;
            view.SetCategorySlotIndex(i, true);
            view.gameObject.SetActive(true);view.enabled=false;view.BindPassive(item,item!=null);
            InventorySplitLayout.PassiveSlot(view,i);
            var hover=view.GetComponent<EnemyPassiveHoverUI>();
            if(hover==null)hover=view.gameObject.AddComponent<EnemyPassiveHoverUI>(); // Behavior only; no visual instantiation.
            hover.owner=this;hover.item=item;
        }
    }
}

