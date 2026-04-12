using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
[CreateAssetMenu(menuName = "Input/Input Reader", fileName = "InputReader")]
public class InputReader : ScriptableObject, GameInput.IGameplayActions, GameInput.IUIActions
{
    [Space]
	[SerializeField] private GameStateSO _gameStateManager;

    private GameInput _gameInput;
	public event UnityAction DashEvent = delegate { };
	public event UnityAction DashCanceledEvent = delegate { };
	public event UnityAction ShockwaveChargeEvent = delegate { };
	public event UnityAction ShockwaveExpelEvent = delegate { };
	public event UnityAction ShockwaveCanceledEvent = delegate { };
	public event UnityAction AttackEvent = delegate { };
	public event UnityAction AttackCanceledEvent = delegate { };
	public event UnityAction PaintEvent = delegate { };
	public event UnityAction PaintCanceledEvent = delegate { };
	public event UnityAction ReloadEvent=delegate{};
	public event UnityAction InteractEvent = delegate { }; // Used to talk, pickup objects, interact with tools like the cooking cauldron
	//public event UnityAction InventoryActionButtonEvent = delegate { };
	//public event UnityAction SaveActionButtonEvent = delegate { };
	//public event UnityAction ResetActionButtonEvent = delegate { };
	public event UnityAction<Vector2> MoveEvent = delegate { };

	// Shared between menus and dialogues
	public event UnityAction MoveSelectionEvent = delegate { };
	// Menus
	public event UnityAction MenuMouseMoveEvent = delegate { };
	public event UnityAction MenuClickButtonEvent = delegate { };
	public event UnityAction MenuUnpauseEvent = delegate { };
	public event UnityAction MenuPauseEvent = delegate { };
	public event UnityAction MenuCloseEvent = delegate { };
	public event UnityAction OpenInventoryEvent = delegate { }; // Used to bring up the inventory
	public event UnityAction CloseInventoryEvent = delegate { }; // Used to bring up the inventory
	public event UnityAction<float> TabSwitched = delegate { };


	private void OnEnable()
	{
		if (_gameInput == null)
		{
			_gameInput = new GameInput();

			_gameInput.UI.SetCallbacks(this);
			_gameInput.Gameplay.SetCallbacks(this);
		}

	}

	private void OnDisable()
	{
		DisableAllInput();
	}
    	public void EnableDialogueInput()
	{
		_gameInput.UI.Enable();
		_gameInput.Gameplay.Disable();
	}

	public void EnableGameplayInput()
	{
		_gameInput.UI.Disable();
		_gameInput.Gameplay.Enable();
	}

	public void EnableMenuInput()
	{
		_gameInput.Gameplay.Disable();

		_gameInput.UI.Enable();
	}

	public void DisableAllInput()
	{
		_gameInput.Gameplay.Disable();
		_gameInput.UI.Disable();
	}
    public bool LeftMouseDown() => Mouse.current.leftButton.isPressed;
    // ---------------- Gameplay ----------------
    public void OnMove(InputAction.CallbackContext context) {
		MoveEvent.Invoke(context.ReadValue<Vector2>()); 
	}

    public void OnAttack(InputAction.CallbackContext context)
	{
		switch (context.phase)
		{
			case InputActionPhase.Performed:
				AttackEvent.Invoke();
				break;
			case InputActionPhase.Canceled:
				AttackCanceledEvent.Invoke();
				break;
		}
	}
	public void OnPaint(InputAction.CallbackContext context)
	{
		switch (context.phase)
		{
			case InputActionPhase.Performed:
				PaintEvent.Invoke();
				break;
			case InputActionPhase.Canceled:
				PaintCanceledEvent.Invoke();
				break;
		}
	}

    public void OnInteract(InputAction.CallbackContext context)
	{
		if ((context.phase == InputActionPhase.Performed)
		&& (_gameStateManager.CurrentGameState == GameState.Gameplay)) // Interaction is only possible when in gameplay GameState
			InteractEvent.Invoke();
	}
    public void OnShockwave(InputAction.CallbackContext context)
	{
		switch (context.phase)
		{
			case InputActionPhase.Performed:
				ShockwaveChargeEvent.Invoke();
				break;
			case InputActionPhase.Canceled:
				ShockwaveCanceledEvent.Invoke();
				break;
		}
	}
    public void OnDash(InputAction.CallbackContext context)
	{
				switch (context.phase)
		{
			case InputActionPhase.Performed:
				DashEvent.Invoke();
				break;
			case InputActionPhase.Canceled:
				DashCanceledEvent.Invoke();
				break;
		}
	}

public void OnReload(InputAction.CallbackContext context)
	{
		switch (context.phase)
		{
			case InputActionPhase.Performed:
				ReloadEvent.Invoke();
				break;
		}
	}
    // ---------------- Menu (UI) ----------------
    public void OnNavigate(InputAction.CallbackContext context) { }
    public void OnSubmit(InputAction.CallbackContext context) { } 
    public void OnCancel(InputAction.CallbackContext context) { }
    public void OnPoint(InputAction.CallbackContext context) { }
    public void OnClick(InputAction.CallbackContext context) { }
    public void OnRightClick(InputAction.CallbackContext context) { }
    public void OnMiddleClick(InputAction.CallbackContext context) { }
    public void OnScrollWheel(InputAction.CallbackContext context) { }
    public void OnTrackedDevicePosition(InputAction.CallbackContext context) { }
    public void OnTrackedDeviceOrientation(InputAction.CallbackContext context) { }
}