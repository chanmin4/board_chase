/*
 * Role hierarchy: Gameplay > Interaction > Core
 * Primary responsibility: Handles Interaction interaction, pickup, trade, loot, or storage flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public class Interaction
{
	public InteractionType type;
	public GameObject interactableObject;

	public Interaction(InteractionType t, GameObject obj)
	{
		type = t;
		interactableObject = obj;
	}
}
