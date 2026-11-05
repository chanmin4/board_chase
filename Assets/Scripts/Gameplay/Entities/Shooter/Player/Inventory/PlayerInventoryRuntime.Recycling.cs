using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public sealed class RecyclingSlotSaveData
{
    public PlayerRunInventorySlotSaveData input;
    public string outputGuid;
    public int outputAmount;
    public float remaining;
    public float duration;
    public bool IsEmpty => input == null || string.IsNullOrEmpty(input.itemGuid);
}

public partial class PlayerInventoryRuntime
{
    private RuntimeContentCatalogSO _recyclingCatalog;
    public int RecyclingCapacity
    {
        get
        {
            int configured=Mathf.Clamp((RecyclingRulesSO.Active?.recycling.baseSlots ?? 2) +
                (SaveSystem.Active?.Profile.recyclingSlotBonus ?? 0),1,Mathf.Max(1,RecyclingRulesSO.Active?.recycling.maximumSlots ?? 10));
            // Lowering authoring limits must never hide a queued item from its owner.
            var jobs=SaveSystem.Active?.RunData.recyclingSlots;
            if(jobs!=null)for(int i=jobs.Count-1;i>=configured;i--)
                if(jobs[i]?.IsEmpty==false)return i+1;
            return configured;
        }
    }
    public IReadOnlyList<RecyclingSlotSaveData> RecyclingSlots => SaveSystem.Active?.RunData.recyclingSlots;
    public bool CanLoseInventorySlots(int removed,out string message,int ignoreIndex=-1)
    {
        message="패시브 해제·리롤로 줄어드는 인벤토리 칸을 먼저 비우세요.";
        int capacity=Mathf.Max(0,Capacity-Mathf.Max(0,removed));
        for(int i=capacity;i<_items.Count;i++)if(i!=ignoreIndex&&_items[i]?.IsEmpty==false)return false;
        return true;
    }
    public bool CanReceiveQuestAcceptance(ShopTradeRequirement[] grants,out string message)
    {
        NormalizeStorage();var simulated=new List<PlayerInventoryItemStack>();
        foreach(var stack in _items){var copy=new PlayerInventoryItemStack();if(stack?.IsEmpty==false)copy.Set(stack.Item,stack.Amount);simulated.Add(copy);}
        var granted=new Dictionary<ItemSO,int>();
        foreach(var grant in grants??Array.Empty<ShopTradeRequirement>())
        {
            if(grant?.Item==null)return Fail("퀘스트 수락 지급품이 설정되지 않았습니다.",out message);
            if(!PassiveInstanceRuntime.CanCreate(grant.Item,out message))return false;
            int remaining=Mathf.Max(1,grant.Amount);
            granted.TryGetValue(grant.Item,out int previous);
            if((long)previous+remaining>int.MaxValue)return Fail("퀘스트 지급 수량이 너무 큽니다.",out message);
            granted[grant.Item]=previous+remaining;
            if(!CanAcquireOwnedAmount(grant.Item,previous+remaining,out message))return false;
            foreach(var stack in simulated)
            {
                if(remaining<=0)break;
                if(!stack.IsEmpty&&stack.Item!=grant.Item)continue;
                int room=stack.IsEmpty?grant.Item.MaxStack:Mathf.Max(0,grant.Item.MaxStack-stack.Amount);
                int add=Mathf.Min(room,remaining);if(add<=0)continue;
                stack.Set(grant.Item,(stack.IsEmpty?0:stack.Amount)+add);remaining-=add;
            }
            if(remaining>0)return Fail("퀘스트 지급품을 받을 인벤토리 공간이 필요합니다.",out message);
        }
        message="수락 지급품 공간 확인 완료.";return true;
    }
    public bool TryReceiveQuestAcceptance(ShopTradeRequirement[] grants,out string message)
    {
        if(!CanReceiveQuestAcceptance(grants,out message))return false;
        var backup=new List<PlayerInventoryItemStack>();
        foreach(var stack in _items)
        {
            var copy=new PlayerInventoryItemStack();
            if(stack?.IsEmpty==false)copy.Set(stack.Item,stack.Amount,stack.ArmorDurability,stack.WeaponCurrentDurability,stack.WeaponMaxDurability,stack.GasMaskFilterDurability,stack.ArmorMaxDurability,stack.GasMaskMaxFilterDurability);
            backup.Add(copy);
        }
        var prepared=new List<(ItemSO item,int amount)>();
        foreach(var grant in grants??Array.Empty<ShopTradeRequirement>())
        {
            if(PassiveInstanceRuntime.NeedsRoll(grant.Item))
            {
                for(int i=0;i<Mathf.Max(1,grant.Amount);i++)
                {
                    var instance=PassiveInstanceRuntime.Create(grant.Item);
                    if(instance==null)return Fail("수락 지급 패시브 생성에 실패했습니다. 인벤토리는 변경하지 않았습니다.",out message);
                    prepared.Add((instance,1));
                }
            }
            else prepared.Add((grant.Item,Mathf.Max(1,grant.Amount)));
        }
        foreach(var grant in prepared)
            if(!TryStoreItem(grant.item,grant.amount,out message)){_items=backup;return false;}
        NotifyChanged();message="퀘스트 수락 지급품 수령 완료.";return true;
    }
    private void Update()
    {
#if UNITY_EDITOR
        if (CombatLabRuntime.IsIsolatedSession) return;
#endif
        if (_passiveOverflowPending && Time.unscaledTime >= _nextPassiveOverflowRetry) DropPassiveCapacityOverflow();
        var slots=SaveSystem.Active?.RunData.recyclingSlots;
        if (slots==null || GetComponent<Damageable>()?.IsDead==true) return;
        foreach(var slot in slots)
            if(slot!=null && !slot.IsEmpty && slot.remaining>0) slot.remaining=Mathf.Max(0,slot.remaining-Time.deltaTime);
    }
    private List<RecyclingSlotSaveData> EnsureRecyclingSlots()
    {
        var profile=SaveSystem.Active?.RunData; if(profile==null)return null;
        profile.recyclingSlots??=new();
        while(profile.recyclingSlots.Count<RecyclingCapacity)profile.recyclingSlots.Add(new());
        return profile.recyclingSlots;
    }
    public bool TryStartRecycling(int inventoryIndex,int processorIndex,out string message)
    {
        NormalizeStorage(); var slots=EnsureRecyclingSlots();
        if(!IsValidIndex(inventoryIndex)||slots==null||processorIndex<0||processorIndex>=RecyclingCapacity)
            return Fail("분해기 또는 인벤토리 칸이 잘못되었습니다.",out message);
        if(!slots[processorIndex].IsEmpty)return Fail("사용 중인 분해기 칸입니다.",out message);
        var stack=_items[inventoryIndex];var recipe=RecyclingRulesSO.Active?.recycling.Find(stack.Item);
        if(stack.IsEmpty||recipe?.output==null)return Fail("Game System Tool에 이 아이템의 분해 결과를 등록하세요.",out message);
        var passives=GetComponent<PlayerPassiveInventoryRuntime>();
        bool equipped=false;
        if(stack.Item is PassiveItemSO passive && passives!=null)
            foreach(var p in passives.EquippedSlots)if(p==passive)equipped=true;
        if (stack.Item is PassiveItemSO used && (equipped || used.EverEquipped || used.DisabledForRaid))
            return Fail("장착했던 패시브와 해제·비활성화된 패시브는 분해할 수 없습니다.", out message);
        slots[processorIndex]=new RecyclingSlotSaveData {
            input=new PlayerRunInventorySlotSaveData { itemGuid=stack.Item.Guid,amount=1,
                armorDurability=stack.ArmorDurability,armorMaxDurability=stack.ArmorMaxDurability,
                weaponCurrentDurability=stack.WeaponCurrentDurability,weaponMaxDurability=stack.WeaponMaxDurability,
                gasMaskFilterDurability=stack.GasMaskFilterDurability,gasMaskMaxFilterDurability=stack.GasMaskMaxFilterDurability },
            outputGuid=recipe.output.Guid,outputAmount=Mathf.Max(1,recipe.outputAmount),duration=Mathf.Max(.1f,recipe.seconds),remaining=Mathf.Max(.1f,recipe.seconds)
        };
        if(stack.Remove(1))stack.Clear();
        NotifyChanged();SaveSystem.Active.SaveDataToDisk();
        message="분해 시작. 완료 후 클릭하면 결과를 받습니다. 진행 중 클릭하면 취소·회수합니다.";return true;
    }
    public bool TryCollectOrCancelRecycling(int processorIndex,out string message)
    {
        var slots=EnsureRecyclingSlots();
        if(slots==null||processorIndex<0||processorIndex>=slots.Count||slots[processorIndex].IsEmpty)
            return Fail("이 칸에 인벤토리 아이템을 드래그하세요.",out message);
        var job=slots[processorIndex];bool done=job.remaining<=0;
        _recyclingCatalog??=Resources.Load<RuntimeContentCatalogSO>("RuntimeContentCatalog");
        if(_recyclingCatalog==null||!_recyclingCatalog.TryResolveItem(done?job.outputGuid:job.input.itemGuid,out var item))
            return Fail("분해 아이템 GUID를 복원하지 못했습니다. 원본은 보존합니다.",out message);
        bool stored=done?TryStoreItem(item,job.outputAmount,out message):TryStoreItem(item,1,
            job.input.armorDurability,job.input.weaponCurrentDurability,job.input.weaponMaxDurability,
            job.input.gasMaskFilterDurability,job.input.armorMaxDurability,job.input.gasMaskMaxFilterDurability,out message);
        if(!stored)return false;
        slots[processorIndex]=new();NotifyChanged();SaveSystem.Active.SaveDataToDisk();
        message=done?"분해 결과 수령 완료.":"분해 취소. 원본 회수, 다음 등록 시 처음부터 진행합니다.";return true;
    }
}
