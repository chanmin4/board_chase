using System.Collections.Generic;

public class SoundEmitterVault
{
    private readonly Dictionary<AudioCueKey, SoundEmitter[]> _emittersByKey = new();

    public AudioCueKey GetKey(AudioCueSO cue)
    {
        return AudioCueKey.Create();
    }

    public void Add(AudioCueKey key, SoundEmitter[] emitters)
    {
        _emittersByKey[key] = emitters;
    }

    public AudioCueKey Add(AudioCueSO cue, SoundEmitter[] emitters)
    {
        AudioCueKey key = GetKey(cue);
        Add(key, emitters);
        return key;
    }

    public bool Get(AudioCueKey key, out SoundEmitter[] emitters)
    {
        return _emittersByKey.TryGetValue(key, out emitters);
    }

    public bool Remove(AudioCueKey key)
    {
        return _emittersByKey.Remove(key);
    }
}
