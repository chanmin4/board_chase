/*
 * Role hierarchy: Gameplay > Entities > Common > Core
 * Primary responsibility: Implements the focused DropItem responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[Serializable]
public class DropItem
{
	[SerializeField] ItemSO _item;
	[SerializeField] float _itemDropRate;

	public ItemSO Item => _item;
	public float ItemDropRate => _itemDropRate;
}
