using UnityEngine;

[DisallowMultipleComponent]
public class Risk_PollutionFrictionEnable : MonoBehaviour
{
    [Header("Installer 호환용(사용되진 않음)")]
    public bool applyOnStart = true;

    [Header("Targets")]
    // (옵션) Rigidbody 감속을 쓸 경우
    public Rigidbody[] rigidbodies;
    // TODO: 커스텀 이동 스크립트를 쓰면 아래 배열에 인스펙터로 할당
    public MonoBehaviour[] moveScripts;

    [Header("Param")]
    public bool enableFriction = true;     // b0 = 감속 활성
    [Min(0f)] public float dampingPerSec = 0.2f; // f0 = 초당 감쇠율(프로젝트 로직에 맞게 사용)

    float[] orig_drag;  // Rigidbody용
    bool capturedRB;    // Rigidbody 원본 캡쳐 여부
    bool capturedMS;    // moveScripts 원본 캡쳐 여부 (필요 시 사용)

    void Awake()
    {
        // TODO: 자동 수집 필요하면 여기에서 Find… 사용
        if (rigidbodies != null && rigidbodies.Length > 0)
        {
            orig_drag = new float[rigidbodies.Length];
            for (int i = 0; i < rigidbodies.Length; i++)
                orig_drag[i] = rigidbodies[i] ? rigidbodies[i].linearDamping : 0f;
            capturedRB = true;
        }

        if (moveScripts != null && moveScripts.Length > 0)
        {
            // TODO: 커스텀 스크립트의 원본 상태를 저장(필요 시)
            // ex) speed/accel 등
            capturedMS = true;
        }
    }

    void Start()    { if (applyOnStart) Apply(); }
    void OnEnable() { if (Application.isPlaying && applyOnStart) Apply(); }
    void OnDisable(){ if (Application.isPlaying) Revert(); }

    public void Apply()
    {
        // (옵션) Rigidbody 기반 감속
        if (capturedRB)
        {
            for (int i = 0; i < rigidbodies.Length; i++)
                if (rigidbodies[i])
                    rigidbodies[i].linearDamping = enableFriction ? Mathf.Max(orig_drag[i], dampingPerSec) : orig_drag[i];
        }

        // TODO: 커스텀 이동 스크립트에 감쇠 적용(프로젝트 로직에 맞게)
        // for (int i = 0; i < moveScripts.Length; i++)
        //    ((YourMoveScript)moveScripts[i]).SetFriction(enableFriction, dampingPerSec);
    }

    public void Revert()
    {
        if (capturedRB)
        {
            for (int i = 0; i < rigidbodies.Length; i++)
                if (rigidbodies[i]) rigidbodies[i].linearDamping = orig_drag[i];
        }

        // TODO: 커스텀 이동 스크립트 원복
        // for (int i = 0; i < moveScripts.Length; i++)
        //    ((YourMoveScript)moveScripts[i]).ResetFriction();
    }
}
