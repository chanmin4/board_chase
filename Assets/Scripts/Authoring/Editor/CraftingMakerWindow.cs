using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class CraftingMakerWindow : EditorWindow
{
    private CraftingCatalogSO _catalog;
    private Vector2 _scroll;
    private Vector2 _listScroll;
    private int _selected;
    [MenuItem("Tools/Game Tool/Crafting Maker")]
    public static void Open() => GetWindow<CraftingMakerWindow>("Crafting Maker");
    private void OnGUI()
    {
        _catalog=(CraftingCatalogSO)EditorGUILayout.ObjectField("제작 카탈로그",_catalog??CraftingCatalogSO.Active,typeof(CraftingCatalogSO),false);
        if(_catalog==null)
        {
            if(GUILayout.Button("Resources에 제작 카탈로그 생성"))
            {
                _catalog=CreateInstance<CraftingCatalogSO>();
                AssetDatabase.CreateAsset(_catalog,"Assets/Resources/CraftingCatalog.asset");
            }
            return;
        }
        EditorGUILayout.HelpBox("완료마다 모듈 재료만 재추첨합니다. 일반 제작은 고정 재료입니다. 퀘스트에는 제작 ID를 연결하세요. 기존 퀘스트 내용은 변경하지 않습니다.",MessageType.Info);
        using(new EditorGUILayout.HorizontalScope())
        {
            if(GUILayout.Button("기존 모듈 등록 · 빠진 종류만")) RegisterModules();
            if(GUILayout.Button("+ 일반 제작"))
            {
                Undo.RecordObject(_catalog,"Add recipe");
                _catalog.recipes.Add(new CraftingRecipe{id="recipe_"+Guid.NewGuid().ToString("N")});
                EditorUtility.SetDirty(_catalog);
            }
            if(GUILayout.Button("저장")){EditorUtility.SetDirty(_catalog);AssetDatabase.SaveAssets();}
        }
        using(new EditorGUILayout.HorizontalScope())
        {
            using(new EditorGUILayout.VerticalScope(GUILayout.Width(240)))
            {
                _listScroll=EditorGUILayout.BeginScrollView(_listScroll);
                for(int i=0;i<_catalog.recipes.Count;i++)
                {
                    var row=_catalog.recipes[i];
                    string label=row?.output?.name??"제작품 미지정";
                    if(row?.randomModuleMaterials==true)label+=" · "+row.moduleRarity;
                    if(GUILayout.Toggle(_selected==i,label,"Button"))_selected=i;
                }
                EditorGUILayout.EndScrollView();
            }
            _scroll=EditorGUILayout.BeginScrollView(_scroll);
            var so=new SerializedObject(_catalog);so.Update();
            var recipes=so.FindProperty("recipes");
            if(_selected>=0&&_selected<recipes.arraySize)
            {
                EditorGUILayout.PropertyField(recipes.GetArrayElementAtIndex(_selected),new GUIContent("제작품·해금·재료"),true);
                if(GUILayout.Button("선택 레시피 삭제")){recipes.DeleteArrayElementAtIndex(_selected);_selected=0;}
            }
            EditorGUILayout.PropertyField(so.FindProperty("materials"),new GUIContent("추첨 승인 Valuable 풀"),true);
            so.ApplyModifiedProperties();EditorGUILayout.EndScrollView();
        }
    }
    private void RegisterModules()
    {
        Undo.RecordObject(_catalog,"Register modules");
        foreach(string id in AssetDatabase.FindAssets("t:ValuableItemSO"))
        {
            var item=AssetDatabase.LoadAssetAtPath<ValuableItemSO>(AssetDatabase.GUIDToAssetPath(id));
            if(item!=null&&item is not CurrencyItemSO&&!_catalog.materials.Contains(item))_catalog.materials.Add(item);
        }
        foreach(string id in AssetDatabase.FindAssets("t:PassiveItemSO"))
        {
            var item=AssetDatabase.LoadAssetAtPath<PassiveItemSO>(AssetDatabase.GUIDToAssetPath(id));
            if(item==null||!AssetDatabase.GetAssetPath(item).StartsWith("Assets/Resources/PassiveModules/",StringComparison.Ordinal)||
                !System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(item)).StartsWith("Module_",StringComparison.Ordinal))continue;
            foreach(var rarity in new[]{PlayerShopItemRarity.Normal,PlayerShopItemRarity.Rare,PlayerShopItemRarity.Unique,PlayerShopItemRarity.Legendary,PlayerShopItemRarity.Mythic})
            {
                if(!item.RandomStatsEnabled&&(item.StatModifiers?.Length??0)==0&&(item.FeatureFlags?.Length??0)==0)continue;
                if(_catalog.recipes.Any(r=>r.output==item&&r.moduleRarity==rarity))continue;
                _catalog.recipes.Add(new CraftingRecipe{id="module_"+item.Guid+"_"+(int)rarity,output=item,
                    randomModuleMaterials=true,moduleRarity=rarity,
                    valuableCategories=item.Category==PassiveCategory.Combat?new[]{"military"}:new[]{"medical","tools"}});
            }
        }
        EditorUtility.SetDirty(_catalog);
    }
}
