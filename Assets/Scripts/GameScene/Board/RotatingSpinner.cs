using UnityEngine;

/// <summary>
/// 얇은 회전 막대(벽). Rigidbody(kinematic) + BoxCollider.
/// Layer를 "Wall" 로 맞추면 플레이어가 부딪힐 때 기존 벽과 동일하게 처리됨.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
[DisallowMultipleComponent]
public class RotatingSpinner : MonoBehaviour
{
    [Header("Size (world units)")]
    public float length = 2.0f;      // 막대 길이 (보드 크기에 맞춰 튜닝)
    public float thickness = 0.15f;  // 막대 두께
    public float height = 0.25f;     // Y 두께(살짝 떠있게)

    [Header("Motion")]
    public float angularSpeed = 120f; // 초당 회전(deg/s, Y축)
    public bool  crossShape = false;  // true면 십자(막대 2개)
        [Tooltip("체크 시 반시계(혹은 기존의 반대) 방향으로 회전합니다.")]
    public bool  reverseDirection = false;      // ← 인스펙터 토글 (false/true)
    [Tooltip("로컬 축 기준으로 돌리고 싶다면 체크 해제하세요. 기본은 월드 Y 축 기준.")]
    public bool  worldSpace = true;

    [Header("Physics")]
    public PhysicsMaterial bounceMaterial; // bounciness=1, friction=0, combine=Max 권장
    public bool addKinematicRigidbody = true;

    BoxCollider col;
  const string kArmName = "Arm90";
    void Awake()
    {
        col = GetComponent<BoxCollider>();
        col.size = new Vector3(length, height, thickness);
        col.center = Vector3.zero;
        if (bounceMaterial) col.material = bounceMaterial;

        if (addKinematicRigidbody && !TryGetComponent<Rigidbody>(out var rb))
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;          // 움직이는 콜라이더 안정화
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

         SetupCrossArm();
    }
    void SetupCrossArm()
    {
        // crossShape 설정에 따라 Arm90 생성/제거
        var armTf = transform.Find(kArmName);
        if (crossShape)
        {
            if (!armTf)
            {
                var arm = new GameObject(kArmName).AddComponent<BoxCollider>();
                arm.transform.SetParent(transform, false);
                arm.size = new Vector3(thickness, height, length);
                if (bounceMaterial) arm.material = bounceMaterial;
            }
            else
            {
                var armCol = armTf.GetComponent<BoxCollider>();
                if (armCol) armCol.size = new Vector3(thickness, height, length);
                if (bounceMaterial && armCol) armCol.material = bounceMaterial;
            }
        }
        else
        {
            if (armTf) DestroyImmediate(armTf.gameObject);
        }
    }
     void Update()
    {
        float dir = reverseDirection ? -1f : 1f;       // ← 방향 토글
        float delta = dir * Mathf.Abs(angularSpeed) * Time.deltaTime;

        if (worldSpace)
            transform.Rotate(0f, delta, 0f, Space.World);
        else
            transform.Rotate(0f, delta, 0f, Space.Self);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!col) col = GetComponent<BoxCollider>();
        if (col)
        {
            col.size = new Vector3(Mathf.Max(0.01f, length),
                                   Mathf.Max(0.01f, height),
                                   Mathf.Max(0.01f, thickness));
        }
        SetupCrossArm();
    }

    [ContextMenu("Toggle Direction")]
    void ToggleDirectionContext()
    {
        reverseDirection = !reverseDirection;
    }
#endif

    /// <summary>코드에서 방향을 바꾸고 싶을 때 호출</summary>
    public void SetReverse(bool reverse) => reverseDirection = reverse;
    /// <summary>현재 방향을 토글</summary>
    public void ToggleDirection() => reverseDirection = !reverseDirection;
}