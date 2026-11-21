using UnityEngine;

[CreateAssetMenu(menuName="Game/Systems/루팅 소음",fileName="LootNoiseRules")]
public sealed class LootNoiseRulesSO : ScriptableObject
{
    [Min(0)] public float radius=75f;
    public SoundStimulusEventChannelSO soundChannel;
    public static LootNoiseRulesSO Active => Resources.Load<LootNoiseRulesSO>("LootNoiseRules");
    public static void Emit(GameObject actor, Vector3 position)
    {
        var rules=Active;
        float radius=rules!=null?rules.radius:75f;
        if(actor==null||radius<=0)return;
        var stimulus=new SoundStimulus(actor,position,radius,0f,SoundStimulusType.Impact,.65f,
            actor.GetInstanceID(),Vector3.zero);
        if(rules?.soundChannel!=null)rules.soundChannel.RaiseEvent(stimulus);
        else foreach(var channel in Resources.FindObjectsOfTypeAll<SoundStimulusEventChannelSO>())
            channel.RaiseEvent(stimulus);
    }
}
