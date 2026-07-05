using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
[CreateAssetMenu(menuName = "Input/Input Reader", fileName = "InputReader")]
public class InputReader : ScriptableObject, GameInput.IGameplayActions, GameInput.IUIActions
{
    [Space]
	[SerializeField] private GameStateSO _gameStateManager;
	private bool _allowPlayerMenuWhileGameplayDisabled;


    private GameInput _gameInput;
	
	
	public event UnityAction DashEvent = delegate { };
	public event UnityAction DashCanceledEvent = delegate { };
	public event UnityAction ShootEvent = delegate { };
	public event UnityAction ShootCanceledEvent = delegate { };
	public event UnityAction ReloadEvent=delegate{};
	public event UnityAction InteractEvent = delegate { }; // Used to talk, pickup objects, interact with tools like the cooking cauldron
	
	//public event UnityAction InventoryActionButtonEvent = delegate { };
	//public event UnityAction SaveActionButtonEvent = delegate { };
	//public event UnityAction ResetActionButtonEvent = delegate { };
	public event UnityAction<Vector2> MoveEvent = delegate { };

	// Shared between menus and dialogues
	public event UnityAction MoveSelectionEvent = delegate { };
	// Menus
	public event UnityAction MapEvent = delegate { };

	public event UnityAction Slot1Event = delegate { };
	public event UnityAction Slot2Event = delegate { };
	public event UnityAction Slot3Event = delegate { };
	public event UnityAction Slot4Event = delegate { };
	public event UnityAction Slot5Event = delegate { };
	
	//public event UnityAction MenuClickButtonEvent = delegate { };
	//public event UnityAction MenuUnpauseEvent = delegate { };
	//public event UnityAction MenuPauseEvent = delegate { };
	
	public event UnityAction PlayerMenuEvent = delegate { };
	public event UnityAction OpenInventoryEvent = delegate { }; // Used to bring up the inventory
	public event UnityAction CloseInventoryEvent = delegate { }; // Used to bring up the inventory
	public event UnityAction<float> TabSwitched = delegate { };
	public bool ReloadInputHeld { get; private set; }
	public event UnityAction MenuMouseMoveEvent = delegate { };
	public event UnityAction MenuCloseEvent = delegate { };

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
		_allowPlayerMenuWhileGameplayDisabled = false;

		_gameInput.UI.Disable();
		_gameInput.Gameplay.Enable();
	}

	public void EnableMenuInput()
	{
		_allowPlayerMenuWhileGameplayDisabled = false;

		ReleaseGameplayInputState();
		_gameInput.Gameplay.Disable();
		_gameInput.UI.Enable();
	}

	public void DisableAllInput()
	{
		_allowPlayerMenuWhileGameplayDisabled = false;

		ReleaseGameplayInputState();
		_gameInput.Gameplay.Disable();
		_gameInput.UI.Disable();
	}

	public void EnablePlayerMenuInput()
	{
		ReleaseGameplayInputState();

		_gameInput.Gameplay.Disable();
		_gameInput.UI.Enable();

		_allowPlayerMenuWhileGameplayDisabled = true;
		_gameInput.Gameplay.PlayerMenu.Enable();
	}

	private void ReleaseGameplayInputState()
	{
		MoveEvent.Invoke(Vector2.zero);
		ShootCanceledEvent.Invoke();
		DashCanceledEvent.Invoke();
		ReloadInputHeld = false;
	}


    public bool LeftMouseDown() => Mouse.current.leftButton.isPressed;
    // ---------------- Gameplay ----------------
    public void OnMove(InputAction.CallbackContext context) {
		MoveEvent.Invoke(context.ReadValue<Vector2>()); 
	}

    public void OnShoot(InputAction.CallbackContext context)
	{
		switch (context.phase)
		{
			case InputActionPhase.Performed:
				if (GameplayAttackInputBlocker.IsBlocked)
				{
					ShootCanceledEvent.Invoke();
					return;
				}

				ShootEvent.Invoke();
				break;
			case InputActionPhase.Canceled:
				ShootCanceledEvent.Invoke();
				break;
		}
	}

   	public void OnInteract(InputAction.CallbackContext context)
	{
		if (context.phase == InputActionPhase.Performed)
		{
			Debug.Log($"[InputReader] Interact performed. GameState={_gameStateManager.CurrentGameState}");
		}

		if ((context.phase == InputActionPhase.Performed)
			&& (_gameStateManager.CurrentGameState == GameState.Gameplay))
		{
			Debug.Log("[InputReader] InteractEvent invoked.");
			InteractEvent.Invoke();
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
				ReloadInputHeld = true;
				ReloadEvent.Invoke();
				break;

			case InputActionPhase.Canceled:
				ReloadInputHeld = false;
				break;
		}
	}
	public void OnSlot1(InputAction.CallbackContext context)
		{
			switch (context.phase)
			{
				case InputActionPhase.Performed:
					Slot1Event.Invoke();
					break;
			}
		}
	public void OnSlot2(InputAction.CallbackContext context)
	{
		switch (context.phase)
		{
			case InputActionPhase.Performed:
				Slot2Event.Invoke();
				break;
		}
	}
			public void OnSlot3(InputAction.CallbackContext context)
	{
		switch (context.phase)
		{
			case InputActionPhase.Performed:
				Slot3Event.Invoke();
				break;
		}
	}
	public void OnSlot4(InputAction.CallbackContext context)
	{
		switch (context.phase)
		{
			case InputActionPhase.Performed:
				Slot4Event.Invoke();
				break;
		}
	}
	public void OnSlot5(InputAction.CallbackContext context)
	{
		switch (context.phase)
		{
			case InputActionPhase.Performed:
				Slot5Event.Invoke();
				break;
		}
	}
	public void OnMap(InputAction.CallbackContext context)
	{
		switch (context.phase)
		{
			case InputActionPhase.Performed:
				MapEvent.Invoke();
				break;
		}
	}

    // ---------------- Menu (UI) ---------------
	public void OnInventory(InputAction.CallbackContext context)
	{
		switch (context.phase)
		{
			case InputActionPhase.Performed:
				MapEvent.Invoke();
				break;
		}
	}
	public void OnPlayerMenu(InputAction.CallbackContext context)
	{
		if (context.phase != InputActionPhase.Performed)
			return;

		bool canOpenOrClose =
			_gameStateManager.CurrentGameState == GameState.Gameplay ||
			_allowPlayerMenuWhileGameplayDisabled;

		if (!canOpenOrClose)
			return;

		PlayerMenuEvent.Invoke();
	}
	
}
