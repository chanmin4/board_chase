/*
 * Role hierarchy: Gameplay > Entities > Common > StateMachine > Actions > MovementActions
 * Primary responsibility: Implements the focused NPCMovementAction responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using System.Collections;

public abstract class NPCMovementAction
{
	// Purpose: Responds to the update callback.
	public abstract void OnUpdate();

	// Purpose: Responds to the state enter callback.
	public abstract void OnStateEnter();

	// Purpose: Responds to the state exit callback.
	public abstract void OnStateExit();
}
