using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PollutionDampingOnDisk : MonoBehaviour
{
    [Header("Runtime Switch (Risk가 제어)")]
    public bool riskEnabled = false;
    [Min(0f)] public float dampingPerSec = 0.2f;
    [Tooltip("오염 트리거의 태그. 비우면 제한없음")]
    public string pollutionTag = "Pollution";
    public bool affectAngular = false;

    Rigidbody rb;
    int insideCount = 0;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!string.IsNullOrEmpty(pollutionTag) && !other.CompareTag(pollutionTag)) return;
        insideCount++;
    }
    void OnTriggerExit(Collider other)
    {
        if (!string.IsNullOrEmpty(pollutionTag) && !other.CompareTag(pollutionTag)) return;
        insideCount = Mathf.Max(0, insideCount - 1);
    }

    void FixedUpdate()
    {
        if (!riskEnabled || insideCount <= 0 || dampingPerSec <= 0f) return;
        float k = Mathf.Exp(-dampingPerSec * Time.fixedDeltaTime);
        rb.linearVelocity *= k;
        if (affectAngular) rb.angularVelocity *= k;
    }
}
