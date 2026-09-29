/*
 * Role hierarchy: Presentation > UI > Gameplay > Shop > ScriptableObjects
 * Primary responsibility: Defines the PlayerShopCatalogSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[System.Obsolete("Use ShopConfigSO. This script remains only so old assets can be removed cleanly.")]
public sealed class PlayerShopCatalogSO : ScriptableObject
{
}
