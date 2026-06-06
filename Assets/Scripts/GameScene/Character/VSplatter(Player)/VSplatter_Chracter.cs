using System;
using UnityEngine;

public class VSplatter_Character : MonoBehaviour
{
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private Transform _feet;

    public Transform Feet => _feet != null ? _feet : transform;

    private Vector2 _inputVector;
    private float _previousSpeed;

    [NonSerialized] public bool DashInput;
    [NonSerialized] public bool ShockwaveInput;
    [NonSerialized] public bool extraActionInput;

    // 좌클릭 Shoot 입력 상태.
    // Attack, AttackAndPaint, Paint 탄환 모두 이 입력을 사용한다.
    [NonSerialized] public bool shootInput;

    [NonSerialized] public Vector3 movementInput;
    [NonSerialized] public Vector3 movementVector;
    [NonSerialized] public ControllerColliderHit lastHit;
    [NonSerialized] public bool isRunning;

    public const float GRAVITY_MULTIPLIER = 5f;
    public const float MAX_FALL_SPEED = -50f;
    public const float MAX_RISE_SPEED = 100f;
    public const float GRAVITY_COMEBACK_MULTIPLIER = 0.03f;
    public const float GRAVITY_DIVIDER = 0.6f;
    public const float AIR_RESISTANCE = 5f;

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        lastHit = hit;
    }

    private void OnEnable()
    {
        if (_inputReader == null)
            return;

        _inputReader.DashEvent += OnDashInitiated;
        _inputReader.DashCanceledEvent += OnDashCanceled;

        _inputReader.ShockwaveChargeEvent += OnShockwaveInitiated;
        _inputReader.ShockwaveExpelEvent += OnShockwaveExpelled;
        _inputReader.ShockwaveCanceledEvent += OnShockwaveCanceled;

        _inputReader.MoveEvent += OnMove;

        _inputReader.ShootEvent += OnShootStarted;
        _inputReader.ShootCanceledEvent += OnShootCanceled;
    }

    private void OnDisable()
    {
        if (_inputReader == null)
            return;

        _inputReader.DashEvent -= OnDashInitiated;
        _inputReader.DashCanceledEvent -= OnDashCanceled;

        _inputReader.ShockwaveChargeEvent -= OnShockwaveInitiated;
        _inputReader.ShockwaveExpelEvent -= OnShockwaveExpelled;
        _inputReader.ShockwaveCanceledEvent -= OnShockwaveCanceled;

        _inputReader.MoveEvent -= OnMove;

        _inputReader.ShootEvent -= OnShootStarted;
        _inputReader.ShootCanceledEvent -= OnShootCanceled;

        shootInput = false;
        DashInput = false;
        ShockwaveInput = false;
        _inputVector = Vector2.zero;
    }

    private void Update()
    {
        RecalculateMovement();
    }

    private void RecalculateMovement()
    {
        float targetSpeed = Mathf.Clamp01(_inputVector.magnitude);

        if (targetSpeed > 0f)
        {
            if (isRunning || shootInput)
                targetSpeed = 1f;
        }

        targetSpeed = Mathf.Lerp(
            _previousSpeed,
            targetSpeed,
            Time.deltaTime * 4f);

        Vector3 adjustedMovement = new Vector3(
            _inputVector.x,
            0f,
            _inputVector.y);

        if (adjustedMovement.sqrMagnitude > 1f)
            adjustedMovement.Normalize();

        movementInput = adjustedMovement * targetSpeed;
        _previousSpeed = targetSpeed;
    }

    private void OnMove(Vector2 movement)
    {
        _inputVector = movement;
    }

    private void OnDashInitiated()
    {
        DashInput = true;
    }

    private void OnDashCanceled()
    {
        DashInput = false;
    }

    private void OnShockwaveInitiated()
    {
        ShockwaveInput = true;
    }

    private void OnShockwaveExpelled()
    {
        ShockwaveInput = false;
    }

    private void OnShockwaveCanceled()
    {
        ShockwaveInput = false;
    }

    private void OnShootStarted()
    {
        shootInput = true;
    }

    private void OnShootCanceled()
    {
        shootInput = false;
    }
}