/*
 * Role hierarchy: Core > Anchors
 * Primary responsibility: Implements the focused TransformAnchor responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Runtime Anchors/Transform")]
public class TransformAnchor : RuntimeAnchorBase<Transform>{ }
