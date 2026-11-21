using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class CraftingRecipe
{
    public string id;
    public ItemSO output;
    [Min(1)] public int amount = 1;
    public bool unlockedByDefault = true;
    public ShopTradeRequirement[] fixedMaterials = Array.Empty<ShopTradeRequirement>();
    public bool randomModuleMaterials;
    public PlayerShopItemRarity moduleRarity;
    public string[] valuableCategories = Array.Empty<string>();
    public CurrencyItemSO currency;
    [Min(0)] public int price;
}

[CreateAssetMenu(menuName = "Game/Systems/제작 · 모듈 재료", fileName = "CraftingCatalog")]
public sealed class CraftingCatalogSO : ScriptableObject
{
    public List<CraftingRecipe> recipes = new();
    [Tooltip("Explicit eligible material pool. No runtime scene scan or hidden quest-only candidates.")]
    public List<ValuableItemSO> materials = new();
    public static CraftingCatalogSO Active => Resources.Load<CraftingCatalogSO>("CraftingCatalog");
}

[Serializable]
public sealed class CraftMaterialSave
{
    public string itemGuid;
    public int amount;
}
[Serializable]
public sealed class CraftRecipeSave
{
    public string recipeId;
    public int revision;
    public List<CraftMaterialSave> materials = new();
}

public static class CraftingRecipeRoll
{
    public static bool IsValidState(CraftRecipeSave state)
    {
        if(state==null||string.IsNullOrWhiteSpace(state.recipeId)||state.revision<0||
            state.materials==null||state.materials.Count==0)return false;
        var guids=new HashSet<string>();
        foreach(var row in state.materials)
            if(row==null||string.IsNullOrWhiteSpace(row.itemGuid)||row.amount<=0||!guids.Add(row.itemGuid))return false;
        return true;
    }
    private static CraftRecipeSave Copy(CraftRecipeSave source)
    {
        var copy=new CraftRecipeSave{recipeId=source.recipeId,revision=source.revision};
        foreach(var row in source.materials??new())
            if(row!=null)copy.materials.Add(new CraftMaterialSave{itemGuid=row.itemGuid,amount=row.amount});
        return copy;
    }
    public static void ReconcileCommittedRecipes(Save profile, RunSaveData run)
    {
        if(profile==null||run?.craftingRecipeCommits==null)return;
        profile.craftingRecipes??=new();
        foreach(var committed in run.craftingRecipeCommits)
        {
            if(!IsValidState(committed))continue;
            int index=profile.craftingRecipes.FindIndex(x=>x!=null&&x.recipeId==committed.recipeId);
            if(index<0)profile.craftingRecipes.Add(Copy(committed));
            else if(profile.craftingRecipes[index].revision<committed.revision)profile.craftingRecipes[index]=Copy(committed);
        }
    }
    public static PlayerShopItemRarity[] RequiredRarities(PlayerShopItemRarity rarity)
    {
        var n = PlayerShopItemRarity.Normal; var r = PlayerShopItemRarity.Rare;
        var u = PlayerShopItemRarity.Unique; var l = PlayerShopItemRarity.Legendary;
        return rarity switch
        {
            PlayerShopItemRarity.Rare => new[] {r,r,n},
            PlayerShopItemRarity.Unique => new[] {u,u,r},
            PlayerShopItemRarity.Legendary => new[] {l,l,u,r},
            PlayerShopItemRarity.Mythic => new[] {rarity,rarity,l,u,r,n},
            _ => new[] {n,n,n}
        };
    }
    public static bool TryRoll(CraftingCatalogSO catalog, CraftingRecipe recipe, int revision,
        out CraftRecipeSave result, out string message)
    {
        result = null; message = "";
        if (catalog == null || recipe?.output == null || string.IsNullOrWhiteSpace(recipe.id))
        { message = "제작품 또는 고정 ID가 없습니다."; return false; }
        if(recipe.amount<1||recipe.amount>10000||recipe.price<0||revision<0){message="제작 수량·가격·갱신 번호가 잘못되었습니다.";return false;}
        var next = new CraftRecipeSave {recipeId=recipe.id,revision=revision};
        bool Add(ItemSO item, int count)
        {
            if(item==null||string.IsNullOrWhiteSpace(item.Guid)||count<=0)return false;
            var row = next.materials.Find(x=>x.itemGuid == item.Guid);
            if (row == null) next.materials.Add(new CraftMaterialSave {itemGuid=item.Guid,amount=count});
            else
            {
                if((long)row.amount+count>int.MaxValue)return false;
                row.amount+=count;
            }
            return true;
        }
        foreach (var row in recipe.fixedMaterials ?? Array.Empty<ShopTradeRequirement>())
        {
            if (row?.IsValid != true) {message="고정 재료 설정 오류"; return false;}
            if(!Add(row.Item,row.Amount)){message="고정 재료 GUID·수량 오류";return false;}
        }
        if (recipe.randomModuleMaterials)
        {
            if (recipe.output is not PassiveItemSO) {message="랜덤 모듈 레시피의 제작품은 모듈이어야 합니다."; return false;}
            var random = new System.Random();
            foreach (var rarity in RequiredRarities(recipe.moduleRarity))
            {
                var pool = (catalog.materials ?? new()).FindAll(x=>x != null && x is not CurrencyItemSO &&
                    x.Rarity == rarity && !string.IsNullOrWhiteSpace(x.Guid) &&
                    Array.IndexOf(recipe.valuableCategories ?? Array.Empty<string>(), x.ValuableCategoryId) >= 0);
                // Repeated picks become a quantity requirement. Empty tiers never silently downgrade.
                pool.Sort((a,b)=>string.CompareOrdinal(a.Guid,b.Guid));
                if (pool.Count == 0) {message=$"{recipe.id}: {rarity} 카테고리 재료 후보가 없습니다."; return false;}
                if(!Add(pool[random.Next(pool.Count)],1)){message="추첨 재료 수량 오류";return false;}
            }
        }
        if (recipe.price > 0)
        {
            if (recipe.currency == null) {message="추가 금액에 사용할 실물 화폐가 없습니다.";return false;}
            if(!Add(recipe.currency,recipe.price)){message="화폐 수량 오류";return false;}
        }
        if (next.materials.Count == 0) {message="재료 없는 무료 제작은 등록할 수 없습니다."; return false;}
        if(!IsValidState(next)){message="재료 GUID·수량 설정이 잘못되었습니다.";return false;}
        result=next; return true;
    }
}
