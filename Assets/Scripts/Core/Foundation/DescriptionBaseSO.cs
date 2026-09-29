/*
 * Role hierarchy: Core > Foundation
 * Primary responsibility: Defines the DescriptionBaseSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

/// <summary>
/// Base class for ScriptableObjects that need a public description field.
/// </summary>
public class DescriptionBaseSO : SerializableScriptableObject
{
	[TextArea] public string description;
}
