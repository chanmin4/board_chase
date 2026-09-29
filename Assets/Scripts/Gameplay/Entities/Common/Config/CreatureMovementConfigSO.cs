/*
 * Role hierarchy: Gameplay > Entities > Common > Config
 * Primary responsibility: Defines the CreatureMovementConfigSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public class CreatureMovementConfigSO : ScriptableObject
{
	[SerializeField] private float _stopDuration;
	[SerializeField] private float _speed;

	public float Speed => _speed;
	public float StopDuration => _stopDuration;
}
