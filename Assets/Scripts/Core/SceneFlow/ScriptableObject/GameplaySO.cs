/*
 * Role hierarchy: Core > SceneFlow > ScriptableObject
 * Primary responsibility: Defines the GameplaySO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

/// <summary>
/// This class contains Settings specific to Gameplay scenes only
/// </summary>

[CreateAssetMenu(fileName = "NewGameplayScene", menuName = "Scene Data/Gameplay")]

public class GameplaySO : GameSceneSO { }
