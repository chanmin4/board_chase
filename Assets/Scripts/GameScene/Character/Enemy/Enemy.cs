using UnityEngine;

public class Enemy : MonoBehaviour
{
	[HideInInspector] public bool isPlayerInAlertZone;
	[HideInInspector] public bool isPlayerInAttackZone;
	[SerializeField] private KnockbackReceiver _knockbackReceiver;

	[Header("Runtime Don't touch")]
	[ReadOnly] [SerializeField] private bool _isInfectionCasting;
	[ReadOnly] [SerializeField] private float _infectionCastStartTime;
	[ReadOnly] [SerializeField] private float _infectionCastDuration;
	[ReadOnly] public Damageable currentTarget; // The StateMachine evaluates its health when needed

	[ReadOnly] [SerializeField] private SectorRuntime _currentSector;
	[ReadOnly] [SerializeField] private bool _isSpawnReady = true;
	[ReadOnly] [SerializeField] private bool _searchSucceeded;
	[ReadOnly] [SerializeField] private bool _infectionTargetValid;
	[ReadOnly] [SerializeField] private bool _isKnockbackRequested;

	private bool _hasInfectionTarget;
	private Vector3 _infectionTargetPosition;

	public SectorRuntime CurrentSector => _currentSector;
	public bool IsSpawnReady => _isSpawnReady;
	public bool HasInfectionTarget => _hasInfectionTarget;
	public Vector3 InfectionTargetPosition => _infectionTargetPosition;
	public bool SearchSucceeded => _searchSucceeded;
	public bool InfectionTargetValid => _infectionTargetValid;

	public bool IsKnockbackRequested =>
		_knockbackReceiver != null && _knockbackReceiver.HasPendingRequest;

	public bool IsKnockbackActive =>
		_knockbackReceiver != null && _knockbackReceiver.IsKnockbackActive;

	public bool IsInfectionCasting => _isInfectionCasting;

	public float InfectionCastProgress01
	{
		get
		{
			if (!_isInfectionCasting || _infectionCastDuration <= 0f)
				return 0f;

			return Mathf.Clamp01((Time.time - _infectionCastStartTime) / _infectionCastDuration);
		}
	}

	public virtual void OnAlertTriggerChange(bool entered, GameObject who)
	{
		isPlayerInAlertZone = entered;

		if (who == null)
		{
			if (!entered)
				ClearTarget();

			return;
		}

		Damageable damageable =
			who.GetComponent<Damageable>() ??
			who.GetComponentInParent<Damageable>();

		if (entered)
		{
			if (damageable != null)
				SetTarget(damageable);

			return;
		}

		if (damageable != null && currentTarget == damageable)
			ClearTarget();
	}


	public virtual void OnAttackTriggerChange(bool entered, GameObject who)
	{
		isPlayerInAttackZone = entered;
	}

	public void NotifyDamagedBy(GameObject attacker)
	{
		if (attacker == null)
			return;

		VSplatter_Character playerCharacter =
			attacker.GetComponent<VSplatter_Character>() ??
			attacker.GetComponentInParent<VSplatter_Character>();

		if (playerCharacter == null)
			return;

		Damageable playerDamageable =
			playerCharacter.GetComponent<Damageable>() ??
			playerCharacter.GetComponentInParent<Damageable>();

		if (playerDamageable == null || playerDamageable.IsDead)
			return;
		SetTarget(playerDamageable);
	}

	public void SetCurrentSector(SectorRuntime sector)
	{
		_currentSector = sector;
	}

	public void SetSpawnReady(bool isReady)
	{
		_isSpawnReady = isReady;
	}

	public void SetInfectionTarget(Vector3 position)
	{
		_infectionTargetPosition = position;
		_hasInfectionTarget = true;
		_searchSucceeded = true;
		_infectionTargetValid = true;
	}

	public void ClearInfectionTarget()
	{
		_hasInfectionTarget = false;
	}

	public void SetSearchSucceeded(bool succeeded)
	{
		_searchSucceeded = succeeded;
	}

	public void SetInfectionTargetvalid(bool isvalid)
	{
		_infectionTargetValid = isvalid;
	}

	protected void SetTarget(Damageable target)
	{
		if (currentTarget == target)
			return;

		ClearTarget();

		currentTarget = target;
		if (currentTarget != null)
			currentTarget.OnDie += OnTargetDead;
	}

	protected void ClearTarget()
	{
		if (currentTarget != null)
			currentTarget.OnDie -= OnTargetDead;

		currentTarget = null;
	}

	protected virtual void OnTargetDead()
	{
		ClearTarget();
		isPlayerInAlertZone = false;
		isPlayerInAttackZone = false;
	}

	protected virtual void OnDisable()
	{
		ClearTarget();
		isPlayerInAlertZone = false;
		isPlayerInAttackZone = false;
	}

	public void RequestKnockback(KnockbackRequest request)
	{
		_knockbackReceiver?.RequestKnockback(request);
	}

	public void ConsumeKnockbackRequest()
	{
		_knockbackReceiver?.ConsumePendingRequest();
	}

	public void ClearKnockbackRequest()
	{
		_knockbackReceiver?.ClearPendingRequest();
	}

	public void BeginInfectionCast(float durationSeconds)
	{
		_isInfectionCasting = true;
		_infectionCastStartTime = Time.time;
		_infectionCastDuration = Mathf.Max(0.01f, durationSeconds);
	}

	public void CancelInfectionCast()
	{
		_isInfectionCasting = false;
		_infectionCastStartTime = 0f;
		_infectionCastDuration = 0f;
	}
}
