/*
 * Role hierarchy: StateMachine > Utilities
 * Primary responsibility: Defines the shared contract implemented by the InitOnlyAttribute role group.
 * Execution boundary: Runtime (VSplatter.StateMachine)
 */

using System;
using UnityEngine;

namespace VSplatter.StateMachine
{
	[AttributeUsage(AttributeTargets.Field)]
	public class InitOnlyAttribute : PropertyAttribute { }
}
