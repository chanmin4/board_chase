/*
 * Role hierarchy: StateMachine > Core
 * Primary responsibility: Defines ChildStateMachine state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (VSplatter.StateMachine)
 */

using UnityEngine;

namespace VSplatter.StateMachine
{
	[DefaultExecutionOrder(200)]
	public class ChildStateMachine : StateMachine
	{
	}
}
