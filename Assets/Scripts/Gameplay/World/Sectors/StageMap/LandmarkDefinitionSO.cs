using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName="Game/Stage/Landmark",fileName="Landmark")]
public sealed class LandmarkDefinitionSO : ScriptableObject
{
    public string id;
    public string displayName;
    public bool powerStation;
    public bool bossEligible = true;
    public SectorMapIconKind mapIconKind = SectorMapIconKind.ThemedCombat;
    public List<LandmarkPrefab> variants = new();
}

[Serializable]
public sealed class StageLandmarkSettings
{
    [Tooltip("Enable after registering authored compound prefabs. Old stages retain their saved/legacy generator.")]
    public bool enabled;
    public List<LandmarkDefinitionSO> guaranteed = new();
    public LandmarkDefinitionSO Find(string id) => guaranteed.Find(x=>x!=null&&x.id==id);
}
