using UnityEditor;
using UnityEngine;

/// <summary>Edits existing source assets, never a disconnected copy of authoring data.</summary>
public sealed class ProgressionFoundationWindow : EditorWindow
{
    [SerializeField] private int _tab;
    [SerializeField] private StageDefinitionSO _stage;
    [SerializeField] private ShooterStatConfigSO _shooter;
    [SerializeField] private PassiveItemSO _passive;
    [SerializeField] private SectorDestructionEffectsSO _effects;
    [SerializeField] private PlayerVisionFogOverlayUI _vision;
    [SerializeField] private ShooterStaminaRuntime _runtime;
    [SerializeField] private ShooterLocomotionSettings _draft = new();
    private int _runtimeId;
    private Vector2 _scroll;
    private CharacterStatusMakerInspector _shooterInspector;
    private ShooterStatConfigSO _inspectedShooter;

    public static void Open()
    {
        var window = GetWindow<ProgressionFoundationWindow>("진행·전투 설정");
        window.minSize = new Vector2(530, 480);
        if (Selection.activeObject is StageDefinitionSO stage) window._stage = stage;
        if (Selection.activeObject is ShooterStatConfigSO shooter) window._shooter = shooter;
        if (Selection.activeObject is PassiveItemSO passive) window._passive = passive;
        window.Show();
    }

    public static void OpenFor(Object asset)
    {
        Open();
        var window = GetWindow<ProgressionFoundationWindow>();
        if (asset is StageDefinitionSO stage) { window._stage = stage; window._tab = 0; }
        if (asset is ShooterStatConfigSO shooter) { window._shooter = shooter; window._tab = 1; }
        if (asset is PassiveItemSO passive) { window._passive = passive; window._tab = 2; }
    }

    private void OnGUI()
    {
        _tab = GUILayout.Toolbar(_tab, new[] { "진행·특별 섹터", "캐릭터·방탄", "패시브", "공용 연출", "실시간" });
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        if (_tab != 4) EditorGUILayout.HelpBox("원본 SO를 직접 편집합니다. Play 중에는 SO 읽기 전용입니다. 변경한 맵·등장 설정은 새 맵 생성부터 적용됩니다.", MessageType.Info);
        switch (_tab)
        {
            case 0:
                _stage = (StageDefinitionSO)EditorGUILayout.ObjectField("스테이지 원본", _stage, typeof(StageDefinitionSO), false);
                EditorGUILayout.HelpBox("해치: 다음 해금된 스테이지로 원정을 유지해 이동. HOME 외곽 탈출과 별개입니다. 특별 섹터는 종류→변형 순이며 진행 중 퀘스트 보장 목록을 여기서 연결합니다.", MessageType.None);
                DrawAsset(_stage, "_raidRuntime", "_specialSectors");
                break;
            case 1:
                _shooter = (ShooterStatConfigSO)EditorGUILayout.ObjectField("플레이어 / 적 Config", _shooter, typeof(ShooterStatConfigSO), false);
                EditorGUILayout.HelpBox("최대 스태미나는 개별 값입니다. 이동·소모·회복 등 공통값은 접힌 공통 규칙에서 확인하고 필요한 항목만 Override하세요. 적 달리기 판단은 AI 탭에 있습니다.", MessageType.None);
                if (_shooter != null)
                {
                    if (_shooterInspector == null || _inspectedShooter != _shooter)
                    { _inspectedShooter = _shooter; _shooterInspector = new CharacterStatusMakerInspector(_shooter); }
                    using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
                        _shooterInspector.Draw(true);
                }
                break;
            case 2:
                _passive = (PassiveItemSO)EditorGUILayout.ObjectField("패시브 원본", _passive, typeof(PassiveItemSO), false);
                EditorGUILayout.HelpBox("랜덤 ON: 생성 순간 최종 희귀도 범위에서 각 행의 값을 한 번 추첨합니다. 범위는 %가 아닌 절대 합연산입니다. 최솟값=최댓값이면 고정값. 이미 생성된 개체는 원본 수정으로 재추첨되지 않습니다.", MessageType.None);
                EditorGUILayout.HelpBox("HOME에서만 장착·해제·교체·수치 리롤이 가능합니다. 귀속품 판매·타인 획득 및 동일 효과 중복 장착은 금지합니다. 분해는 인벤토리 시간제 분해기를 이용합니다.", MessageType.None);
                DrawAsset(_passive, "_randomStatsEnabled", "_rarityRolls", "_allowDuplicatePickup", "_allowDuplicateEquip", "_statModifiers", "_featureFlags");
                if (_passive != null && _passive.RandomStatsEnabled)
                    foreach (var rarity in new[] { PlayerShopItemRarity.Normal, PlayerShopItemRarity.Rare, PlayerShopItemRarity.Unique, PlayerShopItemRarity.Legendary })
                        if (_passive.FindRoll(rarity)?.stats?.Count is not > 0)
                            EditorGUILayout.HelpBox($"{rarity}: 범위가 없습니다. 해당 희귀도로 뽑힐 후보라면 범위를 등록하세요.", MessageType.Warning);
                break;
            case 3:
                _effects = (SectorDestructionEffectsSO)EditorGUILayout.ObjectField("공용 파괴 연기", _effects != null ? _effects : Resources.Load<SectorDestructionEffectsSO>("SectorDestructionEffects"), typeof(SectorDestructionEffectsSO), false);
                DrawAsset(_effects, "_smokePrefab", "_referenceFootprint", "_sizeMultiplier", "_scaleLimits", "_maximumLifetime");
                _vision = (PlayerVisionFogOverlayUI)EditorGUILayout.ObjectField("시야 표현 (씬 객체)", _vision, typeof(PlayerVisionFogOverlayUI), true);
                if (GUILayout.Button("현재 시야 표현 찾기")) _vision = Object.FindFirstObjectByType<PlayerVisionFogOverlayUI>();
                if (_vision != null && _vision.gameObject.scene.IsValid())
                {
                    var data = new SerializedObject(_vision);
                    data.Update();
                    foreach (var key in new[] { "_showVisionBorder", "_normalBorder", "_aimBorder", "_borderWidthPixels", "_aimExtraFogAlpha" })
                        if (data.FindProperty(key) is { } property) EditorGUILayout.PropertyField(property, true);
                    data.ApplyModifiedProperties();
                }
                EditorGUILayout.HelpBox("시야 테두리 설정은 선택한 씬 컴포넌트입니다. 파괴 엄폐 지정·내구도는 Sector Maker의 구조물 배치에서 변경하세요.", MessageType.None);
                break;
            case 4: DrawRuntime(); break;
        }
        EditorGUILayout.EndScrollView();
    }

    private static void DrawAsset(Object asset, params string[] fields)
    {
        if (asset == null) return;
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            var data = new SerializedObject(asset);
            data.Update();
            foreach (var field in fields)
                if (data.FindProperty(field) is { } property) EditorGUILayout.PropertyField(property, true);
            data.ApplyModifiedProperties();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("원본 선택")) Selection.activeObject = asset;
                if (GUILayout.Button("원본 저장")) AssetDatabase.SaveAssetIfDirty(asset);
            }
        }
    }

    private void DrawRuntime()
    {
        EditorGUILayout.HelpBox("Play 중 선택한 플레이어/적 한 개체의 스태미나만 임시 수정합니다. 원본 SO와 저장 데이터에는 쓰지 않습니다. 개체가 사라지면 종료됩니다.", MessageType.Info);
        _runtime = (ShooterStaminaRuntime)EditorGUILayout.ObjectField("실시간 대상", _runtime, typeof(ShooterStaminaRuntime), true);
        if (GUILayout.Button("현재 플레이어 선택"))
        {
            var player = Object.FindFirstObjectByType<PlayerCharacter>();
            _runtime = player != null ? player.GetComponent<ShooterStaminaRuntime>() : null;
        }
        using (new EditorGUI.DisabledScope(!Application.isPlaying || _runtime == null || EditorUtility.IsPersistent(_runtime)))
        {
            if (_runtime == null) return;
            if (_runtimeId != _runtime.GetInstanceID())
            {
                _runtimeId = _runtime.GetInstanceID();
                _draft = JsonUtility.FromJson<ShooterLocomotionSettings>(JsonUtility.ToJson(_runtime.Settings));
                _draft.maximumStamina = _runtime.Maximum;
                _draft.enemyPoolMultiplier = 1f;
                _draft.enemyCanSprint = _runtime.EnemyCanSprint;
                _draft.enemySprintStartDistance = _runtime.EnemySprintStartDistance;
                _draft.enemySprintStopDistance = _runtime.EnemySprintStopDistance;
            }
            EditorGUILayout.LabelField("현재 / 최대", $"{_runtime.Current:0.##} / {_runtime.Maximum:0.##}");
            EditorGUILayout.LabelField("행동", _runtime.IsSprinting ? "달리기" : _runtime.IsExhausted ? "고갈" : "걷기/대기");
            var data = new SerializedObject(this);
            data.Update();
            EditorGUILayout.PropertyField(data.FindProperty("_draft.maximumStamina"), new GUIContent("임시 최대 스태미나 · 실제 최대치"));
            EditorGUILayout.PropertyField(data.FindProperty("_draft"), new GUIContent("임시 이동·스태미나 설정"), true);
            data.ApplyModifiedProperties();
            if (GUILayout.Button("선택 개체에 임시 적용")) _runtime.ApplyTemporarySettings(_draft);
            if (GUILayout.Button("임시 설정 해제 · 원본으로"))
            {
                _runtime.ApplyTemporarySettings(null);
                _runtimeId = 0;
            }
        }
        Repaint();
    }
}
