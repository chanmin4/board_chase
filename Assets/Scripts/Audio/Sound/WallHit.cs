using UnityEngine;

public class WallHit : MonoBehaviour
{
    public string key = "sfx.wallhit";

    void OnEnable()  { WallHitReporter.OnWallHit += Handle; }
    void OnDisable() { WallHitReporter.OnWallHit -= Handle; }

    void Handle(Vector3 point, Vector3 normal, float preSpeed)
    {
        if (AudioMaster.I) AudioMaster.I.PlayKey(key, point);
    }
}
