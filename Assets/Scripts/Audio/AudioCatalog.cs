using System;
using System.Collections.Generic;
using UnityEngine;

public enum AudioChannel { BGM, SFX }

[CreateAssetMenu(menuName = "Audio/AudioCatalog")]
public class AudioCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string key;          // 예: "bgm.main", "sfx.wall.hit"
        public AudioChannel channel; // BGM or SFX
        public AudioEvent ev;
    }

    public List<Entry> items = new();
    Dictionary<string, Entry> _map;

    void OnEnable() { Build(); }
    public void Build()
    {
        _map = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        foreach (var it in items)
            if (!string.IsNullOrEmpty(it.key) && it.ev) _map[it.key] = it;
    }

    public bool TryGet(string key, out Entry e)
    {
        if (_map == null) Build();
        return _map.TryGetValue(key, out e);
    }

#if UNITY_EDITOR
    [ContextMenu("Sort by Namespace (bgm/ui/sfx/voice/amb)")]
    void Editor_Sort()
    {
        int Rank(string top) => top switch
        {
            "bgm" => 0,
            "ui" => 1,
            "sfx" => 2,
            "voice" => 3,
            "amb" => 4,
            _ => 5
        };
        int Cmp(string a, string b)
        {
            var A = (a ?? "").Trim().ToLowerInvariant().Split('.');
            var B = (b ?? "").Trim().ToLowerInvariant().Split('.');
            int ra = Rank(A.Length > 0 ? A[0] : ""), rb = Rank(B.Length > 0 ? B[0] : "");
            if (ra != rb) return ra.CompareTo(rb);
            // 2차: 도메인/맥락, 3차: 세부 액션
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
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
    }


#endif


}

