/*
 * Role hierarchy: StateMachine > Core
 * Primary responsibility: Defines the shared contract implemented by the IStateComponent role group.
 * Execution boundary: Runtime (VSplatter.StateMachine)
 */

namespace VSplatter.StateMachine
{
	interface IStateComponent
	{
		/// <summary>
		/// Called when entering the state.
		/// </summary>
		void OnStateEnter();

		/// <summary>
		/// Called when leaving the state.
		/// </summary>
		void OnStateExit();
	}
}
