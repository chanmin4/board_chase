using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class PlayerMove : MonoBehaviour
{
    [Header("Refs")]
    public PlayerDisk disk;
    public Camera moveCamera;
    public Rigidbody rb;

    [Header("Movement")]
    public float moveSpeed = 10f;
    public float accel = 35f;
    public float decel = 45f;
    public bool cameraRelative = true;
    public bool lockYToGround = true;

    float _speedMul = 1f;

    UnityAction<float> _onSpeedMul;

    void Awake()
    {
        if (!disk) disk = GetComponent<PlayerDisk>();
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!moveCamera) moveCamera = Camera.main;

        // Rigidbody 권장 세팅(원하면 인스펙터에서 그대로 둬도 됨)
        rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // SurvivalGauge가 DiskLauncher.externalSpeedMul로 스턴 슬로우를 뿌리니까, 여기서 받아서 이동속도에 적용
        if (disk != null)
        {
            _onSpeedMul = (v) => _speedMul = Mathf.Clamp(v, 0.1f, 1f);
        }
    }

    void FixedUpdate()
    {
        if (!rb) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(h, 0f, v);
        if (input.sqrMagnitude > 1f) input.Normalize();

        Vector3 wishDir = input;

        if (cameraRelative && moveCamera)
        {
            Vector3 f = moveCamera.transform.forward;
            Vector3 r = moveCamera.transform.right;
            f.y = 0f; r.y = 0f;
            f.Normalize(); r.Normalize();
            wishDir = (r * input.x + f * input.z);
            if (wishDir.sqrMagnitude > 1e-6f) wishDir.Normalize();
        }

        float spd = moveSpeed * _speedMul;
        Vector3 targetVel = wishDir * spd;

        Vector3 cur = rb.linearVelocity;
        Vector3 curXZ = new Vector3(cur.x, 0f, cur.z);

        float rate = (input.sqrMagnitude > 1e-6f) ? accel : decel;
        Vector3 newXZ = Vector3.MoveTowards(curXZ, targetVel, rate * Time.fixedDeltaTime);

        float y = cur.y;
        if (lockYToGround && disk)
        {
            // 바닥 고정(필요하면 RigidbodyConstraints.FreezePositionY로 더 강하게 잠가도 됨)
            y = 0f;
            Vector3 p = rb.position;
            p.y = disk.GroundY;
            rb.position = p;
        }

        rb.linearVelocity = new Vector3(newXZ.x, y, newXZ.z);
    }
}
