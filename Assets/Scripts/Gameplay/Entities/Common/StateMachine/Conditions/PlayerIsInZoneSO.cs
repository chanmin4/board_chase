/*
 * Role hierarchy: Gameplay > Entities > Common > StateMachine > Conditions
 * Primary responsibility: Implements the focused ZoneType responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

public enum ZoneType
{
	Alert,
	Attack
}

public enum AlertDetectionMode
{
	TriggerZoneOnly,
	CurrentSectorOnly
}

[CreateAssetMenu(fileName = "PlayerIsInZone", menuName = "State Machines/Conditions/Player Is In Zone")]
public class PlayerIsInZoneSO : StateConditionSO<PlayerIsInZone>
{
	[Header("Zone")]
	public ZoneType zone;

	[Header("Alert Detection")]
	[Tooltip("Only used when Zone is Alert.")]
	[SerializeField] private AlertDetectionMode _alertDetectionMode = AlertDetectionMode.CurrentSectorOnly;
	[Tooltip("Current player sector channel. Required when Alert Detection Mode is Current Sector Only.")]
	[SerializeField] private SectorRuntimeEventChannelSO _currentSectorChangedEvent;

	public AlertDetectionMode AlertMode => _alertDetectionMode;

	// Purpose: Calculates for enemy from the current inputs.
	public bool EvaluateForEnemy(Enemy enemy)
	{
		if (enemy == null)
			return false;

		switch (zone)
		{
			case ZoneType.Alert:
				return EvaluateAlert(enemy);

			case ZoneType.Attack:
				return enemy.isPlayerInAttackZone;

			default:
				return false;
		}
	}

	// Purpose: Creates the condition owned by this workflow.
	protected override Condition CreateCondition() => new PlayerIsInZone();

	// Purpose: Calculates alert from the current inputs.
	private bool EvaluateAlert(Enemy enemy)
	{
		switch (_alertDetectionMode)
		{
			case AlertDetectionMode.CurrentSectorOnly:
				return IsPlayerInEnemySector(enemy);

			case AlertDetectionMode.TriggerZoneOnly:
			default:
				return enemy.isPlayerInAlertZone;
		}
	}

	// Purpose: Checks whether player in enemy sector is currently true.
	private bool IsPlayerInEnemySector(Enemy enemy)
	{
		if (enemy.CurrentSector == null ||
		    _currentSectorChangedEvent == null ||
		    _currentSectorChangedEvent.Current == null)
		{
			return false;
		}

		return enemy.CurrentSector == _currentSectorChangedEvent.Current;
	}
}

public class PlayerIsInZone : Condition
{
	private Enemy _enemy;

	// Purpose: Caches the component references required before runtime initialization.
	public override void Awake(StateMachine stateMachine)
	{
		_enemy = stateMachine.GetComponent<Enemy>();
	}

	// Purpose: Performs the statement workflow for this component.
	protected override bool Statement()
	{
		return ((PlayerIsInZoneSO)OriginSO).EvaluateForEnemy(_enemy);
	}
}
