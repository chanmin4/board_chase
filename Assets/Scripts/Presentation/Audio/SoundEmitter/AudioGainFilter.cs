/*
 * Role hierarchy: Presentation > Audio > SoundEmitter
 * Primary responsibility: Handles AudioGainFilter audio playback and sound-stimulus delivery.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class AudioGainFilter : MonoBehaviour
{
    [Tooltip("Runtime cue gain in decibels. This is assigned by SoundEmitter from the selected AudioCueSO.")]
    [SerializeField, Range(-24f, 24f)] private float _gainDb;

    private volatile float _linearGain = 1f;

    // Purpose: Updates the gain db state.
    public void SetGainDb(float gainDb)
    {
        _gainDb = Mathf.Clamp(gainDb, -24f, 24f);
        _linearGain = Mathf.Pow(10f, _gainDb / 20f);
    }

    // Unity invokes this on the audio thread. Keep it allocation-free.
    // Purpose: Responds to the audio filter read callback.
    private void OnAudioFilterRead(float[] data, int channels)
    {
        float gain = _linearGain;
        if (gain > 0.9999f && gain < 1.0001f)
            return;

        for (int i = 0; i < data.Length; i++)
            data[i] *= gain;
    }
}
