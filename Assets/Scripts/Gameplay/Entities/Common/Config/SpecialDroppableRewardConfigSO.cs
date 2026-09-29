/*
 * Role hierarchy: Gameplay > Entities > Common > Config
 * Primary responsibility: Defines the SpecialDroppableRewardConfigSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DroppableRewardConfig", menuName = "EntityConfig/Special Reward Dropping Rate Config")]
public class SpecialDroppableRewardConfigSO : DroppableRewardConfigSO
{
	[Tooltip("Current count of dropped items")]
	[SerializeField] private int _specialDroppableCurrentCount = 0;
	[Tooltip("Max count where the special droppable needs to be dropped")]
	[SerializeField] private int _specialDroppableMaxCount = 0;
	[SerializeField] private DropGroup _specialItem = new DropGroup();

	// Purpose: Performs the drop special item workflow for this component.
	public override DropGroup DropSpecialItem()
	{
		_specialDroppableCurrentCount++;
		if (_specialDroppableCurrentCount >= _specialDroppableMaxCount)
			return _specialItem;
		else
			return null; 
	}
}
