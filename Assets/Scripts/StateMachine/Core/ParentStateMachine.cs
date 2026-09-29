/*
 * Role hierarchy: StateMachine > Core
 * Primary responsibility: Defines ParentStateMachine state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (VSplatter.StateMachine)
 */

using UnityEngine;

namespace VSplatter.StateMachine
{
	[DefaultExecutionOrder(-200)]
	public class ParentStateMachine : StateMachine
	{
	}
}
