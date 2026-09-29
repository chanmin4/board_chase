/*
 * Role hierarchy: WIP > Legacy > NumericCurrency
 * Primary responsibility: Hides the retired numeric-currency HUD while its prefab remains for migration compatibility.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public class PlayerCurrencyHUD : MonoBehaviour
{
    // Purpose: Removes the old numerical wallet display from active HUD instances.
    private void Awake()
    {
        gameObject.SetActive(false);
    }
}
