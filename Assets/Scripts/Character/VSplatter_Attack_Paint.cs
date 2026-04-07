/*
//현재 우클릭  페인트 isshooting이랑 충돌중
using UnityEngine;

[DisallowMultipleComponent]
public class VSplatter_Attack_Paint : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private VSplatter_Character _character;
    [SerializeField] private Animator _animator;
    [SerializeField] private SectorPaintManager _sectorPaintManager;
    [SerializeField] private Camera _aimCamera;
    [SerializeField] private Transform _ownerRoot;

    [Header("Attack - Left Click (Scaffold Only)")]
    [SerializeField] private float attackShotsPerSecond = 2f;
    [SerializeField] private bool driveAttackAnimator = true;
    [SerializeField] private string attackHoldBoolName = "IsShooting";
    [SerializeField] private string attackTriggerName = "Shoot";

    [Header("Paint - Right Click")]
    [SerializeField] private float paintShotsPerSecond = 4f;
    [SerializeField] private float paintRadiusWorld = 1.2f;
    [SerializeField] private float maxPaintDistance = 12f;
    [SerializeField] private int paintPriority = 0;
    [SerializeField] private LayerMask aimHitMask = ~0;
    [SerializeField] private SectorPaintManager.PaintChannel paintChannel = SectorPaintManager.PaintChannel.Vaccine;
    [SerializeField] private bool requirePhysicsHit = true;
    [SerializeField] private float fallbackPlaneY = 0f;

    [Header("Optional Animator - Paint")]
    [SerializeField] private bool drivePaintAnimator = false;
    [SerializeField] private string paintHoldBoolName = "IsPainting";
    [SerializeField] private string paintTriggerName = "Paint";

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;
    [SerializeField] private bool debugDrawAimPoint = false;
    [SerializeField] private float debugDrawDuration = 0.1f;

    private float _nextAttackTime;
    private float _nextPaintTime;

    private int _attackHoldBoolHash;
    private int _attackTriggerHash;
    private int _paintHoldBoolHash;
    private int _paintTriggerHash;

    private bool _hasAttackHoldBool;
    private bool _hasAttackTrigger;
    private bool _hasPaintHoldBool;
    private bool _hasPaintTrigger;

    private void Reset()
    {
        if (_character == null)
            _character = GetComponent<VSplatter_Character>();

        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        if (_sectorPaintManager == null)
            _sectorPaintManager = FindAnyObjectByType<SectorPaintManager>();

        if (_aimCamera == null)
            _aimCamera = Camera.main;

        if (_ownerRoot == null)
            _ownerRoot = transform;
    }

    private void Awake()
    {
        if (_character == null)
            _character = GetComponent<VSplatter_Character>();

        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        if (_sectorPaintManager == null)
            _sectorPaintManager = FindAnyObjectByType<SectorPaintManager>();

        if (_aimCamera == null)
            _aimCamera = Camera.main;

        if (_ownerRoot == null)
            _ownerRoot = transform;

        CacheAnimatorParameters();
    }

    private void Update()
    {
        if (_character == null)
            return;

        UpdateAnimatorHoldBools();
        HandleAttackInput();
        HandlePaintInput();
    }

    private void CacheAnimatorParameters()
    {
        _attackHoldBoolHash = Animator.StringToHash(attackHoldBoolName);
        _attackTriggerHash = Animator.StringToHash(attackTriggerName);
        _paintHoldBoolHash = Animator.StringToHash(paintHoldBoolName);
        _paintTriggerHash = Animator.StringToHash(paintTriggerName);

        _hasAttackHoldBool = AnimatorHasParameter(attackHoldBoolName, AnimatorControllerParameterType.Bool);
        _hasAttackTrigger = AnimatorHasParameter(attackTriggerName, AnimatorControllerParameterType.Trigger);
        _hasPaintHoldBool = AnimatorHasParameter(paintHoldBoolName, AnimatorControllerParameterType.Bool);
        _hasPaintTrigger = AnimatorHasParameter(paintTriggerName, AnimatorControllerParameterType.Trigger);
    }

    private bool AnimatorHasParameter(string paramName, AnimatorControllerParameterType type)
    {
        if (_animator == null || string.IsNullOrEmpty(paramName))
            return false;

        AnimatorControllerParameter[] parameters = _animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == paramName && parameters[i].type == type)
                return true;
        }

        return false;
    }

    private void UpdateAnimatorHoldBools()
    {
        if (_animator == null)
            return;

        if (driveAttackAnimator && _hasAttackHoldBool)
            _animator.SetBool(_attackHoldBoolHash, _character.attackInput);

        if (drivePaintAnimator && _hasPaintHoldBool)
            _animator.SetBool(_paintHoldBoolHash, _character.paintInput);
    }

    private void HandleAttackInput()
    {
        if (!_character.attackInput)
            return;

        if (Time.time < _nextAttackTime)
            return;

        FireAttackOnce();
        _nextAttackTime = Time.time + (1f / Mathf.Max(0.01f, attackShotsPerSecond));
    }

    private void FireAttackOnce()
    {
        if (debugLogs)
            Debug.Log("[VSplatter_Attack_Paint] LeftClick Attack scaffold fired.");

        if (_animator != null && driveAttackAnimator && _hasAttackTrigger)
            _animator.SetTrigger(_attackTriggerHash);

        // TODO:
        // 1) 적 레이어/구조물 레이어로 레이캐스트 혹은 투사체 생성
        // 2) 적 피격 시 데미지 처리
        // 3) 구조물/벽 적중 시 소멸 혹은 충돌 이펙트 처리
        // 4) 추후 같은 탄약 풀/반동/사운드 연결
    }

    private void HandlePaintInput()
    {
        if (!_character.paintInput)
            return;

        if (Time.time < _nextPaintTime)
            return;

        FirePaintOnce();
        _nextPaintTime = Time.time + (1f / Mathf.Max(0.01f, paintShotsPerSecond));
    }

    private void FirePaintOnce()
    {
        if (_sectorPaintManager == null)
            return;

        if (!TryGetAimPoint(out Vector3 aimPoint))
        {
            if (debugLogs)
                Debug.Log("[VSplatter_Attack_Paint] Paint canceled: no valid aim point.");
            return;
        }

        if (!IsWithinPaintRange(aimPoint))
        {
            if (debugLogs)
                Debug.Log("[VSplatter_Attack_Paint] Paint canceled: out of range.");
            return;
        }

        bool accepted = _sectorPaintManager.RequestCircle(
            paintChannel,
            aimPoint,
            paintRadiusWorld,
            paintPriority,
            this);

        if (accepted)
        {
            if (_animator != null && drivePaintAnimator && _hasPaintTrigger)
                _animator.SetTrigger(_paintTriggerHash);

            if (debugLogs)
                Debug.Log($"[VSplatter_Attack_Paint] Paint accepted at {aimPoint} r={paintRadiusWorld}");
        }
        else
        {
            if (debugLogs)
                Debug.Log($"[VSplatter_Attack_Paint] Paint rejected at {aimPoint}");
        }

        if (debugDrawAimPoint)
        {
            Debug.DrawLine(aimPoint + Vector3.up * 0.15f, aimPoint + Vector3.up * 1.0f, accepted ? Color.cyan : Color.red, debugDrawDuration);
            Debug.DrawRay(aimPoint, Vector3.right * 0.35f, accepted ? Color.cyan : Color.red, debugDrawDuration);
            Debug.DrawRay(aimPoint, Vector3.forward * 0.35f, accepted ? Color.cyan : Color.red, debugDrawDuration);
        }
    }

    private bool TryGetAimPoint(out Vector3 worldPoint)
    {
        worldPoint = default;

        if (_aimCamera == null)
            _aimCamera = Camera.main;

        if (_aimCamera == null)
            return false;

        Ray ray = _aimCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 999f, aimHitMask, QueryTriggerInteraction.Ignore))
        {
            worldPoint = hit.point;
            return true;
        }

        if (requirePhysicsHit)
            return false;

        Plane plane = new Plane(Vector3.up, new Vector3(0f, fallbackPlaneY, 0f));
        if (!plane.Raycast(ray, out float enter))
            return false;

        worldPoint = ray.GetPoint(enter);
        return true;
    }

    private bool IsWithinPaintRange(Vector3 targetPoint)
    {
        Transform origin = _ownerRoot != null ? _ownerRoot : transform;

        Vector3 from = origin.position;
        from.y = 0f;

        Vector3 to = targetPoint;
        to.y = 0f;

        float sqrDist = (to - from).sqrMagnitude;
        return sqrDist <= (maxPaintDistance * maxPaintDistance);
    }
}
*/
