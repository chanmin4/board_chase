using UnityEngine;

/// <summary>Per-resident voice/interaction cues, played through the shared AudioCue channel.</summary>
[CreateAssetMenu(menuName = "Audio/Profiles/NPC Audio Profile", fileName = "NpcAudioProfile")]
public sealed class NpcAudioProfileSO : DescriptionBaseSO
{
    public AudioCueEventChannelSO channel;
    public AudioConfigurationSO configuration;
    [Tooltip("Temporary non-verbal cues are supplied. Replace these with recorded dialogue when ready.")]
    public AudioCueSO greeting;
    public AudioCueSO farewell;
    public AudioCueSO accept;
    public AudioCueSO reject;
}
