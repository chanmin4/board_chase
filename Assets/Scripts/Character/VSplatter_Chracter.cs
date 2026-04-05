using System;
using UnityEngine;

/// <summary>
/// <para>This component consumes input on the InputReader and stores its values. The input is then read, and manipulated, by the StateMachines's Actions.</para>
/// </summary>
public class VSplatter_Character: MonoBehaviour
{
	[SerializeField] private InputReader _inputReader = default;
	private Vector2 _inputVector;
	private float _previousSpeed;

	//These fields are read and manipulated by the StateMachine actions
	[NonSerialized] public bool DashInput;
	[NonSerialized] public bool ShockwaveInput;
	[NonSerialized] public bool extraActionInput;
	[NonSerialized] public bool attackInput;
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
		_inputReader.MoveEvent += OnMove;
		_inputReader.StartedRunning += OnStartedRunning;
		_inputReader.StoppedRunning += OnStoppedRunning;
		_inputReader.AttackEvent += OnStartedAttack;
		//...
	}

	//Removes all listeners to the events coming from the InputReader script
	private void OnDisable()
	{
		_inputReader.DashEvent -= OnDashInitiated;
		_inputReader.DashCanceledEvent -= OnDashCanceled;
		_inputReader.MoveEvent -= OnMove;
		_inputReader.StartedRunning -= OnStartedRunning;
		_inputReader.StoppedRunning -= OnStoppedRunning;
		_inputReader.AttackEvent -= OnStartedAttack;
		//...
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
	private void OnStoppedRunning() => isRunning = false;

	private void OnStartedRunning() => isRunning = true;


	private void OnStartedAttack() => attackInput = true;

	// Triggered from Animation Event
	public void ConsumeAttackInput() => attackInput = false;
}
