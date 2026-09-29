/*
 * Role hierarchy: Gameplay > Progression > Hideout > Legacy
 * Primary responsibility: Preserves serialized-project compatibility for the retired code-generated Hideout portal component.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

/// <summary>
/// Legacy no-op component retained only because existing generated project files and older prefab data may reference it.
/// Hideout stage entry is now owned by <see cref="HideoutStageSelectStation"/> on an authored scene object.
/// </summary>
[Obsolete("HideoutStageEntryPortal is retired. Use HideoutStageSelectStation on an authored Hideout object.")]
[DisallowMultipleComponent]
public sealed class HideoutStageEntryPortal : MonoBehaviour
{
}
