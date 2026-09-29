/*
 * Role hierarchy: WIP > Legacy > NumericCurrency
 * Primary responsibility: Retains old enemy-prefab component serialization without granting retired numerical currency.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public class EnemyKillRewardSource : MonoBehaviour
{
    // Purpose: Retains the old creature-config binding API; physical loot is configured by the loot source instead.
    public void SetEnemyStatConfig(EnemyStatConfigSO enemyStatConfig)
    {
    }

    // Purpose: Retains the old shooter-config binding API; physical loot is configured by the loot source instead.
    public void SetEnemyShooterConfig(EnemyShooterConfigSO enemyShooterConfig)
    {
    }
}
