/*
 * Role hierarchy: Gameplay > Entities > Common > Config
 * Primary responsibility: Defines the DroppableRewardConfigSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DroppableRewardConfig", menuName = "EntityConfig/Reward Dropping Rate Config")]
public class DroppableRewardConfigSO : ScriptableObject
{
	[Tooltip("Item scattering distance from the source of dropping.")]
	[SerializeField] private float _scatteringDistance = default;
	[Tooltip("The list of drop goup that can be dropped by this critter when killed")]
	[SerializeField] private List<DropGroup> _dropGroups = new List<DropGroup>();

	public float ScatteringDistance => _scatteringDistance;
	public List<DropGroup> DropGroups => _dropGroups;

	// Purpose: Performs the drop special item workflow for this component.
	public virtual DropGroup DropSpecialItem()
	{
		return null; 
	}
}
