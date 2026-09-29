/*
 * Role hierarchy: Gameplay > Entities > Common > Core
 * Primary responsibility: Implements the focused DropGroup responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class DropGroup
{
	[SerializeField] List<DropItem> _drops;
	[SerializeField] float _dropRate;

	public List<DropItem> Drops => _drops;
	public float DropRate => _dropRate;
}
