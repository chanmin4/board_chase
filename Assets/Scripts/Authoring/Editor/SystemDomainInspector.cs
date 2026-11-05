using UnityEditor;
using UnityEngine;

/// <summary>Domain editors share only the workbench, never their data or runtime rules.</summary>
internal static class SystemDomainInspector
{
    public static bool Supports(ScriptableObject asset) => asset is ExperienceRulesSO or ItemUsageRulesSO or
        RecyclingRulesSO or SectorTravelRulesSO or MapDiscoveryRulesSO or ItemRarityStyleSO;

    public static void Draw(ScriptableObject asset)
    {
        var data=new SerializedObject(asset);data.Update();
        switch(asset)
        {
            case ExperienceRulesSO xp:
                EditorGUILayout.LabelField("경험치 · 레벨",EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("성장 곡선·획득량·레벨 보상만 설정합니다. 누적 XP는 플레이어 저장 파일에 보관하며 이 SO를 덮어쓰지 않습니다.",MessageType.Info);
                var total=SaveSystem.Active?.Profile.experience?.total??0;
                EditorGUILayout.LabelField($"현재 Lv.{xp.experience.Level(total)} · 누적 XP {total}");
                var experience=data.FindProperty("experience");
                var curve=experience.FindPropertyRelative("experiencePerLevel");
                curve.isExpanded=EditorGUILayout.Foldout(curve.isExpanded,"레벨별 필요 경험치",true);
                if(curve.isExpanded)
                {
                    curve.arraySize=Mathf.Clamp(EditorGUILayout.IntField("단계 수",curve.arraySize),1,999);
                    for(int i=0;i<curve.arraySize;i++)
                        curve.GetArrayElementAtIndex(i).intValue=Mathf.Max(1,EditorGUILayout.IntField($"Lv.{i+1} → {i+2}",curve.GetArrayElementAtIndex(i).intValue));
                }
                DrawChildren(experience,"experiencePerLevel");
                break;
            case ItemUsageRulesSO:
                EditorGUILayout.LabelField("아이템 사용 레벨",EditorStyles.boldLabel);
                var usage=data.FindProperty("usage");
                DrawLevels(usage.FindPropertyRelative("weapon"),"무기",System.Enum.GetNames(typeof(RangedWeaponType)));
                DrawLevels(usage.FindPropertyRelative("armor"),"방탄",new[]{"미사용 0","Class 1","Class 2","Class 3","Class 4","Class 5","Class 6","Class 7 · 미정","Class 8 · 미정"});
                DrawLevels(usage.FindPropertyRelative("ammunition"),"탄약",new[]{"미사용 0","Tier 1","Tier 2","Tier 3","Tier 4","Tier 5","Tier 6","Tier 7 · 미정","Tier 8 · 미정"});
                DrawLevels(usage.FindPropertyRelative("passive"),"패시브",new[]{"Normal","Rare","Legendary · 미정","Unique","Mythic · 미정"});
                EditorGUILayout.HelpBox("1=제한 없음. Item Maker 개별 지정이 우선입니다. 획득·보관·스테이지 입장을 잠그지 않습니다.",MessageType.Info);
                break;
            case RecyclingRulesSO:
                EditorGUILayout.LabelField("시간제 분해기",EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("슬롯·무기/방탄 레시피만 담당합니다. 패시브 재료와 시간은 왼쪽 패시브 메뉴 한 곳에서 설정합니다. 진행 중 작업의 저장된 결과·시간은 바뀌지 않습니다.",MessageType.Info);
                DrawChildren(data.FindProperty("recycling"));
                LootObjectConversionUtility.Draw();
                break;
            case SectorTravelRulesSO:
                EditorGUILayout.LabelField("섹터 이동 · 포탈",EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("시작방 포함 각 방의 장치를 조작해야 그 방의 포탈이 열립니다. 개방은 원정 저장에 유지되며, 이동은 매번 별도 시전합니다. 재사용 쿨타임은 없습니다. 탈출구의 클리어·전력 조건은 유지합니다.",MessageType.Info);
                DrawChildren(data.FindProperty("travel"));
                break;
            case MapDiscoveryRulesSO:
                EditorGUILayout.LabelField("지도 발견 표시",EditorStyles.boldLabel);
                DrawChildren(data.FindProperty("map"));
                EditorGUILayout.HelpBox("지도 아이콘은 같은 메뉴의 SectorMapIcons SO에서 수정합니다. Stage Maker에서도 같은 원본을 사용합니다.",MessageType.None);
                break;
            case ItemRarityStyleSO:
                EditorGUILayout.LabelField("공통 아이템 등급 색상",EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(data.FindProperty("rarityColors"),new GUIContent("등급 색상 · 저장 enum 순서"),true);
                break;
        }
        data.ApplyModifiedProperties();
    }

    private static void DrawChildren(SerializedProperty root,string skip=null)
    {
        var cursor=root.Copy();var end=root.GetEndProperty();bool enter=true;
        while(cursor.NextVisible(enter)&&!SerializedProperty.EqualContents(cursor,end))
        {
            enter=false;if(cursor.name==skip)continue;
            EditorGUILayout.PropertyField(cursor,new GUIContent(Label(cursor.name)),true);
        }
    }

    private static string Label(string name) => name switch
    {
        "maximumLevel"=>"최대 레벨", "normalKill"=>"일반 적 처치 XP", "bossKill"=>"보스 처치 XP",
        "sectorDiscovery"=>"섹터 발견 XP", "defaultQuest"=>"기본 퀘스트 XP", "discoveryByRarity"=>"등급별 발견 XP",
        "extractionMultiplier"=>"생존 정산 배율", "deathMultiplier"=>"사망 정산 배율", "statRewards"=>"레벨별 영구 보상",
        "enabled"=>"사용", "baseGaugeCapacity"=>"기본 감염 게이지 용량", "defenseCapacityPerLevel"=>"방호 수치당 추가 용량",
        "naturalGaugeRecoveryPerSecond"=>"초당 게이지 자연 회복", "maximumStacks"=>"최대 감염 중첩",
        "damageInterval"=>"감염 피해 간격 (초)", "infectionPerStackPerTick"=>"중첩당 틱 피해",
        "lootOpenExposure"=>"루팅 창 노출", "itemPickupExposure"=>"아이템 획득 노출", "hitExposure"=>"피격 노출",
        "bleedingExposurePerSecond"=>"출혈 초당 노출", "gasExposurePerPressureSecond"=>"가스 압력·초당 노출",
        "infectedBulletExposure"=>"감염 적 탄환 노출", "baseSlots"=>"기본 분해 슬롯", "maximumSlots"=>"최대 분해 슬롯",
        "recipes"=>"무기 · 방탄 레시피", "freeSectorTravel"=>"미클리어 섹터도 이동 허용", "portalCooldown"=>"포탈 재사용 대기 (초)",
        "portalCast"=>"매 이동 상호작용 (초)", "requirePortalActivation"=>"방별 장치로 포탈 개방", "portalActivationCast"=>"장치 개방 상호작용 (초)", "hideUnvisited"=>"미방문 방 아이콘 숨김", "hideBossIcons"=>"보스 아이콘 숨김",
        _=>name
    };

    private static void DrawLevels(SerializedProperty array,string label,string[] names)
    {
        array.isExpanded=EditorGUILayout.Foldout(array.isExpanded,label,true);
        if(!array.isExpanded)return;
        using(new EditorGUI.IndentLevelScope())
        {
            array.arraySize=names.Length;
            for(int i=0;i<names.Length;i++)
            {
                var element=array.GetArrayElementAtIndex(i);
                element.intValue=Mathf.Max(1,EditorGUILayout.IntField(names[i],element.intValue));
            }
        }
    }
}
