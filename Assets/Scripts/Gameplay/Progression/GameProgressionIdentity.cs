using UnityEngine;

/// <summary>Stable production spawn ordinal; corpse deletion never changes another enemy's XP identity.</summary>
[DisallowMultipleComponent]
public sealed class GameProgressionIdentity:MonoBehaviour
{
    public string Key {get;private set;}
    public void Configure(string key)=>Key=key;
}
