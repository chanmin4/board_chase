/*
 * Role hierarchy: Core > SceneFlow > ScriptableObject
 * Primary responsibility: Defines the MenuSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

/// <summary>
/// This class contains Settings specific to Menus only
/// </summary>
[CreateAssetMenu(fileName = "NewMenu", menuName = "Scene Data/Menu")]
public class MenuSO : GameSceneSO { }
