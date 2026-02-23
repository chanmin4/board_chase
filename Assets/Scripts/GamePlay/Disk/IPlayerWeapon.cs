using UnityEngine;

public interface IPlayerWeapon
{
    bool CanFire { get; }
    void Tick(float dt);
    bool TryFire(PlayerShoot shooter, Vector3 aimPoint);
}
