using UnityEngine;

public class WallHitReporter : MonoBehaviour
{
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
        if (rb && rb.linearVelocity.magnitude < minSpeed) return;
        if (Time.time - lastTime < cooldown) return;

        director?.AddWallHit(1);
        lastTime = Time.time;
    }

    // (옵션) 트리거 벽을 쓸 경우
    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & wallMask) == 0) return;
        if (Time.time - lastTime < cooldown) return;
        director?.AddWallHit(1);
        lastTime = Time.time;
    }
}
