using System;

[Serializable]
public struct AudioCueKey : IEquatable<AudioCueKey>
{
    public int id;
    public static readonly AudioCueKey Invalid = new AudioCueKey { id = 0 };

    private static int _nextId = 1;
    public static AudioCueKey Create() => new AudioCueKey { id = _nextId++ };

    public bool IsValid => id != 0;
    public bool Equals(AudioCueKey other) => id == other.id;
    public override bool Equals(object obj) => obj is AudioCueKey other && Equals(other);
    public override int GetHashCode() => id;
    public override string ToString() => IsValid ? $"AudioCueKey({id})" : "AudioCueKey(Invalid)";

    public static bool operator ==(AudioCueKey a, AudioCueKey b) => a.id == b.id;
    public static bool operator !=(AudioCueKey a, AudioCueKey b) => a.id != b.id;
}
