using UnityEngine;

public class WallHit : MonoBehaviour
{
    [Tooltip("카탈로그 키. 예: sfx.wall.hit")]
    public string key = "sfx.wallhit";

    void OnEnable()  { WallHitReporter.OnWallHit += Handle; }
    void OnDisable() { WallHitReporter.OnWallHit -= Handle; }

    void Handle(Vector3 point, Vector3 normal, float preSpeed)
    {
        if (AudioMaster.I) AudioMaster.I.PlayKey(key, point);
    }
}
