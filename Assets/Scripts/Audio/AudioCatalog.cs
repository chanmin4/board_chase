using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor; // 에디터 전용 API
#endif

public enum AudioChannel { BGM,UI, SFX,VOICE,AMB,OTHERS }

[CreateAssetMenu(menuName = "Audio/AudioCatalog")]
public class AudioCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string key;             // 예: "bgm.main", "sfx.wall.hit"
        public AudioChannel channel;   // BGM or SFX
        public AudioEvent ev;
    }

    public List<Entry> items = new();
    Dictionary<string, Entry> _map;

    void OnEnable() { Build(); }

    public void Build()
    {
        _map = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        foreach (var it in items)
            if (!string.IsNullOrEmpty(it.key) && it.ev)
                _map[it.key] = it;
    }

    public bool TryGet(string key, out Entry e)
    {
        if (_map == null) Build();
        return _map.TryGetValue(key, out e);
    }

#if UNITY_EDITOR
 [MenuItem("CONTEXT/AudioCatalog/Reset", false, 0)]
    static void ResetWithConfirm(MenuCommand cmd)
    {
        var cat = (AudioCatalog)cmd.context;
        bool ok = EditorUtility.DisplayDialog(
            "Reset Audio Catalog?",
            "카탈로그를 초기화하시겠어요?\n(되돌리기: Ctrl+Z)",
            "Reset", "Cancel"
        );
        if (!ok) return;

        Undo.RecordObject(cat, "Reset Audio Catalog");
        // ‘완전 초기화 금지’로 쓰고 싶으면 다음 두 줄 주석 처리
        cat.items?.Clear();
        cat.Build();

        EditorUtility.SetDirty(cat);
        AssetDatabase.SaveAssets();
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        EditorApplication.QueuePlayerLoopUpdate();
    }

    // 필요 시 조건부 비활성화(회색 처리)
    [MenuItem("CONTEXT/AudioCatalog/Reset", true)]
    static bool ValidateReset(MenuCommand cmd) => !EditorApplication.isPlaying;



    // ---------- 공통 유틸 ----------
    static AudioChannel ChannelForTop(string top)
        => string.Equals(top, "bgm", StringComparison.OrdinalIgnoreCase) ? AudioChannel.BGM : AudioChannel.SFX;

    static string DefaultKeyForTop(string top)
        => string.Equals(top, "others", StringComparison.OrdinalIgnoreCase) ? "new.key" : $"{top}.new.entry";

    static void AddEmptyForTop_Internal(AudioCatalog cat, string top)
    {
        if (!cat) return;
        if (cat.items == null) cat.items = new();
        cat.items.Add(new Entry
        {
            key = DefaultKeyForTop(top),
            channel = ChannelForTop(top),
            ev = null
        });
        cat.Build();
        EditorUtility.SetDirty(cat);
        AssetDatabase.SaveAssets();
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        EditorApplication.QueuePlayerLoopUpdate();
        Debug.Log($"[AudioCatalog] Added empty entry ({top})");
    }

    // ---------- 인스펙터 점(⋮) 메뉴 ----------
    [ContextMenu("Add Empty Entry/BGM")]   void Ctx_Add_BGM()   => AddEmptyForTop_Internal(this, "bgm");
    [ContextMenu("Add Empty Entry/UI")]    void Ctx_Add_UI()    => AddEmptyForTop_Internal(this, "ui");
    [ContextMenu("Add Empty Entry/SFX")]   void Ctx_Add_SFX()   => AddEmptyForTop_Internal(this, "sfx");
    [ContextMenu("Add Empty Entry/VOICE")] void Ctx_Add_VOICE() => AddEmptyForTop_Internal(this, "voice");
    [ContextMenu("Add Empty Entry/AMB")]   void Ctx_Add_AMB()   => AddEmptyForTop_Internal(this, "amb");
    [ContextMenu("Add Empty Entry/OTHERS")]void Ctx_Add_OTHERS()=> AddEmptyForTop_Internal(this, "others");

    [ContextMenu("Sort by Namespace (bgm/ui/sfx/voice/amb)")]
    void Editor_SortByNamespace()
    {
        int Rank(string top) => top switch
        {
            "bgm"  => 0,
            "ui"   => 1,
            "sfx"  => 2,
            "voice"=> 3,
            "amb"  => 4,
            _      => 5
        };

        int Cmp(string a, string b)
        {
            var A = (a ?? "").Trim().ToLowerInvariant().Split('.');
            var B = (b ?? "").Trim().ToLowerInvariant().Split('.');

            int ra = Rank(A.Length > 0 ? A[0] : "");
            int rb = Rank(B.Length > 0 ? B[0] : "");
            if (ra != rb) return ra.CompareTo(rb);

            for (int i = 1; i < Mathf.Max(A.Length, B.Length); i++)
            {
                string ai = i < A.Length ? A[i] : "";
                string bi = i < B.Length ? B[i] : "";
                int c = string.CompareOrdinal(ai, bi);
                if (c != 0) return c;
            }
            return string.CompareOrdinal(a, b);
        }

        items.Sort((x, y) => Cmp(x?.key, y?.key));
        Build();
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        EditorApplication.QueuePlayerLoopUpdate();
        Debug.Log("[AudioCatalog] Sorted by namespace");
    }

    // ---------- 프로젝트 창 우클릭(Assets) 메뉴 ----------
    [MenuItem("Assets/Audio Catalog/Add Empty/BGM", true)]
    static bool V_BGM() => Selection.activeObject is AudioCatalog;
    [MenuItem("Assets/Audio Catalog/Add Empty/BGM")]
    static void M_BGM()  => AddEmptyForTop_Internal(Selection.activeObject as AudioCatalog, "bgm");

    [MenuItem("Assets/Audio Catalog/Add Empty/UI", true)]
    static bool V_UI() => Selection.activeObject is AudioCatalog;
    [MenuItem("Assets/Audio Catalog/Add Empty/UI")]
    static void M_UI()  => AddEmptyForTop_Internal(Selection.activeObject as AudioCatalog, "ui");

    [MenuItem("Assets/Audio Catalog/Add Empty/SFX", true)]
    static bool V_SFX() => Selection.activeObject is AudioCatalog;
    [MenuItem("Assets/Audio Catalog/Add Empty/SFX")]
    static void M_SFX() => AddEmptyForTop_Internal(Selection.activeObject as AudioCatalog, "sfx");

    [MenuItem("Assets/Audio Catalog/Add Empty/VOICE", true)]
    static bool V_VOICE() => Selection.activeObject is AudioCatalog;
    [MenuItem("Assets/Audio Catalog/Add Empty/VOICE")]
    static void M_VOICE()=> AddEmptyForTop_Internal(Selection.activeObject as AudioCatalog, "voice");

    [MenuItem("Assets/Audio Catalog/Add Empty/AMB", true)]
    static bool V_AMB() => Selection.activeObject is AudioCatalog;
    [MenuItem("Assets/Audio Catalog/Add Empty/AMB")]
    static void M_AMB() => AddEmptyForTop_Internal(Selection.activeObject as AudioCatalog, "amb");

    [MenuItem("Assets/Audio Catalog/Add Empty/OTHERS", true)]
    static bool V_OTHERS() => Selection.activeObject is AudioCatalog;
    [MenuItem("Assets/Audio Catalog/Add Empty/OTHERS")]
    static void M_OTHERS()=> AddEmptyForTop_Internal(Selection.activeObject as AudioCatalog, "others");
#endif // UNITY_EDITOR
}
