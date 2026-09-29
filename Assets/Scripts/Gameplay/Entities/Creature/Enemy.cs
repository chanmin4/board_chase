/*
 * Role hierarchy: Gameplay > Entities > Creature
 * Primary responsibility: Implements the focused Enemy responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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
	[ReadOnly] [SerializeField] private bool _isSpawnReady = false;
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
	// Purpose: Responds to the alert trigger change callback.
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


	// Purpose: Responds to the attack trigger change callback.
	public virtual void OnAttackTriggerChange(bool entered, GameObject who)
	{
		isPlayerInAttackZone = entered;
	}

	// Purpose: Publishes the damaged by notification.
	public virtual void NotifyDamagedBy(GameObject attacker)
	{
		if (attacker == null)
			return;

		PlayerCharacter playerCharacter =
			attacker.GetComponent<PlayerCharacter>() ??
			attacker.GetComponentInParent<PlayerCharacter>();

		if (playerCharacter == null)
			return;

		Damageable playerDamageable =
			playerCharacter.GetComponent<Damageable>() ??
			playerCharacter.GetComponentInParent<Damageable>();

		if (playerDamageable == null || playerDamageable.IsDead)
			return;
		SetTarget(playerDamageable);
	}

	// Purpose: Updates the current sector state.
	public void SetCurrentSector(SectorRuntime sector)
	{
		_currentSector = sector;
	}

	// Purpose: Updates the spawn ready state.
	public virtual void SetSpawnReady(bool isReady)
	{
		_isSpawnReady = isReady;
	}

	// Purpose: Updates the infection target state.
	public void SetInfectionTarget(Vector3 position)
	{
		_infectionTargetPosition = position;
		_hasInfectionTarget = true;
		_searchSucceeded = true;
		_infectionTargetValid = true;
	}

	// Purpose: Clears the current infection target state.
	public void ClearInfectionTarget()
	{
		_hasInfectionTarget = false;
	}

	// Purpose: Updates the search succeeded state.
	public void SetSearchSucceeded(bool succeeded)
	{
		_searchSucceeded = succeeded;
	}

	// Purpose: Updates the infection targetvalid state.
	public void SetInfectionTargetvalid(bool isvalid)
	{
		_infectionTargetValid = isvalid;
	}

	// Purpose: Updates the target state.
	protected void SetTarget(Damageable target)
	{
		if (currentTarget == target)
			return;

		ClearTarget();

		currentTarget = target;
		if (currentTarget != null)
			currentTarget.OnDie += OnTargetDead;
	}

	// Purpose: Clears the current target state.
	protected void ClearTarget()
	{
		if (currentTarget != null)
			currentTarget.OnDie -= OnTargetDead;

		currentTarget = null;
	}

	// Purpose: Responds to the target dead callback.
	protected virtual void OnTargetDead()
	{
		ClearTarget();
		isPlayerInAlertZone = false;
		isPlayerInAttackZone = false;
	}

	// Purpose: Releases active subscriptions and transient state while disabled.
	protected virtual void OnDisable()
	{
		ClearTarget();
		isPlayerInAlertZone = false;
		isPlayerInAttackZone = false;
	}

	// Purpose: Requests knockback from the owning service.
	public void RequestKnockback(KnockbackRequest request)
	{
		_knockbackReceiver?.RequestKnockback(request);
	}

	// Purpose: Performs the consume knockback request workflow for this component.
	public void ConsumeKnockbackRequest()
	{
		_knockbackReceiver?.ConsumePendingRequest();
	}

	// Purpose: Clears the current knockback request state.
	public void ClearKnockbackRequest()
	{
		_knockbackReceiver?.ClearPendingRequest();
	}

	// Purpose: Begins the infection cast workflow.
	public void BeginInfectionCast(float durationSeconds)
	{
		_isInfectionCasting = true;
		_infectionCastStartTime = Time.time;
		_infectionCastDuration = Mathf.Max(0.01f, durationSeconds);
	}

	// Purpose: Checks whether cancel infection cast is currently permitted.
	public void CancelInfectionCast()
	{
		_isInfectionCasting = false;
		_infectionCastStartTime = 0f;
		_infectionCastDuration = 0f;
	}
}
