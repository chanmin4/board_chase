using UnityEngine;

[DisallowMultipleComponent]
public class Risk_DragCooldown : MonoBehaviour
{
    [Header("Target & Param")]
    public DiskLauncher disklauncher;   // ← 강타입
    public float addSeconds = 1f;       // RiskDef.float_parameter1 합산값이 들어옴

    [Header("Apply Timing")]
    public bool applyOnStart = true;    // 인스톨러에서 false로 내려 수동 Apply()도 가능

    float _original;    // 원본 쿨타임
    bool  _hasOriginal; // 원본 저장 여부

    void Reset()
    {
        if (!disklauncher) disklauncher = FindAnyObjectByType<DiskLauncher>();
    }
    void Awake()
    {
        if (!disklauncher) disklauncher = FindAnyObjectByType<DiskLauncher>();
    }

    void Start()    { if (applyOnStart) Apply(); }
    void OnEnable() { if (Application.isPlaying && applyOnStart) Apply(); }
    void OnDisable(){ if (Application.isPlaying) Revert(); }

    public void Apply()
    {
        if (!disklauncher) return;
        if (!_hasOriginal) { _original = disklauncher.cooldownSeconds; _hasOriginal = true; }

        // 원본 + 추가초
        disklauncher.cooldownSeconds = Mathf.Max(0f, _original + addSeconds);

        // (선택) UI 즉시 갱신이 꼭 필요하다면 DiskLauncher에 public 메서드로
        // NotifyCooldown() 하나 열어두고 여기서 호출하면 됩니다.
        // 지금 상태로도 다음 발사(StartCooldown)부터 새 값이 적용돼요.
    }

    public void Revert()
    {
        if (!disklauncher || !_hasOriginal) return;
        disklauncher.cooldownSeconds = _original;
    }
}
