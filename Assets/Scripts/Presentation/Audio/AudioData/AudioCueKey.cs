/*
 * Role hierarchy: Presentation > Audio > AudioData
 * Primary responsibility: Handles AudioCueKey audio playback and sound-stimulus delivery.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;

[Serializable]
public struct AudioCueKey : IEquatable<AudioCueKey>
{
    public int id;
    public static readonly AudioCueKey Invalid = new AudioCueKey { id = 0 };

    private static int _nextId = 1;
    // Purpose: Creates the create owned by this workflow.
    public static AudioCueKey Create() => new AudioCueKey { id = _nextId++ };

    public bool IsValid => id != 0;
    // Purpose: Performs the equals workflow for this component.
    public bool Equals(AudioCueKey other) => id == other.id;
    // Purpose: Performs the equals workflow for this component.
    public override bool Equals(object obj) => obj is AudioCueKey other && Equals(other);
    // Purpose: Returns the current hash code.
    public override int GetHashCode() => id;
    // Purpose: Performs the to string workflow for this component.
    public override string ToString() => IsValid ? $"AudioCueKey({id})" : "AudioCueKey(Invalid)";

    public static bool operator ==(AudioCueKey a, AudioCueKey b) => a.id == b.id;
    public static bool operator !=(AudioCueKey a, AudioCueKey b) => a.id != b.id;
}
