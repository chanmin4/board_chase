/*
 * Role hierarchy: Gameplay > Items > Weapons > Definitions
 * Primary responsibility: Defines the ShotgunWeaponSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "ShotgunWeapon",
    menuName = "Game/Character Item/Weapon/Shotgun")]
public class ShotgunWeaponSO : RangedWeaponSO
{
    [Header("Shotgun Fire Pattern")]
    [Tooltip("Full horizontal angle covered by all pellets in one shot. Pellet count is owned by the equipped ShotgunShellSO.")]
    [SerializeField, Min(0f)] private float _pelletSpreadAngleDegrees = 14f;

    public override RangedWeaponType WeaponType => RangedWeaponType.Shotgun;
    public override RangedFirePattern FirePattern => RangedFirePattern.PelletSpread;
    public override float PelletSpreadAngleDegrees => Mathf.Max(0f, _pelletSpreadAngleDegrees);

    // Purpose: Resolves the projectile count needed by this component.
    public override int ResolveProjectileCount(BulletSO bullet)
    {
        return bullet is ShotgunShellSO shell ? shell.PelletCount : 1;
    }
}
