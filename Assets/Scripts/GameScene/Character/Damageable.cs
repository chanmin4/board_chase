using UnityEngine;
using UnityEngine.Events;

public class Damageable : MonoBehaviour
{
	[Header("Health")]
	[SerializeField] private HealthConfigSO _healthConfigSO;
	[Tooltip("this feature is allocated automatically if healthConfigSO is provided")] 
	[SerializeField] private HealthSO _currentHealthSO;
	[Header("Death")]
	[SerializeField] private bool _destroyOnDeath = true;
	[SerializeField] private float _destroyDelay = 1f;

	[Header("Runtime Debug")]
	[ReadOnly] [SerializeField] private float _debugCurrentHealth;
	[ReadOnly] [SerializeField] private float _debugMaxHealth;
	[ReadOnly] [SerializeField] private float _debugHealthNormalized;


	[Header("Combat")]
	[SerializeField] private GetHitEffectConfigSO _getHitEffectSO;
	[SerializeField] private Renderer _mainMeshRenderer;
	[SerializeField] private DroppableRewardConfigSO _droppableRewardSO;
	[Header("Invulnerability")]
	[SerializeField] private InvulnerabilityController _invulnerabilityController;
	[SerializeField] private InvulnerabilityConfigSO _postHitInvulnerabilityConfig;

	[Header("Broadcasting On")]
	[SerializeField] private VoidEventChannelSO _updateHealthUI = default;
	[SerializeField] private VoidEventChannelSO _deathEvent = default;

	[Header("Listening To")]
	[SerializeField] private FloatEventChannelSO _restoreHealth = default; //Getting cured when eating food

	public DroppableRewardConfigSO DroppableRewardConfig => _droppableRewardSO;

	//Flags that the StateMachine uses for Conditions to move between states
	public bool GetHit { get; set; }
	public bool IsDead { get; set; }
	public float MaxHealth => _currentHealthSO != null ? _currentHealthSO.MaxHealth : 0f;
	public float CurrentHealth => _currentHealthSO != null ? _currentHealthSO.CurrentHealth : 0f;
	public float HealthNormalized => MaxHealth > 0f ? Mathf.Clamp01(CurrentHealth / MaxHealth) : 0f;


 	public event UnityAction<Damageable> OnHealthChanged;

	public GetHitEffectConfigSO GetHitEffectConfig => _getHitEffectSO;
	public Renderer MainMeshRenderer => _mainMeshRenderer; //used to apply the hit flash effect
	public bool IsInvulnerable =>
		_invulnerabilityController != null && _invulnerabilityController.IsInvulnerable;

	public bool CanReceiveDamage => !IsDead && !IsInvulnerable;
	public event UnityAction OnDie;

	private void Awake()
	{
		//If the HealthSO hasn't been provided in the Inspector (as it's the case for the player),
		//we create a new SO unique to this instance of the component. This is typical for enemies.
		if (_currentHealthSO == null)
		{
			_currentHealthSO = ScriptableObject.CreateInstance<HealthSO>();
			_currentHealthSO.SetMaxHealth(_healthConfigSO.InitialHealth);
			_currentHealthSO.SetCurrentHealth(_healthConfigSO.InitialHealth);
		}
		if (_invulnerabilityController == null)
    		TryGetComponent(out _invulnerabilityController);

		SyncRuntimeHealthDebug();

		if (_updateHealthUI != null)
			_updateHealthUI.RaiseEvent();
	}

	private void OnEnable()
	{
		if(_restoreHealth != null)
			_restoreHealth.OnEventRaised += Cure;
	}

	private void OnDisable()
	{
		if(_restoreHealth != null)
			_restoreHealth.OnEventRaised -= Cure;
	}

	public void ReceiveAnAttack(float damage, GameObject attacker = null)
	{
		if (!CanReceiveDamage)
			return;

		_currentHealthSO.InflictDamage(damage);
		SyncRuntimeHealthDebug();

		GetHit = true;

		bool diedThisHit = _currentHealthSO.CurrentHealth <= 0f;

		if (diedThisHit)
		{
			_currentHealthSO.SetCurrentHealth(0f);
			IsDead = true;
		}
		else if (_invulnerabilityController != null)
		{
			_invulnerabilityController.Begin(_postHitInvulnerabilityConfig);
		}

		if (!diedThisHit && attacker != null && TryGetComponent(out Enemy enemy))
			enemy.NotifyDamagedBy(attacker);

		OnHealthChanged?.Invoke(this);

		if (_updateHealthUI != null)
			_updateHealthUI.RaiseEvent();

		if (!diedThisHit)
			return;

		if (OnDie != null)
			OnDie.Invoke();

		if (_deathEvent != null)
			_deathEvent.RaiseEvent();

		if (_destroyOnDeath)
		{
			Destroy(gameObject, Mathf.Max(0f, _destroyDelay));
		}
}

	public void Kill()
	{
		ReceiveAnAttack(_currentHealthSO.CurrentHealth);
	}

	/// <summary>
	/// Called by the StateMachine action ResetHealthSO. Used to revive the Rock critters.
	/// </summary>
	public void Revive()
	{
		_currentHealthSO.SetCurrentHealth(_healthConfigSO.InitialHealth);
		SyncRuntimeHealthDebug();
		OnHealthChanged?.Invoke(this);
		if (_updateHealthUI != null)
			_updateHealthUI.RaiseEvent();
			
		IsDead = false;
	}

	/// <summary>
	/// Used for cure events, like eating food. Triggered by an IntEventChannelSO.
	/// </summary>
	private void Cure(float healthToAdd)
	{
		if (IsDead)
			return;
			
		_currentHealthSO.RestoreHealth(healthToAdd);
		SyncRuntimeHealthDebug();
		OnHealthChanged?.Invoke(this);
		if (_updateHealthUI != null)
			_updateHealthUI.RaiseEvent();
	}

	private void SyncRuntimeHealthDebug()
	{
		_debugCurrentHealth = CurrentHealth;
		_debugMaxHealth = MaxHealth;
		_debugHealthNormalized = HealthNormalized;
	}
}
