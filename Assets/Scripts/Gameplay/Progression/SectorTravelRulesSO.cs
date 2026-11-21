using System;
using UnityEngine;

/// <summary>Independent 섹터 이동 source. Runtime never reads the retired aggregate.</summary>
[CreateAssetMenu(fileName = "SectorTravelRules", menuName = "Game/Systems/섹터 이동")]
public sealed class SectorTravelRulesSO : ScriptableObject
{
    private static SectorTravelRulesSO _active;
    public static SectorTravelRulesSO Active => _active != null ? _active : _active = Resources.Load<SectorTravelRulesSO>("SectorTravelRules");
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() => _active = null;
    public SectorTravelRules travel = new();
}

[Serializable]
public sealed class SectorTravelRules
{
    public bool freeSectorTravel = true;
    [HideInInspector] public float portalCooldown; // Legacy serialization only; travel no longer reads this.
    public bool requirePortalActivation = false;
    [Min(0)] public float returnPortalCooldown = 5f;
    [Min(0)] public float portalActivationCast = 5f;
    [Min(0)] public float portalCast = 5f;
}
