/*
 * Role hierarchy: Core > Input
 * Primary responsibility: Collects InputReader input and forwards it as gameplay action requests.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
[CreateAssetMenu(menuName = "Input/Input Reader", fileName = "InputReader")]
public class InputReader : ScriptableObject, GameInput.IGameplayActions, GameInput.IUIActions
{
	private const string BindingOverridesPrefsKey = "GameInput.BindingOverrides";
	private const string AuxiliaryGameplayMapName = "GameplayAuxiliary";

    [Space]
	[SerializeField] private GameStateSO _gameStateManager;
	private bool _allowPlayerMenuWhileGameplayDisabled;


    private GameInput _gameInput;
	
	
	public event UnityAction DashEvent = delegate { };
	public event UnityAction DashCanceledEvent = delegate { };
	public event UnityAction ShootEvent = delegate { };
	public event UnityAction ShootCanceledEvent = delegate { };
	public event UnityAction RightClickEvent = delegate { };
	public event UnityAction MeleeAttackEvent = delegate { };
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
	public event UnityAction CycleAmmoEvent = delegate { };
	public event UnityAction DropBackpackEvent = delegate { };

	private InputAction _cycleAmmoAction;
	private InputAction _dropBackpackAction;
	private InputActionMap _auxiliaryGameplayActions;

	/// <summary>
	/// The live input asset used by gameplay. Settings must read and write this
	/// instance rather than the authoring .inputactions asset so a rebind takes
	/// effect immediately in the current run.
	/// </summary>
	public InputActionAsset RuntimeInputActions
	{
		get
		{
			EnsureInputActionsCreated();
			return _gameInput != null ? _gameInput.asset : null;
		}
	}
	public string CycleAmmoBindingDisplayString
	{
		get
		{
			if (_cycleAmmoAction != null && _cycleAmmoAction.bindings.Count > 0)
			{
				string display = _cycleAmmoAction.GetBindingDisplayString(
					0,
					InputBinding.DisplayStringOptions.DontIncludeInteractions);

				if (!string.IsNullOrWhiteSpace(display))
					return display;
			}

			return "T";
		}
	}
	public string DropBackpackBindingDisplayString => GetBindingDisplayString(_dropBackpackAction, "X");
	
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

	// Purpose: Activates runtime subscriptions and refreshes the enabled state.
	private void OnEnable()
	{
		EnsureInputActionsCreated();
	}

	/// <summary>
	/// Creates the generated input wrapper once, applies persisted overrides,
	/// and connects this reader to its gameplay and UI action maps.
	/// </summary>
	private void EnsureInputActionsCreated()
	{
		if (_gameInput != null)
			return;

		_gameInput = new GameInput();
		EnsureAuxiliaryGameplayActionsCreated();
		LoadBindingOverrides();
		_gameInput.UI.SetCallbacks(this);
		_gameInput.Gameplay.SetCallbacks(this);
	}

	/// <summary>
	/// Registers gameplay controls introduced after the generated GameInput
	/// wrapper. They join the same runtime input asset, therefore the existing
	/// Settings rebind UI can discover and persist them.
	/// </summary>
	private void EnsureAuxiliaryGameplayActionsCreated()
	{
		if (_gameInput == null || _gameInput.asset == null)
			return;

		_auxiliaryGameplayActions = _gameInput.asset.FindActionMap(
			AuxiliaryGameplayMapName,
			throwIfNotFound: false) ??
			_gameInput.asset.AddActionMap(AuxiliaryGameplayMapName);

		_cycleAmmoAction = FindOrAddAuxiliaryButton("CycleAmmo", "<Keyboard>/t");
		_dropBackpackAction = FindOrAddAuxiliaryButton("DropBackpack", "<Keyboard>/x");

		_cycleAmmoAction.performed -= HandleCycleAmmoPerformed;
		_cycleAmmoAction.performed += HandleCycleAmmoPerformed;
		_dropBackpackAction.performed -= HandleDropBackpackPerformed;
		_dropBackpackAction.performed += HandleDropBackpackPerformed;
	}

	// Purpose: Finds or creates one rebindable runtime-only gameplay button.
	private InputAction FindOrAddAuxiliaryButton(string actionName, string defaultBinding)
	{
		InputAction action = _auxiliaryGameplayActions.FindAction(actionName, throwIfNotFound: false);
		return action ?? _auxiliaryGameplayActions.AddAction(
			actionName,
			InputActionType.Button,
			defaultBinding);
	}

	// Purpose: Resolves a binding label with a stable fallback for HUD and settings UI.
	private static string GetBindingDisplayString(InputAction action, string fallback)
	{
		if (action != null && action.bindings.Count > 0)
		{
			string display = action.GetBindingDisplayString(
				0,
				InputBinding.DisplayStringOptions.DontIncludeInteractions);

			if (!string.IsNullOrWhiteSpace(display))
				return display;
		}

		return fallback;
	}

	/// <summary>
	/// Finds an action in the live gameplay input asset for the Settings menu.
	/// This deliberately returns the runtime instance, not an editor asset copy.
	/// </summary>
	public InputAction FindRuntimeAction(string actionMapName, string actionName)
	{
		EnsureInputActionsCreated();

		if (_gameInput == null || string.IsNullOrWhiteSpace(actionName))
			return null;

		string qualifiedName = string.IsNullOrWhiteSpace(actionMapName)
			? actionName
			: $"{actionMapName}/{actionName}";

		return _gameInput.asset.FindAction(qualifiedName, throwIfNotFound: false);
	}

	/// <summary>
	/// Reloads persisted binding overrides into the same input instance used by
	/// gameplay. Calling this repeatedly is safe and replaces old overrides.
	/// </summary>
	public void LoadBindingOverrides()
	{
		if (_gameInput == null || _gameInput.asset == null)
			return;

		_gameInput.asset.RemoveAllBindingOverrides();

		SaveSystem saveSystem = SaveSystem.Active;
		if (saveSystem != null && !saveSystem.HasLoadedSaveData)
			saveSystem.LoadSaveDataFromDisk();

		string json = saveSystem != null
			? saveSystem.GetInputBindingOverrides()
			: PlayerPrefs.GetString(BindingOverridesPrefsKey, string.Empty);

		if (!string.IsNullOrWhiteSpace(json))
			_gameInput.asset.LoadBindingOverridesFromJson(json);
	}

	/// <summary>
	/// Persists the live input overrides after a successful Settings rebind.
	/// </summary>
	public void SaveBindingOverrides()
	{
		EnsureInputActionsCreated();

		if (_gameInput == null || _gameInput.asset == null)
			return;

		string json = _gameInput.asset.SaveBindingOverridesAsJson();
		SaveSystem saveSystem = SaveSystem.Active;
		if (saveSystem != null)
		{
			saveSystem.SetInputBindingOverrides(json);
			return;
		}

		// Isolated test scenes can still function before PersistentManager exists.
		PlayerPrefs.SetString(BindingOverridesPrefsKey, json);
		PlayerPrefs.Save();
	}

	// Purpose: Releases active subscriptions and transient state while disabled.
	private void OnDisable()
	{
		DisableAllInput();
		_auxiliaryGameplayActions?.Disable();
	}
    	// Purpose: Enables dialogue input for this component.
    	public void EnableDialogueInput()
	{
		_gameInput.UI.Enable();
		_gameInput.Gameplay.Disable();
		_auxiliaryGameplayActions?.Disable();
	}

	// Purpose: Enables gameplay input for this component.
	public void EnableGameplayInput()
	{
		_allowPlayerMenuWhileGameplayDisabled = false;

		_gameInput.UI.Disable();
		_gameInput.Gameplay.Enable();
		_auxiliaryGameplayActions?.Enable();
	}

	// Purpose: Enables menu input for this component.
	public void EnableMenuInput()
	{
		_allowPlayerMenuWhileGameplayDisabled = false;

		ReleaseGameplayInputState();
		_gameInput.Gameplay.Disable();
		_auxiliaryGameplayActions?.Disable();
		_gameInput.UI.Enable();
	}

	// Purpose: Disables all input for this component.
	public void DisableAllInput()
	{
		_allowPlayerMenuWhileGameplayDisabled = false;

		ReleaseGameplayInputState();
		_gameInput.Gameplay.Disable();
		_gameInput.UI.Disable();
		_auxiliaryGameplayActions?.Disable();
	}

	// Purpose: Enables player menu input for this component.
	public void EnablePlayerMenuInput()
	{
		ReleaseGameplayInputState();

		_gameInput.Gameplay.Disable();
		_auxiliaryGameplayActions?.Disable();
		_gameInput.UI.Enable();

		_allowPlayerMenuWhileGameplayDisabled = true;
		_gameInput.Gameplay.PlayerMenu.Enable();
		_gameInput.Gameplay.Map.Enable();
	}

	// Purpose: Releases the owned gameplay input state resource.
	private void ReleaseGameplayInputState()
	{
		MoveEvent.Invoke(Vector2.zero);
		ShootCanceledEvent.Invoke();
		DashCanceledEvent.Invoke();
		ReloadInputHeld = false;
	}


    // Purpose: Performs the left mouse down workflow for this component.
    public bool LeftMouseDown() => Mouse.current.leftButton.isPressed;
    // ---------------- Gameplay ----------------
    // Purpose: Responds to the move callback.
    public void OnMove(InputAction.CallbackContext context) {
		MoveEvent.Invoke(context.ReadValue<Vector2>()); 
	}

    // Purpose: Responds to the shoot callback.
    public void OnShoot(InputAction.CallbackContext context)
	{
		switch (context.phase)
		{
			case InputActionPhase.Performed:
				ShootEvent.Invoke();
				break;
			case InputActionPhase.Canceled:
				ShootCanceledEvent.Invoke();
				break;
		}
	}

	// Purpose: Responds to the melee attack callback.
	public void OnMeleeAttack(InputAction.CallbackContext context)
	{
		if (context.phase == InputActionPhase.Performed)
			MeleeAttackEvent.Invoke();
	}

	// Purpose: Responds to the meele attack callback.
	public void OnMeeleAttack(InputAction.CallbackContext context)
	{
		OnMeleeAttack(context);
	}

   	// Purpose: Responds to the interact callback.
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

    // Purpose: Responds to the dash callback.
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

	// Purpose: Responds to the reload callback.
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
	// Purpose: Responds to the slot1 callback.
	public void OnSlot1(InputAction.CallbackContext context)
		{
			switch (context.phase)
			{
				case InputActionPhase.Performed:
					Slot1Event.Invoke();
					break;
			}
		}
	// Purpose: Responds to the slot2 callback.
	public void OnSlot2(InputAction.CallbackContext context)
	{
		switch (context.phase)
		{
			case InputActionPhase.Performed:
				Slot2Event.Invoke();
				break;
		}
	}
			// Purpose: Responds to the slot3 callback.
			public void OnSlot3(InputAction.CallbackContext context)
	{
		switch (context.phase)
		{
			case InputActionPhase.Performed:
				Slot3Event.Invoke();
				break;
		}
	}
	// Purpose: Responds to the slot4 callback.
	public void OnSlot4(InputAction.CallbackContext context)
	{
		switch (context.phase)
		{
			case InputActionPhase.Performed:
				Slot4Event.Invoke();
				break;
		}
	}
	// Purpose: Responds to the slot5 callback.
	public void OnSlot5(InputAction.CallbackContext context)
	{
		switch (context.phase)
		{
			case InputActionPhase.Performed:
				Slot5Event.Invoke();
				break;
		}
	}

	// Purpose: Handles the incoming cycle ammo performed request or event.
	private void HandleCycleAmmoPerformed(InputAction.CallbackContext context)
	{
		if (context.phase == InputActionPhase.Performed)
			CycleAmmoEvent.Invoke();
	}

	// Purpose: Publishes the rebindable equipped-backpack drop request.
	private void HandleDropBackpackPerformed(InputAction.CallbackContext context)
	{
		if (context.phase == InputActionPhase.Performed)
			DropBackpackEvent.Invoke();
	}
	// Purpose: Responds to the map callback.
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
	// Purpose: Responds to the inventory callback.
	public void OnInventory(InputAction.CallbackContext context)
	{
		switch (context.phase)
		{
			case InputActionPhase.Performed:
				PlayerMenuEvent.Invoke();
				break;
		}
	}
	// Purpose: Responds to the player menu callback.
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

	// Purpose: Responds to the navigate callback.
	public void OnNavigate(InputAction.CallbackContext context)
	{
	}

	// Purpose: Responds to the submit callback.
	public void OnSubmit(InputAction.CallbackContext context)
	{
	}

	// Purpose: Responds to the cancel callback.
	public void OnCancel(InputAction.CallbackContext context)
	{
	}

	// Purpose: Responds to the point callback.
	public void OnPoint(InputAction.CallbackContext context)
	{
	}

	// Purpose: Responds to the click callback.
	public void OnClick(InputAction.CallbackContext context)
	{
	}

	// Purpose: Responds to the right click callback.
	public void OnRightClick(InputAction.CallbackContext context)
	{
		if (context.phase == InputActionPhase.Performed)
			RightClickEvent.Invoke();
	}

	// Purpose: Responds to the middle click callback.
	public void OnMiddleClick(InputAction.CallbackContext context)
	{
	}

	// Purpose: Responds to the scroll wheel callback.
	public void OnScrollWheel(InputAction.CallbackContext context)
	{
	}
	
}
