/*
 * Role hierarchy: Presentation > Audio
 * Primary responsibility: Handles WorldGameAudio audio playback and sound-stimulus delivery.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

// WorldGameAudio.cs
using UnityEngine;

[DisallowMultipleComponent]
public class WorldGameAudio : MonoBehaviour
{
    // Reserved for non-player world audio later.
    // Player-driven interact/pickup audio is handled by PlayerShooterCharacterAudio.
}