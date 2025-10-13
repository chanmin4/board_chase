/*
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PollutionDampingOnDisk : MonoBehaviour
{
    [Header("Runtime Switch (Risk가 제어)")]
    public bool  riskEnabled   = false;
    [Min(0f)] public float dampingPerSec = 0.2f;   // 초당 감쇠율(지수)
    public bool  affectAngular = false;

    [Header("Refs (비워두면 자동 검색)")]
    public PaintOwner mask;                // ★ 오염 마스크

    Rigidbody rb;

    // 디버그 확인용(읽기 전용)
    [SerializeField] bool dbgInside = false;       // 현재 오염 구역 안인가?

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!mask) mask = FindAnyObjectByType<PaintMaskRenderer>();
    }

    void FixedUpdate()
    {
        if (!riskEnabled || dampingPerSec <= 0f || rb == null || mask == null) return;

        // ★ 트리거/태그 대신 마스크 픽셀을 직접 확인
        dbgInside = mask.IsContaminatedWorld(rb.position);
        if (!dbgInside) return;

        float k = Mathf.Exp(-dampingPerSec * Time.fixedDeltaTime);
        rb.linearVelocity *= k;
        if (affectAngular) rb.angularVelocity *= k;
    }
}
*/