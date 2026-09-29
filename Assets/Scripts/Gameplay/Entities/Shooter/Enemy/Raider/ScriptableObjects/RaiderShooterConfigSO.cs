/*
 * Role hierarchy: Gameplay > Entities > Shooter > Enemy > Raider > ScriptableObjects
 * Primary responsibility: Defines the RaiderShooterConfigSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "RaiderShooterConfig",
    menuName = "Game/Enemy/Raider Shooter Config")]
public class RaiderShooterConfigSO : EnemyShooterConfigSO
{
    // Purpose: Performs the pick loadout workflow for this component.
    public EnemyShooterLoadout PickLoadout()
    {
        return PickEnemyLoadout();
    }
}
