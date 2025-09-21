using UnityEngine;

[CreateAssetMenu(menuName="Audio/AudioEvent")]
public class AudioEvent : ScriptableObject
{
    public AudioClip[] clips;

    [Header("Mix")]
    [Range(0f,3f)] public float volume = 1f;
    public Vector2 volumeJitter = new(0f, 0f);
    public Vector2 pitchJitter  = new(-0.04f, 0.04f);

    [Header("3D")]
    public bool is3D = true;
    public float maxDistance = 35f;

    [Header("Policy")]
    public int  maxVoices = 4;      // 동시 최대
    public float cooldown = 0f;     // 같은 이벤트 연타 방지(초)
    public string concurrencyKey;   // 같은 키끼리 동시 제한

    // 런타임 상태
    [System.NonSerialized] public float lastPlayTime = -999f;
    [System.NonSerialized] public int   playingCount = 0;

    public AudioClip Pick() => (clips != null && clips.Length > 0) ? clips[Random.Range(0, clips.Length)] : null;
}
