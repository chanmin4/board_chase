using UnityEngine;

[CreateAssetMenu(menuName="Game/Systems/근거리 팩션 발견 공유",fileName="FactionDiscoveryRules")]
public sealed class FactionDiscoveryRulesSO : ScriptableObject
{
    public bool enabled=true;
    [Min(0)] public float radius=45f;
    [Min(.05f)] public float visibleRefreshSeconds=.5f;
    public bool sameSectorOnly;
    public static FactionDiscoveryRulesSO Active => Resources.Load<FactionDiscoveryRulesSO>("FactionDiscoveryRules");
    public static bool InRange(Vector3 source,Vector3 receiver,float radius)
    {
        source.y=receiver.y=0f;
        return radius>0f&&(source-receiver).sqrMagnitude<=radius*radius;
    }
}
