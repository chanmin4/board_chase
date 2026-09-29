/*
 * Role hierarchy: StateMachine > ScriptableObjects
 * Primary responsibility: Defines the DescriptionSMActionBaseSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (VSplatter.StateMachine)
 */

using UnityEngine;

namespace VSplatter.StateMachine.ScriptableObjects
{
	/// <summary>
	/// Base class for StateMachine ScriptableObjects that need a public description field.
	/// </summary>
	public class DescriptionSMActionBaseSO : ScriptableObject
	{
		[TextArea] public string description;
	}

}