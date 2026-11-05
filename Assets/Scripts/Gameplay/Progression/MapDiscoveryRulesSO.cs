using System;
using UnityEngine;

/// <summary>Independent 지도 발견 표시 source. Runtime never reads the retired aggregate.</summary>
[CreateAssetMenu(fileName = "MapDiscoveryRules", menuName = "Game/Systems/지도 발견 표시")]
public sealed class MapDiscoveryRulesSO : ScriptableObject
{
    private static MapDiscoveryRulesSO _active;
    public static MapDiscoveryRulesSO Active => _active != null ? _active : _active = Resources.Load<MapDiscoveryRulesSO>("MapDiscoveryRules");
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() => _active = null;
    public MapDiscoveryRules map = new();
}

[Serializable]
public sealed class MapDiscoveryRules
{
    public bool hideUnvisited = true;
    public bool hideBossIcons = true;
}
