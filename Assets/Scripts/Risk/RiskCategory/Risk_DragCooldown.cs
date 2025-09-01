using UnityEngine;
using System.Reflection;

/// <summary>
/// 선택된 위기(리스크) 때문에 '드래그 쿨다운 +f0초'를
/// 원본 DiskLauncher 코드를 수정하지 않고 주입/해제하는 패치.
/// - OnEnable/Start 시 적용, OnDisable 시 깔끔히 원복
/// - 우선순위: SetRiskExtraCooldown()/GetRiskExtraCooldown() API가 있으면 그걸 사용
///             없으면 baseCooldownSec 필드를 찾아 '직접 가산' (리플렉션)
/// </summary>
[DisallowMultipleComponent]
public class Risk_DragCooldown : MonoBehaviour
{
    [Header("Target & Param")]
    [Tooltip("쿨다운을 덮어씌울 대상 런처. 비우면 같은 오브젝트에서 찾음")]
    public MonoBehaviour disklauncher;   // DiskLauncher 타입 인스펙터 할당 권장
    [Tooltip("추가 쿨다운(초). 예: 1.0 → +1초")]
    public float addSeconds = 1.0f;

    [Header("Apply Timing")]
    public bool applyOnStart = true;       // 런 시작 시 자동 적용

    // --- 내부 상태 ---
    object _launcher;                // 캐시된 대상
    float? _originalBaseCooldown;    // baseCooldownSec 원본값(필드 모드일 때만 사용)
    float  _appliedExtra;            // API 모드에서 이번 패치로 더한 값

    MethodInfo _miRecalc;            // RecalcCooldownNow()
    MethodInfo _miSetExtra;          // SetRiskExtraCooldown(float)
    MethodInfo _miGetExtra;          // GetRiskExtraCooldown()
    FieldInfo  _fiBaseCooldown;      // baseCooldownSec

    void Awake()
    {
        // 대상 탐색
        _launcher = disklauncher ? (object)disklauncher : (object)GetComponent("DiskLauncher");
        if (_launcher == null)
        {
            Debug.LogWarning("[RiskPatch_DragCooldown] DiskLauncher를 찾지 못했습니다.");
            enabled = false;
            return;
        }

        var t = _launcher.GetType();

        // 가능한 API/필드 캐시
        _miSetExtra = t.GetMethod("SetRiskExtraCooldown", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        _miGetExtra = t.GetMethod("GetRiskExtraCooldown", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        _miRecalc   = t.GetMethod("RecalcCooldownNow",     BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        _fiBaseCooldown = t.GetField("baseCooldownSec",    BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
    }

    void Start()
    {
        if (applyOnStart) Apply();
    }

    void OnEnable()
    {
        if (Application.isPlaying && applyOnStart) Apply();
    }

    void OnDisable()
    {
        if (Application.isPlaying) Revert();
    }

    /// <summary>리스크 ON: 추가 쿨다운 적용</summary>
    public void Apply()
    {
        if (_launcher == null) return;

        // 1) 전용 API가 있으면 그걸 사용(권장)
        if (_miSetExtra != null && _miGetExtra != null)
        {
            float cur = (float)_miGetExtra.Invoke(_launcher, null);
            _appliedExtra = addSeconds;
            float next = Mathf.Max(0f, cur + _appliedExtra);
            _miSetExtra.Invoke(_launcher, new object[]{ next });
            return;
        }

        // 2) 없으면 baseCooldownSec 필드에 직접 가산
        if (_fiBaseCooldown != null)
        {
            if (!_originalBaseCooldown.HasValue)
                _originalBaseCooldown = (float)_fiBaseCooldown.GetValue(_launcher);

            float newBase = Mathf.Max(0f, _originalBaseCooldown.Value + addSeconds);
            _fiBaseCooldown.SetValue(_launcher, newBase);
        }

        // 3) 실효 쿨다운 재계산 함수가 있으면 호출
        if (_miRecalc != null) _miRecalc.Invoke(_launcher, null);
    }

    /// <summary>리스크 OFF: 원래대로 복구</summary>
    public void Revert()
    {
        if (_launcher == null) return;

        // 1) API 모드면 우리가 더했던 만큼만 되돌림
        if (_miSetExtra != null && _miGetExtra != null)
        {
            float cur = (float)_miGetExtra.Invoke(_launcher, null);
            float next = Mathf.Max(0f, cur - _appliedExtra);
            _miSetExtra.Invoke(_launcher, new object[]{ next });
            _appliedExtra = 0f;
            return;
        }

        // 2) 필드 모드면 원본 baseCooldownSec 복원
        if (_originalBaseCooldown.HasValue && _fiBaseCooldown != null)
            _fiBaseCooldown.SetValue(_launcher, _originalBaseCooldown.Value);

        if (_miRecalc != null) _miRecalc.Invoke(_launcher, null);
    }
}
