using System;
using System.Collections.Generic;
using UnityEngine;

public partial class PlayerInventoryRuntime
{
    private bool _craftingTransaction;
    public bool PrepareCraftRecipes(out string message)
    {
        message="";
        var catalog=CraftingCatalogSO.Active;
        var save=SaveSystem.Active;
        if(catalog?.recipes==null||save==null)return Fail("제작 저장 정보가 없습니다.",out message);
        CraftingRecipeRoll.ReconcileCommittedRecipes(save.Profile,save.RunData);
        var states=save.Profile.craftingRecipes??=new();
        var pending=new List<CraftRecipeSave>();
        foreach(var recipe in catalog.recipes)
        {
            if(recipe==null||states.Exists(x=>x?.recipeId==recipe.id)||
                catalog.recipes.FindAll(x=>x!=null&&x.id==recipe.id).Count!=1)continue;
            if(!PassiveInstanceRuntime.CanCreate(recipe.output,out _,recipe.randomModuleMaterials?recipe.moduleRarity:null))continue;
            if(CraftingRecipeRoll.TryRoll(catalog,recipe,0,out var state,out _))pending.Add(state);
        }
        if(pending.Count==0)return true;
        // Initialize a large catalog with ONE run/profile commit, not one disk write per visible row.
        var commits=save.RunData.craftingRecipeCommits??=new();
        commits.AddRange(pending);
        if(!save.TrySaveRunDataToDisk())
        {
            foreach(var row in pending)commits.Remove(row);
            return Fail("제작 목록 저장 실패 · 재료는 변경하지 않았습니다.",out message);
        }
        CraftingRecipeRoll.ReconcileCommittedRecipes(save.Profile,save.RunData);
        save.SaveDataToDisk();
        return true;
    }
    public bool TryGetCraftRecipe(string id, out CraftRecipeSave state, out string message)
    {
        state = null; message = "";
        var catalog = CraftingCatalogSO.Active;
        var recipe = catalog?.recipes.Find(x=>x != null && x.id == id);
        var profile = SaveSystem.Active?.Profile;
        if (recipe?.output == null || profile == null) return Fail("제작 카탈로그/저장 정보가 없습니다.",out message);
        if(catalog.recipes.FindAll(x=>x!=null&&x.id==id).Count!=1)return Fail("중복 제작 ID입니다.",out message);
        CraftingRecipeRoll.ReconcileCommittedRecipes(profile,SaveSystem.Active.RunData);
        var states = profile.craftingRecipes ??= new();
        state = states.Find(x=>x != null && x.recipeId == id);
        if (state != null)
        {
            if (!CraftingRecipeRoll.IsValidState(state))
                return Fail("저장 레시피가 잘못되었습니다. 재료 소비·재추첨 없이 중단했습니다.",out message);
            return true;
        }
        if(!PassiveInstanceRuntime.CanCreate(recipe.output,out message,recipe.randomModuleMaterials?recipe.moduleRarity:null))return false;
        if (!CraftingRecipeRoll.TryRoll(catalog,recipe,0,out state,out message)) return false;
        var commits=SaveSystem.Active.RunData.craftingRecipeCommits??=new();
        commits.Add(state);
        if(!SaveSystem.Active.TrySaveRunDataToDisk())
        {
            commits.Remove(state);state=null;
            return Fail("현재 레시피를 저장하지 못했습니다. 제작은 잠시 중단합니다.",out message);
        }
        states.Add(state);
        SaveSystem.Active.SaveDataToDisk(); // Viewing never rerolls an existing recipe.
        return true;
    }

    public bool TryCraft(string id, out string message)
    {
        message = "";
        if (_craftingTransaction || !GameProgressionRuntime.IsHomePreparation || GetComponent<Damageable>()?.IsDead == true)
            return Fail("제작은 살아 있는 플레이어가 HOME에서만 할 수 있습니다.",out message);
        var catalog = CraftingCatalogSO.Active;
        var recipe = catalog?.recipes.Find(x=>x != null && x.id == id);
        var save = SaveSystem.Active;
        if (recipe?.output == null || save == null) return Fail("등록된 제작품이 없습니다.",out message);
        if (!recipe.unlockedByDefault && save.Profile.unlockedCraftRecipeIds?.Contains(id) != true)
            return Fail("퀘스트로 제작법을 먼저 해금하세요.",out message);
        if (!TryGetCraftRecipe(id,out var current,out message)) return false;
        var persistence=FindFirstObjectByType<PlayerRunStatePersistence>();
        if(persistence==null||persistence.IsRestorePending||FindFirstObjectByType<PlayerInventoryRuntime>()!=this)
            return Fail("플레이어 저장·복원이 준비된 후 제작할 수 있습니다.",out message);
        var requirements = new List<ShopTradeRequirement>();
        foreach (var row in current.materials)
        {
            ItemSO material = null;
            foreach (var candidate in catalog.materials ?? new()) if(candidate != null && candidate.Guid == row.itemGuid) {material=candidate;break;}
            if (recipe.currency != null && recipe.currency.Guid == row.itemGuid) material = recipe.currency;
            foreach (var fixedRow in recipe.fixedMaterials ?? Array.Empty<ShopTradeRequirement>())
                if (fixedRow?.Item != null && fixedRow.Item.Guid == row.itemGuid) material=fixedRow.Item;
            if (material == null || row.amount <= 0) return Fail("저장 레시피의 재료가 삭제되었습니다. 재추첨하지 않았습니다.",out message);
            requirements.Add(new ShopTradeRequirement(material,row.amount));
        }
        if (!TryValidateInventoryItemRequirements(requirements,out message)) return false;
        var equipped=GetComponent<PlayerPassiveInventoryRuntime>()?.EquippedSlots;
        bool Spendable(PlayerInventoryItemStack stack)
        {
            if(stack?.IsEmpty!=false)return false;
            if(stack.Item is not PassiveItemSO module)return true;
            if(module.IsBound)return false;
            if(equipped!=null)foreach(var item in equipped)if(item==module)return false;
            return true;
        }
        foreach(var requirement in requirements)
        {
            long available=0;
            foreach(var stack in _items)if(Spendable(stack)&&IsSameItemIdentity(stack.Item,requirement.Item))available+=stack.Amount;
            if(available<requirement.Amount)return Fail("귀속·장착 모듈은 제작 재료로 소비하지 않습니다.",out message);
        }
        // Conservative capacity preflight: keep original state if output has no room.
        if (CalculateStoreCapacity(recipe.output) < recipe.amount)
            return Fail("제작 결과를 받을 공간이 필요합니다. 재료는 소비하지 않았습니다.",out message);
        if (!CanAcquireOwnedAmount(recipe.output,recipe.amount,out message)) return false;
        if (!PassiveInstanceRuntime.CanCreate(recipe.output,out message,
            recipe.randomModuleMaterials ? recipe.moduleRarity : null)) return false;
        if (current.revision == int.MaxValue) return Fail("제작 레시피 갱신 한도에 도달했습니다.",out message);
        if (!CraftingRecipeRoll.TryRoll(catalog,recipe,current.revision+1,out var next,out message)) return false;
        var prepared = new List<ItemSO>();
        for (int i=0;i<recipe.amount;i++)
        {
            var item = PassiveInstanceRuntime.NeedsRoll(recipe.output) ? PassiveInstanceRuntime.Create(recipe.output,
                rarity: recipe.randomModuleMaterials ? recipe.moduleRarity : null) : recipe.output;
            if(item == null) return Fail("제작 개체 준비 실패",out message);
            prepared.Add(item);
        }
        NormalizeStorage();
        var original = _items;
        var working = new List<PlayerInventoryItemStack>();
        foreach(var stack in original)
        {
            var copy = new PlayerInventoryItemStack();
            if(stack?.IsEmpty == false) copy.Set(stack.Item,stack.Amount,stack.ArmorDurability,
                stack.WeaponCurrentDurability,stack.WeaponMaxDurability,stack.GasMaskFilterDurability,
                stack.ArmorMaxDurability,stack.GasMaskMaxFilterDurability);
            working.Add(copy);
        }
        _craftingTransaction = true;
        _items = working;
        try
        {
            // Non-callback commit: inventory, next recipe and save are updated before one notification.
            foreach(var requirement in requirements)
            {
                int remaining = requirement.Amount;
                foreach(var stack in _items)
                {
                    if(!Spendable(stack) || !IsSameItemIdentity(stack.Item,requirement.Item)) continue;
                    int take = Mathf.Min(remaining,stack.Amount);
                    if(stack.Remove(take)) stack.Clear();
                    remaining-=take; if(remaining==0) break;
                }
                if(remaining>0) { _items=original; return Fail("재료가 변경되었습니다.",out message); }
            }
            foreach(var item in prepared)
                if(!TryStoreItem(item,1,out message)) {_items=original;return false;}
            // Write inventory + recipe revision into the SAME atomic run partition first.
            var commits=save.RunData.craftingRecipeCommits??=new();
            int commitIndex=commits.FindIndex(x=>x!=null&&x.recipeId==id);
            var previousCommit=commitIndex>=0?commits[commitIndex]:null;
            if(commitIndex>=0)commits[commitIndex]=next;else commits.Add(next);
            if(!persistence.CaptureCurrentPlayerState()||!save.TrySaveRunDataToDisk())
            {
                if(commitIndex>=0)commits[commitIndex]=previousCommit;else commits.Remove(next);
                _items=original;
                persistence.CaptureCurrentPlayerState(); // Restore the in-memory snapshot as well, without another disk write.
                return Fail("제작 저장 실패 · 재료와 결과를 이전 상태로 유지했습니다.",out message);
            }
            CraftingRecipeRoll.ReconcileCommittedRecipes(save.Profile,save.RunData);
            save.SaveDataToDisk(); // If profile write fails, the atomic run commit recovers it on load.
            _craftingTransaction=false;
            NotifyChanged();
            QuestRuntime.ReportAction(id,recipe.amount,QuestObjectiveType.Craft,recipe.output);
            message="제작 완료 · 다음 레시피 갱신"; return true;
        }
        finally {_craftingTransaction=false;}
    }
}
