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

    [Header("Physics")]
    public PhysicsMaterial bounceMaterial; // bounciness=1, friction=0, combine=Max 권장
    public bool addKinematicRigidbody = true;

    BoxCollider col;

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

        // 선택적으로 십자 형태 보강
        if (crossShape)
        {
            var arm = new GameObject("Arm90").AddComponent<BoxCollider>();
            arm.transform.SetParent(transform, false);
            arm.size = new Vector3(thickness, height, length);
            if (bounceMaterial) arm.material = bounceMaterial;
        }
    }

    void Update()
    {
        transform.Rotate(0f, angularSpeed * Time.deltaTime, 0f, Space.World);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!col) col = GetComponent<BoxCollider>();
        if (col) col.size = new Vector3(Mathf.Max(0.01f, length), Mathf.Max(0.01f, height), Mathf.Max(0.01f, thickness));
    }
#endif
}
