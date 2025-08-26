using System;
using System.Collections.Generic;
using UnityEngine;

public enum AudioChannel { BGM, SFX }

[CreateAssetMenu(menuName="Audio/AudioCatalog")]
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
}
