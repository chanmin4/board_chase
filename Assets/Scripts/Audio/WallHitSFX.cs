using UnityEngine;

/// <summary>
/// 벽 튕김 소리. PlayerDisk에 붙이고 WallHitReporter 이벤트만 구독.
/// 속도에 따라 볼륨/피치 약간 가변.
/// </summary>
public class WallHitSFX : MonoBehaviour
{
    public AudioClip[] clips;
    public float minSpeed = 1.0f;
    public float maxSpeed = 25f;
    public float minVol = 0.2f;
    public float maxVol = 1.0f;
    public float extraPitchJitter = 0.06f;

    void OnEnable()
    {
        WallHitReporter.OnWallHit += Handle;
    }
    void OnDisable()
    {
        WallHitReporter.OnWallHit -= Handle;
    }

    void Handle(Vector3 point, Vector3 normal, float preSpeed)
    {
        if (clips == null || clips.Length == 0) return;
        if (preSpeed < minSpeed) return;

        float t = Mathf.InverseLerp(minSpeed, maxSpeed, preSpeed);
        float vol = Mathf.Lerp(minVol, maxVol, t);
        var clip = clips[Random.Range(0, clips.Length)];

        AudioMaster.I.PlaySFXAt(clip, point, vol, extraPitchJitter);
    }
}
