/*
 * Role hierarchy: Gameplay > Items > Weapons > Definitions
 * Primary responsibility: Defines the ShotgunShellSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "ShotgunShell",
    menuName = "Game/Bullet Item/Basic/Shotgun Shell")]
public class ShotgunShellSO : BasicBulletSO
{
    [Header("Shotgun Pellets")]
    [Tooltip("Number of projectile pellets created by one consumed shell. The equipped shotgun's trigger-pull damage is divided across these pellets; Mark remains authored per pellet.")]
    [SerializeField, Min(1)] private int _pelletCount = 8;

    public override BulletFamily Family => BulletFamily.ShotgunShell;
    public int PelletCount => Mathf.Max(1, _pelletCount);
}
