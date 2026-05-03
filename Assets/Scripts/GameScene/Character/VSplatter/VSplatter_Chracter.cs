using System;
using UnityEngine;

/// <summary>
/// <para>This component consumes input on the InputReader and stores its values. The input is then read, and manipulated, by the StateMachines's Actions.</para>
/// </summary>
public class VSplatter_Character: MonoBehaviour
{
	[SerializeField] private InputReader _inputReader = default;
	[SerializeField] private Transform _feet;
	public Transform Feet => _feet != null ? _feet : transform;
	private Vector2 _inputVector;
	private float _previousSpeed;

	//These fields are read and manipulated by the StateMachine actions
	[NonSerialized] public bool DashInput;
	[NonSerialized] public bool ShockwaveInput;
	[NonSerialized] public bool extraActionInput;
	[NonSerialized] public bool attackInput;
	[NonSerialized] public bool paintInput;
	[NonSerialized] public Vector3 movementInput; //Initial input coming from the Protagonist script
	[NonSerialized] public Vector3 movementVector; //Final movement vector, manipulated by the StateMachine actions
	[NonSerialized] public ControllerColliderHit lastHit;
	[NonSerialized] public bool isRunning; // Used when using the keyboard to run, brings the normalised speed to 1

	public const float GRAVITY_MULTIPLIER = 5f;
	public const float MAX_FALL_SPEED = -50f;
	public const float MAX_RISE_SPEED = 100f;
	public const float GRAVITY_COMEBACK_MULTIPLIER = .03f;
	public const float GRAVITY_DIVIDER = .6f;
	public const float AIR_RESISTANCE = 5f;

	private void OnControllerColliderHit(ControllerColliderHit hit)
	{
		lastHit = hit;
	}

	//Adds listeners for events being triggered in the InputReader script
	private void OnEnable()
	{
		_inputReader.DashEvent += OnDashInitiated;
		_inputReader.DashCanceledEvent += OnDashCanceled;
		_inputReader.ShockwaveChargeEvent+= OnShockWaveInitiated;
		_inputReader.ShockwaveExpelEvent+=OnShockWaveExpel;
		_inputReader.ShockwaveCanceledEvent+=OnShockWaveCanceled;
		_inputReader.MoveEvent += OnMove;
		_inputReader.AttackEvent += OnStartedAttack;
		_inputReader.AttackCanceledEvent+=OnCanceledAttack;
		_inputReader.PaintEvent+=OnPaint;
		_inputReader.PaintCanceledEvent+=OnPaintCanceled;

		//...
	}

	//Removes all listeners to the events coming from the InputReader script
	private void OnDisable()
	{
		_inputReader.DashEvent -= OnDashInitiated;
		_inputReader.DashCanceledEvent -= OnDashCanceled;
		_inputReader.ShockwaveChargeEvent-= OnShockWaveInitiated;
		_inputReader.ShockwaveExpelEvent-=OnShockWaveExpel;
		_inputReader.ShockwaveCanceledEvent-=OnShockWaveCanceled;
		_inputReader.MoveEvent -= OnMove;
		_inputReader.AttackEvent -= OnStartedAttack;
		_inputReader.AttackCanceledEvent-=OnCanceledAttack;
		_inputReader.PaintEvent-=OnPaint;
		_inputReader.PaintCanceledEvent-=OnPaintCanceled;
		//...
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
			if (isRunning)
				targetSpeed = 1f;

			if (attackInput)
				targetSpeed = 1f;
		}

		targetSpeed = Mathf.Lerp(_previousSpeed, targetSpeed, Time.deltaTime * 4f);

		Vector3 adjustedMovement = new Vector3(_inputVector.x, 0f, _inputVector.y);

		if (adjustedMovement.sqrMagnitude > 1f)
			adjustedMovement.Normalize();

		movementInput = adjustedMovement * targetSpeed;

		_previousSpeed = targetSpeed;
		//Debug.Log($"input={_inputVector}, movementInput={movementInput}");
	}

	//---- EVENT LISTENERS ----

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
	private void OnShockWaveInitiated()
	{
		ShockwaveInput = true;
	}
	

	private void OnShockWaveExpel()
	{
		ShockwaveInput = false;
	}
	private void OnShockWaveCanceled()
	{
		ShockwaveInput=false;
	}


	private void OnStartedAttack() => attackInput = true;
	private void OnCanceledAttack()=> attackInput=false;
    private void OnPaint() => paintInput = true;
	private void OnPaintCanceled() => paintInput = false;
    // Triggered from Animation Event
    //public void ConsumeAttackInput() => attackInput = false;

}
