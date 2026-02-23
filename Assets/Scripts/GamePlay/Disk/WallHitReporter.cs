using UnityEngine;
using System;
public class WallHitReporter : MonoBehaviour
{   
    public static event Action<Vector3, Vector3, float> OnWallHit;
    public SurvivalDirector director;
    public LayerMask wallMask;          // ← 'Wall' 레이어 포함
    public float minSpeed = 0.5f;       // 너무 느린 접촉은 무시
    public float cooldown = 0.08f;      // 연속 접촉 스팸 방지

    Rigidbody rb;
    float lastTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!director) director = FindAnyObjectByType<SurvivalDirector>();
    }

    void OnCollisionEnter(Collision col)
    {

        // 레이어 체크
        if (((1 << col.gameObject.layer) & wallMask) == 0) return;

        // 속도/쿨다운 체크
        float speed = rb ? rb.linearVelocity.magnitude : 0f;
        if (speed < minSpeed) return;
        if (Time.time - lastTime < cooldown) return;
        if (AudioMaster.I) AudioMaster.I.PlayKey("sfx.wallhit");
        //director?.AddWallHit(1);
        lastTime = Time.time;

        var c = col.GetContact(0);
        OnWallHit?.Invoke(c.point, c.normal, speed);
    }

    /*
    // 트리거 벽을 쓸 경우
    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & wallMask) == 0) return;
        if (Time.time - lastTime < cooldown) return;
        director?.AddWallHit(1);
        lastTime = Time.time;
    }
    */
}
