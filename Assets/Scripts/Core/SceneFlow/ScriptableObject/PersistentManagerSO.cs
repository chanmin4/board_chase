/*
 * Role hierarchy: Core > SceneFlow > ScriptableObject
 * Primary responsibility: Defines the PersistentManagerSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

//CreateAssetMenu commented since we don't want to create more than one Initialisation GameSceneSO
[CreateAssetMenu(fileName = "PersistentManager", menuName = "Scene Data/PersistentManager")]
public class PersistentManagerSO : GameSceneSO { } 
