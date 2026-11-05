using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>Serialized SO authoring only: no runtime copies or scene modifications.</summary>
internal sealed class CharacterStatusMakerInspector
{
    private readonly ScriptableObject _asset;
    private SerializedObject _data;
    private ReorderableList _modifiers;
    private int _statTab;
    private string _fieldSearch = "";
    private string _aiSearch = "";
    private Texture2D _iconImage;
    private readonly Dictionary<string, bool> _foldouts = new();
    private static readonly string[] AiGroups = { "기본 정보 · 프리팹 · 외형", "전술 · 산개 · 이동", "탐지 · 추적 · 소리", "근접 공격", "투척 공격", "대시 · 회피", "사격 · 전투", "기타 연결 설정" };
    private static readonly string[] StatTabs = { "능력치 · 생존", "시야", "AI · 연결 설정" };
    private static readonly HashSet<string> StatFields = new(StringComparer.Ordinal)
    {
        "_initialHealth", "_moveSpeed", "_carryCapacityKg", "_baseArmorClass", "_shooter", "_locomotion", "_aimFeedback",
        "dodgeChance", "_armor", "_infection", "_dash", "_inventory", "_maximumStamina",
        "_commonRules", "_overrideCommonLocomotion", "_overrideCommonAimFeedback", "_overrideCommonArmorDrop"
    };
    private static readonly HashSet<string> VisionFields = new(StringComparer.Ordinal)
        { "_vision", "_useIndividualTargetDetection" };
    // These roots deliberately use HideInInspector to prevent duplicate authoring.
    // Never rely on hasVisibleChildren/PropertyField to draw their override UI.
    private static readonly Dictionary<string, string[]> OverrideSettingFields = new(StringComparer.Ordinal)
    {
        ["_locomotion"] = new[]
        {
            "enabled", "walkSpeedMultiplier", "sprintSpeedMultiplier", "sprintCostPerSecond",
            "dashCost", "recoveryPerSecond", "recoveryDelay", "exhaustionThreshold"
        },
        ["_aimFeedback"] = new[]
        {
            "hitKickDistance", "maximumHitKickDistance", "hitKickRecoveryPerSecond",
            "enemyHitInaccuracySeconds", "enemyHitExtraSpreadDegrees"
        }
    };
    private static readonly Dictionary<string, string> Labels = new(StringComparer.Ordinal)
    {
        ["_alertReactionMultiplier"] = "경계 · 재발견 반응 대기 배율", ["_patrolRadius"] = "순찰 반경", ["_patrolIdleSeconds"] = "순찰 도착 후 대기 (초)",
        ["distanceMultiplier"] = "대시 거리 배율", ["homeOnlyPassiveChanges"] = "구형 HOME 제한 · 미사용",
        ["combatSlotLevels"] = "전투 1~4칸 해금 레벨", ["survivalSlotLevels"] = "생존 1~4칸 해금 레벨",
        ["combatEffectMultiplier"] = "전투 효과 강도 배율", ["survivalEffectMultiplier"] = "생존 효과 강도 배율",
        ["changeCooldownSeconds"] = "장착·교체·해제 공통 쿨다운 (초)",
        ["unifiedModule"] = "통합 패시브 모듈", ["startingDraws"] = "시작 추첨 종류 · 레벨/비용/등급 비중",
        ["startingPool"] = "시작 패시브 후보", ["drawCountLevels"] = "추첨 1회씩 추가되는 레벨",
        ["moduleYield"] = "미장착품 분해 모듈 수량",
        ["tiers"] = "등급별 모듈 반환량 · 리롤 비용", ["parts"] = "구형 등급 파츠 · 리롤 호환용",
        ["rerollCost"] = "리롤 소모 수량", ["recyclingSeconds"] = "패시브 분해 시간 (초)",
        ["enemyPool"] = "적 추가 패시브 후보",
        ["_id"] = "고유 ID", ["_enemy"] = "적 기본 정보", ["_displayName"] = "표시 이름",
        ["_initialHealth"] = "최대 체력", ["_moveSpeed"] = "이동 속도", ["_baseArmorClass"] = "기본 방어 등급",
        ["_carryCapacityKg"] = "기준 수용 무게 (kg)",
        ["_shooter"] = "발소리 · 소리 조사 지연", ["_meleeDamage"] = "근접 피해", ["dashCooldownSeconds"] = "대시 쿨다운 (초)",
        ["_locomotion"] = "걷기 · 달리기 · 스태미나", ["_aimFeedback"] = "피격 흔들림 · 명중 저하",
        ["_maximumStamina"] = "최대 스태미나 · 개별 값", ["_commonRules"] = "공통 규칙 원본 SO",
        ["_inventory"] = "소지 용량 · 포켓 + 기본칸 + 가방", ["_pocketCapacity"] = "포켓 칸 수",
        ["_baseInventoryCapacity"] = "가방 없이 기본 인벤토리 칸 수", ["_dash"] = "대시 · 개별 설정",
        ["speedMultiplier"] = "대시 속도 배율", ["fixedSpeed"] = "대시 고정 속도", ["durationSeconds"] = "대시 시간 (초)",
        ["cooldownSeconds"] = "대시 쿨다운 (초)", ["useMoveSpeedBasedDash"] = "기본 이속 기준 대시",
        ["_enemyVision"] = "적 공통 시야 · 차단 레이어 / 눈높이 포함",
        ["enabled"] = "스태미나 사용", ["maximumStamina"] = "기본 스태미나", ["enemyPoolMultiplier"] = "적 스태미나 풀 배율",
        ["walkSpeedMultiplier"] = "걷기 속도 배율", ["sprintSpeedMultiplier"] = "달리기 속도 배율",
        ["sprintCostPerSecond"] = "달리기 초당 소모", ["dashCost"] = "대시 1회 소모",
        ["recoveryPerSecond"] = "초당 회복량", ["recoveryDelay"] = "회복 대기 (초)",
        ["exhaustionThreshold"] = "달리기 · 대시 입력 시작 최소 비율", ["enemyCanSprint"] = "적 달리기 사용",
        ["enemySprintStartDistance"] = "적 달리기 시작 거리", ["enemySprintStopDistance"] = "적 달리기 종료 거리",
        ["shotKickDistance"] = "발사 반동 거리", ["hitKickDistance"] = "피격 흔들림 거리",
        ["maximumKickDistance"] = "흔들림 누적 상한", ["enemyHitInaccuracySeconds"] = "적 피격 명중 저하 (초)",
        ["maximumHitKickDistance"] = "피격 흔들림 누적 상한", ["hitKickRecoveryPerSecond"] = "피격 흔들림 초당 복구",
        ["enemyHitExtraSpreadDegrees"] = "적 피격 추가 탄퍼짐 (도)",
        ["_limitCorpseArmorDurability"] = "시체 방탄 내구도 제한", ["_normalArmorDropDurability"] = "일반 적 방탄 드롭 내구도", ["_bossArmorDropDurability"] = "보스 방탄 드롭 내구도",
        ["dodgeChance"] = "회피 확률", ["_armor"] = "방어 설정", ["_infection"] = "감염 설정",
        ["_paintMark"] = "페인트 표식", ["_vision"] = "시야 설정",
        ["_visionRange"] = "전방 시야 거리", ["_closeVisionRadius"] = "근접 원형 시야 반경",
        ["_useLineOfSight"] = "벽에 의한 시야 차단", ["_lineOfSightBlockerMask"] = "시야 차단 레이어",
        ["_eyeHeight"] = "눈 높이", ["_targetHeight"] = "대상 확인 높이", ["_useForwardArc"] = "전방 부채꼴 사용",
        ["_baseArcAngle"] = "기본 시야각 (도)", ["_aimArcAngleMultiplier"] = "완전 조준 시 시야각 배율",
        ["_nearForwardArcExtraAngle"] = "근접 원 접합 확장각 (도)",
        ["_useIndividualTargetDetection"] = "이 SO의 개별 시야 사용",
        ["_healthDamageCoefficient"] = "체력 피해 계수", ["_infectionDamageCoefficient"] = "감염 피해 계수",
        ["_markAmountCoefficient"] = "표식량 계수", ["_maxRange"] = "사격 거리",
        ["_shotsPerSecond"] = "초당 발사 수", ["_reloadDurationSeconds"] = "재장전 시간 (초)",
        ["_magazineSize"] = "탄창 크기", ["_penetrationClassBonus"] = "관통 등급 보너스 · 소수점",
        ["_aimSpeed"] = "조준 속도", ["_hipFireSpreadAngleDeg"] = "비조준 탄퍼짐 (도)",
        ["_aimSpreadAngleDeg"] = "조준 탄퍼짐 (도)",
        ["_faction"] = "소속 세력", ["_enemyRole"] = "적 역할", ["_fsmArchetype"] = "행동 유형", ["_enemyShooterPrefab"] = "적 프리팹",
        ["_useMeleeAttack"] = "근접 공격 사용", ["_useThrowableAttack"] = "투척 공격 사용",
        ["_useDash"] = "대시 사용", ["_avoidHarmfulAreas"] = "위험 지역 회피",
        ["_avoidHarmfulAreasWhileAttacking"] = "공격 중 위험 지역 회피",
        ["_useTacticalIntent"] = "전술 판단 사용", ["_useFlankingTactics"] = "측면 공격 · 산개 전술 사용",
        ["_useIndividualCombatStats"] = "개별 전투 능력치 사용", ["_autoSwitchCombatWeaponSlots"] = "전투 무기 슬롯 자동 전환",
        ["_interactConfig"] = "상호작용 설정", ["_infectionDamageOverTimeMultiplier"] = "감염 지속 피해 배율",
        ["_infectionRecoveryTimeWait"] = "감염 회복 대기시간 (초)", ["_infectionDurabilityLossMultiplier"] = "감염에 의한 내구도 손실 배율",
        ["_gunshotSoundRadius"] = "총성 반경", ["_footstepSoundRadius"] = "발소리 반경",
        ["_footstepSoundInterval"] = "발소리 간격 (초)", ["_inventoryCapacity"] = "인벤토리 용량",
        ["_icon"] = "아이콘 Sprite", ["_iconKind"] = "아이콘 없을 때 기본 모양", ["_iconColor"] = "아이콘 색상",
        ["_polarity"] = "효과 분류", ["_description"] = "플레이어용 설명",
        ["_durationSeconds"] = "지속시간 (초)", ["_persistentStacks"] = "치료 전까지 유지 · 중첩형",
        ["_maximumStacks"] = "최대 중첩", ["_damagePerStackPerSecond"] = "중첩당 초당 피해",
        ["_damageTickInterval"] = "피해 적용 간격 (초)", ["_isBleeding"] = "출혈로 분류",
        ["_removeOnFriendlyPaint"] = "아군 페인트로 해제", ["_blocksActions"] = "모든 행동 차단",
        ["_suppressesSoundIndicators"] = "소리 방향 표시 숨김", ["_isInjury"] = "총상으로 분류",
        ["_suppressesInjuryApplications"] = "새 총상 발생 억제", ["_armorClassDelta"] = "방어 등급 증감",
        ["_worldVfxPrefab"] = "월드 효과 프리팹", ["_modifiers"] = "능력치 효과 배율",
        ["_enabled"] = "사용 ON / OFF", ["_sourceId"] = "무적 소스 ID", ["_grantMode"] = "무적 유지 방식",
        ["_useOutlineVisual"] = "윤곽선 사용", ["_outlineColor"] = "윤곽선 색상", ["_outlineWidth"] = "윤곽선 두께",
        ["_outlineFadeInSeconds"] = "나타나는 시간 (초)", ["_outlineFadeOutSeconds"] = "사라지는 시간 (초)",
        ["_outlineColorPropertyName"] = "색상 셰이더 속성", ["_outlineWidthPropertyName"] = "두께 셰이더 속성"
    };
    private static readonly string[] ModifierLabels = { "이동 속도", "감염 회복", "탄퍼짐", "받는 피해", "시야각" };

    internal CharacterStatusMakerInspector(ScriptableObject asset) { _asset = asset; Rebuild(); }

    internal void Rebuild()
    {
        if (_asset == null) return;
        _data = new SerializedObject(_asset);
        var modifiers = _data.FindProperty("_modifiers");
        _modifiers = null;
        if (!(_asset is StatusEffectDefinitionSO) || modifiers == null) return;
        _modifiers = new ReorderableList(_data, modifiers, true, true, true, true)
        {
            elementHeight = EditorGUIUtility.singleLineHeight * 2f + 10f
        };
        _modifiers.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "능력치 효과 · 끌어서 순서 변경 / + 추가 / − 삭제");
        _modifiers.drawElementCallback = (rect, index, active, focused) =>
        {
            var element = _modifiers.serializedProperty.GetArrayElementAtIndex(index);
            var type = element.FindPropertyRelative("type");
            var multiplier = element.FindPropertyRelative("multiplier");
            rect.y += 3f; rect.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.BeginProperty(rect, GUIContent.none, element);
            EditorGUI.BeginChangeCheck();
            int chosen = EditorGUI.Popup(rect, "대상 능력치", type.enumValueIndex, ModifierLabels);
            if (EditorGUI.EndChangeCheck()) type.enumValueIndex = chosen;
            rect.y += EditorGUIUtility.singleLineHeight + 3f;
            EditorGUI.PropertyField(rect, multiplier, new GUIContent("배율 (1 = 변화 없음)"));
            EditorGUI.EndProperty();
        };
        _modifiers.onAddCallback = list =>
        {
            int index = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            var element = list.serializedProperty.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("type").enumValueIndex = 0;
            element.FindPropertyRelative("multiplier").floatValue = 1f;
            list.index = index;
        };
    }

    internal void Draw(bool simple)
    {
        if (_asset == null) return;
        _data.UpdateIfRequiredOrScript();
        bool canWrite = GUI.enabled && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling;
        float originalLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = Mathf.Clamp(EditorGUIUtility.currentViewWidth * .23f, 150f, 230f);
        try
        {
            if (!simple)
            {
                _fieldSearch = EditorGUILayout.TextField("필드 검색 · 이름 / 경로", _fieldSearch);
                DrawAllFields();
            }
            else if (_asset is StatusEffectDefinitionSO) DrawStatus();
            else if (_asset is InvulnerabilityConfigSO) DrawInvulnerability();
            else if (_asset is EntityStatConfigSO) DrawStats();
            else if (_asset is PassiveModuleRulesSO) DrawPassiveRaidRules();
            else
            {
                EditorGUILayout.HelpBox("이 설정 SO의 실제 필드입니다. 연결된 SO는 오른쪽 ↗ 버튼으로 별도 편집할 수 있습니다.", MessageType.None);
                DrawAllFields(false);
            }
            if (canWrite) _data.ApplyModifiedProperties(); // Unity Undo + dirty tracking; never SaveAssets globally.
            else _data.Update();
        }
        finally { EditorGUIUtility.labelWidth = originalLabelWidth; }
    }

    private void DrawPassiveRaidRules()
    {
        EditorGUILayout.HelpBox("원정 중 효과만 적용. 전투/생존 각 4칸은 아래 레벨로 해금합니다. 장착 이력품은 분해 불가·귀환 시 소멸, 미장착품은 반입·시간제 분해 가능합니다. 아래 수치는 임시이며 원본 SO에 저장합니다.", MessageType.Info);
        Section("전투 / 생존 슬롯 · 적용 강도", () =>
        {
            Field("combatSlotLevels"); Field("survivalSlotLevels");
            Field("combatEffectMultiplier"); Field("survivalEffectMultiplier");
            Field("changeCooldownSeconds");
        });
        Section("통합 모듈 · HOME 시작 패시브 추첨", () =>
        {
            Field("unifiedModule"); Field("drawCountLevels"); Field("startingDraws"); Field("startingPool");
            EditorGUILayout.HelpBox("확률 배열 순서: Normal / Rare / Unique / Legendary / Mythic. 비중 합계는 자동 정규화됩니다. 재분해 반환량은 추첨 당시 비용보다 작게 개체별로 제한됩니다.", MessageType.None);
        });
        Section("시간제 분해 · 수치 리롤 · 적 후보", () => { Field("recyclingSeconds"); Field("tiers"); Field("enemyPool"); });
    }

    private void DrawInfectionAggravation()
    {
        Section("감염악화 · 원형 누적 상태이상", () =>
        {
            Field("_id"); DrawIcon(); Field("_description");
            EditorGUILayout.HelpBox("가스/노출 → 원형 한 바퀴마다 최대 HP 잠식. 별도 5초 피해 틱과 감염 ≥ 현재 HP 사망은 없습니다. 자연감소와 앰플은 미완성 게이지만, 주사기/HOME 정화는 잠식을 회복합니다. 방 확산은 이번 변경 대상이 아닙니다.", MessageType.Info);
        });
        Section("누적 · 자연감소", () =>
        {
            var rules = _data.FindProperty("_aggravation");
            EditorGUILayout.PropertyField(rules.FindPropertyRelative("enabled"), new GUIContent("감염악화 사용"));
            EditorGUILayout.PropertyField(rules.FindPropertyRelative("baseGaugeCapacity"), new GUIContent("원형 한 바퀴의 누적량"));
            Field("_maximumStacks");
            EditorGUILayout.PropertyField(rules.FindPropertyRelative("naturalGaugeRecoveryPerSecond"), new GUIContent("초당 자연감소량 · 미완성 게이지만"));
            EditorGUILayout.PropertyField(rules.FindPropertyRelative("naturalRecoveryDelaySeconds"), new GUIContent("마지막 노출 후 자연감소 대기 (초)"));
        });
        Section("최대 체력 잠식", () =>
        {
            EditorGUILayout.PropertyField(_data.FindProperty("_aggravation").FindPropertyRelative("maximumHealthLossPerStack"), new GUIContent("원형 한 바퀴당 최대 HP 감소"));
            EditorGUILayout.HelpBox("이미 부상으로 낮아진 현재 HP에는 중복 피해가 없습니다. 표시 스택 상한에서도 새 원형 완주는 계속 잠식합니다.", MessageType.None);
        });
        Section("감염 요인별 누적량", () =>
        {
            string[] names = { "gasExposurePerPressureSecond", "infectedBulletExposure", "lootOpenExposure", "itemPickupExposure", "hitExposure", "bleedingExposurePerSecond", "infectedRoomHitDamageRatio" };
            string[] labels = { "가스 강도·초당 누적량", "감염 적 탄환 누적량", "루팅 창 열기", "아이템 획득", "일반 피격", "출혈 스택·초당 누적량", "감염방 피격 · 실제 피해 대비 추가 비율" };
            var rules = _data.FindProperty("_aggravation");
            for (int i=0;i<names.Length;i++) EditorGUILayout.PropertyField(rules.FindPropertyRelative(names[i]),new GUIContent(labels[i]));
        });
    }

    private void DrawStatus()
    {
        if (_asset is InfectionAggravationStatusSO) { DrawInfectionAggravation(); return; }
        Section("기본 정보 · 아이콘", () =>
        {
            Field("_id");
            DrawIcon();
            EnumField("_polarity", "효과 분류", new[] { "디버프 · 해로운 효과", "버프 · 이로운 효과", "중립" });
            Field("_description");
            EditorGUILayout.HelpBox("ID는 기존 저장/코드 연결에 사용될 수 있습니다. 이름만 바꾸려면 위의 에셋 이름 변경을 사용하세요.", MessageType.None);
        });
        Section("지속시간 · 중첩 · 지속 피해", () =>
        {
            Field("_persistentStacks");
            bool persistent = _data.FindProperty("_persistentStacks").boolValue;
            using (new EditorGUI.DisabledScope(persistent)) Field("_durationSeconds");
            using (new EditorGUI.DisabledScope(!persistent)) Field("_maximumStacks");
            EditorGUILayout.HelpBox(persistent ? "치료 전까지 유지되며 재적용 시 중첩이 증가합니다." : "재적용 시 남은 시간이 갱신됩니다. 같은 효과가 중복 적용되지는 않습니다.", MessageType.None);
            Field("_damagePerStackPerSecond"); Field("_damageTickInterval"); Field("_isBleeding");
            Field("_removeOnFriendlyPaint");
        });
        Section("효과 배율", () =>
        {
            if (GUI.enabled) _modifiers?.DoLayoutList();
            else EditorGUILayout.PropertyField(_data.FindProperty("_modifiers"), new GUIContent("능력치 효과 배율"), true);
            EditorGUILayout.HelpBox("0.7 = 원래의 70%, 1 = 그대로, 1.5 = 150%. 서로 다른 상태이상의 배율은 곱해집니다.\n예: 이동 속도 0.7 → 감속 / 받는 피해 0.7 → 피해 감소. 같은 대상 항목을 중복 추가하지 않는 것을 권장합니다.", MessageType.Info);
        });
        Section("행동 제한 · 총상 · 시각 효과", () =>
        {
            Field("_blocksActions"); Field("_suppressesSoundIndicators"); Field("_isInjury");
            Field("_suppressesInjuryApplications"); Field("_armorClassDelta"); Field("_worldVfxPrefab");
        });
    }

    private void DrawIcon()
    {
        var icon = _data.FindProperty("_icon");
        using (new EditorGUILayout.HorizontalScope())
        {
            Rect preview = GUILayoutUtility.GetRect(84f, 84f, GUILayout.Width(84f), GUILayout.Height(84f));
            GUI.Box(preview, GUIContent.none);
            var sprite = icon.objectReferenceValue as Sprite;
            if (sprite != null)
            {
                // Respect sprite atlas cropping without creating temporary textures.
                Rect source = sprite.rect;
                float scale = Mathf.Min(76f / source.width, 76f / source.height);
                Rect draw = new Rect(preview.center.x - source.width * scale * .5f,
                    preview.center.y - source.height * scale * .5f, source.width * scale, source.height * scale);
                GUI.DrawTextureWithTexCoords(draw, sprite.texture, new Rect(source.x / sprite.texture.width,
                    source.y / sprite.texture.height, source.width / sprite.texture.width, source.height / sprite.texture.height));
            }
            else GUI.Label(preview, "Sprite\n드래그", EditorStyles.centeredGreyMiniLabel);
            HandleIconDrop(preview, icon);
            using (new EditorGUILayout.VerticalScope())
            {
                Field("_icon");
                EnumField("_iconKind", "기본 아이콘", new[] { "사용자 지정", "감속", "감염 회복 감소", "페인트 코팅" });
                Field("_iconColor");
            }
        }
        EditorGUILayout.LabelField("이미지는 Sprite로 가져와 드래그하세요. 아이콘이 비어 있으면 기존 HUD 기본 모양을 사용합니다.", EditorStyles.wordWrappedMiniLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            _iconImage = EditorGUILayout.ObjectField("PNG 등 이미지 준비", _iconImage, typeof(Texture2D), false) as Texture2D;
            using (new EditorGUI.DisabledScope(_iconImage == null))
                if (GUILayout.Button("Sprite로 준비 · 적용", GUILayout.Width(145f))) PrepareIconImage();
        }
    }

    private void PrepareIconImage()
    {
        if (!GUI.enabled || _iconImage == null) return;
        string path = AssetDatabase.GetAssetPath(_iconImage);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null || !path.StartsWith("Assets/", StringComparison.Ordinal))
        { EditorUtility.DisplayDialog("아이콘 준비", "Assets 안의 이미지 에셋을 선택하세요.", "확인"); return; }
        if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Multiple)
        { EditorUtility.DisplayDialog("아이콘 선택", "여러 Sprite가 포함된 이미지입니다. Project에서 이미지를 펼친 뒤 원하는 Sprite를 아이콘 칸에 넣으세요.", "확인"); return; }
        if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
        {
            if (!EditorUtility.DisplayDialog("이미지 가져오기 설정 변경", path + "\n\n이 이미지 전체를 Sprite (Single)로 가져옵니다. 같은 이미지를 사용하는 다른 에셋에도 영향이 있으므로 전용 아이콘 이미지 사용을 권장합니다.", "Sprite로 변경", "취소")) return;
            _data.ApplyModifiedProperties();
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
            _data.Update();
        }
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null) _data.FindProperty("_icon").objectReferenceValue = sprite;
    }

    private static void HandleIconDrop(Rect rect, SerializedProperty icon)
    {
        Event evt = Event.current;
        if (!GUI.enabled || !rect.Contains(evt.mousePosition) ||
            (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform)) return;
        Sprite sprite = null;
        foreach (var obj in DragAndDrop.objectReferences)
        {
            if (obj is Sprite value) { sprite = value; break; }
            if (obj is Texture2D)
            {
                var sprites = new List<Sprite>();
                foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(obj)))
                    if (sub is Sprite candidate) sprites.Add(candidate);
                if (sprites.Count == 1) { sprite = sprites[0]; break; }
            }
        }
        DragAndDrop.visualMode = sprite != null ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
        if (evt.type == EventType.DragPerform && sprite != null)
        { DragAndDrop.AcceptDrag(); icon.objectReferenceValue = sprite; }
        evt.Use();
    }

    private void DrawInvulnerability()
    {
        Section("무적 설정", () =>
        {
            Field("_enabled"); Field("_sourceId");
            EnumField("_grantMode", "무적 유지 방식", new[] { "시간제", "활성 상태 동안 유지 · 상시용" });
            using (new EditorGUI.DisabledScope(_data.FindProperty("_grantMode").enumValueIndex != 0)) Field("_durationSeconds");
            EditorGUILayout.HelpBox("이 SO가 연결된 기존 무적 시스템의 설정을 바꿉니다.\n'활성 상태 동안 유지'는 기존 호출부가 무적을 시작한 뒤 해제할 때까지 유지합니다. SO를 만드는 것만으로 NPC에 자동 적용되지는 않습니다.", MessageType.Info);
        });
        Section("무적 윤곽선", () =>
        {
            Field("_useOutlineVisual");
            using (new EditorGUI.DisabledScope(!_data.FindProperty("_useOutlineVisual").boolValue))
            {
                Field("_outlineColor"); Field("_outlineWidth"); Field("_outlineFadeInSeconds");
                Field("_outlineFadeOutSeconds"); Field("_outlineColorPropertyName"); Field("_outlineWidthPropertyName");
            }
        });
    }

    private void DrawStats()
    {
        _statTab = GUILayout.Toolbar(_statTab, StatTabs);
        EditorGUILayout.Space(6f);
        if (_statTab == 1)
        {
            var individual = _data.FindProperty("_useIndividualTargetDetection");
            if (_asset is EnemyShooterConfigSO enemy && individual != null)
            {
                EditorGUI.BeginChangeCheck();
                bool useLocal = EditorGUILayout.ToggleLeft("공통 적 시야 대신 개별 Override", individual.boolValue);
                if (EditorGUI.EndChangeCheck())
                {
                    var initialized = _data.FindProperty("_useIndividualTargetDetectionInitialized");
                    if (useLocal && enemy.CommonRules != null && initialized != null && !initialized.boolValue)
                    {
                        CopyVisibleSettings(new SerializedObject(enemy.CommonRules).FindProperty("_enemyVision"), _data.FindProperty("_vision"));
                        initialized.boolValue = true;
                    }
                    individual.boolValue = useLocal;
                }
            }
            bool shared = _asset is EnemyShooterConfigSO && individual != null && !individual.boolValue;
            if (shared)
            {
                Field("_commonRules");
                EditorGUILayout.HelpBox("공통 적 시야 사용 중입니다. 전체 변경은 공통 원본, 이 적만 변경하려면 Override를 켜세요.", MessageType.Info);
                var rules = ((EnemyShooterConfigSO)_asset).CommonRules;
                if (rules != null) DrawReadOnlySettings(new SerializedObject(rules).FindProperty("_enemyVision"));
            }
            else Field("_vision");
            var vision = _data.FindProperty("_vision");
            var angle = vision?.FindPropertyRelative("_baseArcAngle");
            var aim = vision?.FindPropertyRelative("_aimArcAngleMultiplier");
            if (!shared && angle != null && aim != null)
                EditorGUILayout.HelpBox($"완전 조준 시 시야각: {angle.floatValue:0.##}° × {aim.floatValue:0.###} = {angle.floatValue * aim.floatValue:0.##}°\n시야 거리와 사격 거리는 서로 독립입니다.", MessageType.None);
            return;
        }
        if (_statTab == 0)
        {
            DrawTopLevel(property => StatFields.Contains(property.name) && property.name != "_commonRules" &&
                !property.name.StartsWith("_overrideCommon", StringComparison.Ordinal));
            if (_asset is ShooterStatConfigSO) DrawSharedRules();
        }
        else DrawAiFields();
    }

    private void DrawSharedRules()
    {
        if (!_foldouts.TryGetValue("sharedRules", out bool open)) open = false;
        open = EditorGUILayout.Foldout(open, "공통 규칙 · 기본 공유 / 펼쳐서 Override", true);
        _foldouts["sharedRules"] = open;
        if (!open) return;
        Field("_commonRules");
        var shooter = (ShooterStatConfigSO)_asset;
        var shared = shooter.CommonRules;
        EditorGUILayout.HelpBox("공통값은 아래에서 읽기 전용으로 확인합니다. 개별 수정은 Override ON, 전체 수정은 공통 원본 SO에서 하세요. OFF로 돌리면 다시 공유값을 사용합니다.", MessageType.Info);
        if (shared == null)
            EditorGUILayout.HelpBox("공통 규칙 원본이 없습니다. 공유 사용을 위해 ShooterCommonRules SO를 연결하세요.", MessageType.Warning);
        DrawSharedOverride("이동 · 소모 · 회복", "_overrideCommonLocomotion", shared, "_locomotion");
        DrawSharedOverride("피격 흔들림 · 명중 저하", "_overrideCommonAimFeedback", shared, "_aimFeedback");
        if (_asset is EnemyShooterConfigSO)
            DrawSharedOverride("방탄 드롭 범위", "_overrideCommonArmorDrop", shared, "_normalArmorDropDurability", "_bossArmorDropDurability");
    }

    private void DrawSharedOverride(string title, string toggleName, ScriptableObject shared, params string[] fields)
    {
        var toggle = _data.FindProperty(toggleName);
        if (toggle == null) return;
        var source = shared != null ? new SerializedObject(shared) : null;
        EditorGUI.BeginChangeCheck();
        bool local = EditorGUILayout.ToggleLeft(title + " · Override", toggle.boolValue);
        if (EditorGUI.EndChangeCheck())
        {
            var initialized = _data.FindProperty(toggleName + "Initialized");
            if (local && source != null && initialized != null && !initialized.boolValue)
            {
                foreach (string field in fields)
                    CopyVisibleSettings(source.FindProperty(field), _data.FindProperty(field));
                initialized.boolValue = true;
            }
            toggle.boolValue = local;
        }
        if (local)
        {
            foreach (string field in fields) DrawLocalOverride(field);
        }
        else if (source != null)
            foreach (string field in fields)
                if (source.FindProperty(field) is { } property) DrawReadOnlySettings(property);
    }

    private void DrawLocalOverride(string field)
    {
        var root = _data.FindProperty(field);
        if (root == null) return;
        if (!OverrideSettingFields.TryGetValue(field, out var childNames))
        {
            DrawProperty(root);
            return;
        }
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(Label(root) + " · 개별값", EditorStyles.miniBoldLabel);
            foreach (string childName in childNames)
            {
                var child = root.FindPropertyRelative(childName);
                if (child != null) DrawProperty(child);
            }
        }
    }

    private static void DrawReadOnlySettings(SerializedProperty property)
    {
        if (property.propertyType != SerializedPropertyType.Generic)
        {
            using (new EditorGUI.DisabledScope(true)) DrawProperty(property);
            return;
        }
        EditorGUILayout.LabelField(Label(property) + " · 공통값", EditorStyles.miniBoldLabel);
        var child = property.Copy();
        var end = child.GetEndProperty();
        bool enter = true;
        while (child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end))
        {
            enter = false;
            using (new EditorGUI.DisabledScope(true)) DrawProperty(child.Copy());
        }
    }

    private static void CopyVisibleSettings(SerializedProperty source, SerializedProperty target)
    {
        if (source == null || target == null) return;
        switch (source.propertyType)
        {
            case SerializedPropertyType.Boolean: target.boolValue = source.boolValue; return;
            case SerializedPropertyType.Integer:
            case SerializedPropertyType.Enum:
            case SerializedPropertyType.LayerMask: target.intValue = source.intValue; return;
            case SerializedPropertyType.Float: target.floatValue = source.floatValue; return;
            case SerializedPropertyType.Vector2: target.vector2Value = source.vector2Value; return;
            case SerializedPropertyType.Vector3: target.vector3Value = source.vector3Value; return;
        }
        var child = source.Copy();
        var end = child.GetEndProperty();
        bool enter = true;
        while (child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end))
        {
            enter = false;
            CopyVisibleSettings(child, target.FindPropertyRelative(child.name));
        }
    }

    private void DrawAiFields()
    {
        if (_asset is EnemyShooterConfigSO)
        {
            if (!_foldouts.TryGetValue("sprintAi", out bool sprintOpen)) sprintOpen = false;
            sprintOpen = EditorGUILayout.Foldout(sprintOpen, "달리기 AI · 개별 판단 거리", true);
            _foldouts["sprintAi"] = sprintOpen;
            if (sprintOpen)
            {
                Field("_locomotion.enemyCanSprint"); Field("_locomotion.enemySprintStartDistance");
                Field("_locomotion.enemySprintStopDistance");
            }
        }
        _aiSearch = EditorGUILayout.TextField("AI · 연결 필드 검색", _aiSearch);
        var groups = new List<SerializedProperty>[AiGroups.Length];
        for (int i = 0; i < groups.Length; i++) groups[i] = new List<SerializedProperty>();
        var iterator = _data.GetIterator();
        bool first = true;
        while (iterator.NextVisible(first))
        {
            first = false;
            if (iterator.name == "m_Script" || StatFields.Contains(iterator.name) || VisionFields.Contains(iterator.name)) continue;
            if (!string.IsNullOrWhiteSpace(_aiSearch) &&
                (iterator.name + " " + iterator.displayName + " " + Label(iterator)).IndexOf(_aiSearch.Trim(), StringComparison.OrdinalIgnoreCase) < 0) continue;
            groups[AiGroup(iterator.name)].Add(iterator.Copy());
        }
        for (int i = 0; i < groups.Length; i++)
        {
            if (groups[i].Count == 0) continue;
            string title = AiGroups[i];
            if (!_foldouts.TryGetValue(title, out bool open)) open = i == 0;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                open = EditorGUILayout.Foldout(open, title + " · " + groups[i].Count + "개", true);
                _foldouts[title] = open;
                if (open || !string.IsNullOrWhiteSpace(_aiSearch))
                    foreach (var property in groups[i]) DrawProperty(property);
            }
        }
    }

    private static int AiGroup(string field)
    {
        string key = field.ToLowerInvariant();
        if (key.Contains("prefab") || key.Contains("appearance") || key.Contains("spawnlook") ||
            field == "_enemy" || field == "_enemyRole" || field == "_faction" || field == "_fsmArchetype") return 0;
        if (key.Contains("melee")) return 3;
        if (key.Contains("throwable")) return 4;
        if (key.Contains("dash") || key.Contains("dodge")) return 5;
        if (key.Contains("tactic") || key.Contains("flank") || key.Contains("reposition") || key.Contains("harmful") ||
            key.Contains("separation") || key.Contains("formation") || key.Contains("movement") || key.Contains("spacing")) return 1;
        if (key.Contains("target") || key.Contains("detect") || key.Contains("sound") || key.Contains("investigat") || key.Contains("reaction")) return 2;
        if (key.Contains("attack") || key.Contains("combat") || key.Contains("aim") || key.Contains("weapon") || key.Contains("fire")) return 6;
        return 7;
    }

    private void DrawAllFields(bool search = true)
    {
        if (!search || string.IsNullOrWhiteSpace(_fieldSearch))
        { DrawTopLevel(property => true); return; }
        var iterator = _data.GetIterator();
        bool enter = true;
        int count = 0;
        while (iterator.NextVisible(enter))
        {
            enter = true;
            if (iterator.name == "m_Script") continue;
            string terms = iterator.propertyPath + " " + iterator.displayName + " " + Label(iterator);
            if (terms.IndexOf(_fieldSearch.Trim(), StringComparison.OrdinalIgnoreCase) < 0) continue;
            DrawProperty(iterator.Copy());
            count++;
            enter = false;
        }
        if (count == 0) EditorGUILayout.HelpBox("일치하는 필드가 없습니다.", MessageType.None);
    }

    private void DrawTopLevel(Func<SerializedProperty, bool> include)
    {
        var iterator = _data.GetIterator();
        bool first = true;
        while (iterator.NextVisible(first))
        {
            first = false;
            if (iterator.name != "m_Script" && include(iterator)) DrawProperty(iterator.Copy());
        }
    }

    private void Field(string name)
    {
        var property = _data.FindProperty(name);
        if (property != null) DrawProperty(property);
    }

    private static string Label(SerializedProperty property) => Labels.TryGetValue(property.name, out string label) ? label : property.displayName;

    private static void DrawProperty(SerializedProperty property)
    {
        if (property.propertyType == SerializedPropertyType.Generic && !property.isArray && property.hasVisibleChildren)
        {
            // Collapsed by default: keep technical/profile settings out of the everyday stat view.
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, Label(property), true);
                if (!property.isExpanded) return;
                var child = property.Copy();
                var end = child.GetEndProperty();
                bool enter = true;
                while (child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end))
                { enter = false; DrawProperty(child.Copy()); }
            }
        }
        else if (property.propertyType == SerializedPropertyType.ObjectReference)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(property, new GUIContent(Label(property), property.tooltip), true);
                using (new EditorGUI.DisabledScope(property.objectReferenceValue == null))
                    if (GUILayout.Button(new GUIContent("↗", "연결된 SO는 Maker에서 열기 / 그 외는 Project에서 찾기"), GUILayout.Width(25f)))
                    {
                        var referenced = property.objectReferenceValue as ScriptableObject;
                        if (referenced != null && CharacterStatusMakerWindow.Supports(referenced.GetType()))
                        {
                            property.serializedObject.ApplyModifiedProperties();
                            CharacterStatusMakerWindow.OpenAsset(referenced);
                            GUIUtility.ExitGUI();
                        }
                        else { Selection.activeObject = property.objectReferenceValue; EditorGUIUtility.PingObject(property.objectReferenceValue); }
                    }
            }
        }
        else EditorGUILayout.PropertyField(property, new GUIContent(Label(property), property.tooltip), true);
    }

    private void EnumField(string name, string label, string[] choices)
    {
        var property = _data.FindProperty(name);
        if (property == null) return;
        EditorGUI.BeginChangeCheck();
        int value = EditorGUILayout.Popup(new GUIContent(label, property.tooltip), property.enumValueIndex, choices);
        if (EditorGUI.EndChangeCheck()) property.enumValueIndex = value;
    }

    private static void Section(string title, Action draw)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        { GUILayout.Label(title, EditorStyles.boldLabel); draw(); }
        EditorGUILayout.Space(5f);
    }
}
