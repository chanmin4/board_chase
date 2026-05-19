using UnityEngine;
using UnityEngine.Events;
using System.Collections;
public class Damageable : MonoBehaviour
{
	[Header("Health")]
	[SerializeField] private HealthConfigSO _healthConfigSO;
	[Tooltip("this feature is allocated automatically if healthConfigSO is provided")] 
	[SerializeField] private HealthSO _currentHealthSO;
	[Header("Player Health Floor")]
	[Tooltip("If true, this Damageable never drops below Minimum Alive Health by normal damage.")]
	[SerializeField] private bool _keepAliveAtMinimumHealth = false;

	[SerializeField, Min(0f)] private float _minimumAliveHealth = 1f;
	[Header("Death")]
	[SerializeField] private bool _destroyOnDeath = false;
	[SerializeField] private float _destroyDelay = 1f;
	[Header("Damage Multiplier")]
	[SerializeField, Min(0f)] private float _defaultDamageTakenMultiplier = 1f;
	[SerializeField, Min(0f)] private float _damageTakenMultiplier = 1f;
	[SerializeField, Min(0f)] private float _minDamageTakenMultiplier = 0f;
	[SerializeField, Min(0f)] private float _maxDamageTakenMultiplier = 99f;
	[Header("Runtime Debug")]
	[ReadOnly] [SerializeField] private float _debugCurrentHealth;
	[ReadOnly] [SerializeField] private float _debugMaxHealth;
	[ReadOnly] [SerializeField] private float _debugHealthNormalized;
	[ReadOnly] [SerializeField] private float _debugDamageTakenMultiplier = 1f;

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

	public float DamageTakenMultiplier => _damageTakenMultiplier;
	private Coroutine _damageMultiplierRoutine;
	public event UnityAction<Damageable> OnDamageMultiplierChanged;
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
			float initialHealth = ResolveInitialHealth();
			_currentHealthSO.SetMaxHealth(initialHealth);
			_currentHealthSO.SetCurrentHealth(initialHealth);
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
		if (!CanReceiveDamage || _currentHealthSO == null)
			return;

		float finalDamage = Mathf.Max(0f, damage * _damageTakenMultiplier);
		if (finalDamage <= 0f)
			return;

		float minAliveHealth = Mathf.Min(
			Mathf.Max(0f, _minimumAliveHealth),
			MaxHealth);

		bool useHealthFloor = _keepAliveAtMinimumHealth && MaxHealth > 0f;
		float nextHealth = CurrentHealth - finalDamage;

		if (useHealthFloor && nextHealth <= minAliveHealth)
			_currentHealthSO.SetCurrentHealth(minAliveHealth);
		else
			_currentHealthSO.InflictDamage(finalDamage);

		SyncRuntimeHealthDebug();

		GetHit = true;

		bool diedThisHit = !useHealthFloor && _currentHealthSO.CurrentHealth <= 0f;

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

		OnDie?.Invoke();

		if (_deathEvent != null)
			_deathEvent.RaiseEvent();

		if (_destroyOnDeath)
			Destroy(gameObject, Mathf.Max(0f, _destroyDelay));
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
		_debugDamageTakenMultiplier = _damageTakenMultiplier;
	}

	private float ResolveInitialHealth()
	{
		float initialHealth = _healthConfigSO != null ? _healthConfigSO.InitialHealth : 1f;

		if (TryGetComponent(out Enemy _))
			initialHealth = DifficultyRuntime.ApplyEnemyHealth(initialHealth);

		return Mathf.Max(1f, initialHealth);
	}

	public void ApplyMaxHealthFromStats(float maxHealth, bool healToFull)
	{
		if (_currentHealthSO == null)
			return;

		maxHealth = Mathf.Max(1f, maxHealth);

		float previousNormalized = HealthNormalized;

		_currentHealthSO.SetMaxHealth(maxHealth);

		float nextHealth = healToFull
			? maxHealth
			: Mathf.Clamp(previousNormalized * maxHealth, 0f, maxHealth);

		_currentHealthSO.SetCurrentHealth(nextHealth);

		SyncRuntimeHealthDebug();
		OnHealthChanged?.Invoke(this);

		if (_updateHealthUI != null)
			_updateHealthUI.RaiseEvent();
	}
	public void SetDamageTakenMultiplier(float multiplier)
	{
		StopDamageMultiplierTimer();

		_damageTakenMultiplier = Mathf.Max(0f, multiplier);
		NotifyDamageMultiplierChanged();
	}
	public void SetDamageTakenMultiplierForSeconds(float multiplier, float duration)
	{
		StopDamageMultiplierTimer();

		_damageTakenMultiplier = Mathf.Max(0f, multiplier);
		NotifyDamageMultiplierChanged();

		if (duration > 0f)
			_damageMultiplierRoutine = StartCoroutine(ResetDamageMultiplierAfter(duration));
	}

	public void ResetDamageTakenMultiplier()
	{
		StopDamageMultiplierTimer();

		_damageTakenMultiplier = Mathf.Max(0f, _defaultDamageTakenMultiplier);
		NotifyDamageMultiplierChanged();
	}
	private IEnumerator ResetDamageMultiplierAfter(float seconds)
	{
		yield return new WaitForSeconds(seconds);

		_damageMultiplierRoutine = null;
		_damageTakenMultiplier = Mathf.Max(0f, _defaultDamageTakenMultiplier);
		NotifyDamageMultiplierChanged();
	}
	private void StopDamageMultiplierTimer()
	{
		if (_damageMultiplierRoutine == null)
			return;

		StopCoroutine(_damageMultiplierRoutine);
		_damageMultiplierRoutine = null;
	}

	private void NotifyDamageMultiplierChanged()
	{
		OnDamageMultiplierChanged?.Invoke(this);
		OnHealthChanged?.Invoke(this);

		if (_updateHealthUI != null)
			_updateHealthUI.RaiseEvent();
	}


}
