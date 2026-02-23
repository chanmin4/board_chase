using UnityEngine;

[DisallowMultipleComponent]
public class Weapon_Pistol : MonoBehaviour, IPlayerWeapon
{
    [Header("Refs")]
    public InkBulletSpawner bulletSpawner; // 같은 오브젝트에 붙여두면 자동 Get 가능

    [Header("Direct Hit Damage")]
    public float directHitDamage = 25f;

    [Header("Paint Splash")]
    public float paintRadiusWorld = 0.6f;
    public bool clearEnemyMask = true;

    [Header("Fire")]
    public float fireCooldown = 0.18f;
    float _cd;

    [Header("Bullet")]
    public float bulletSpeed = 18f;

    [Header("Bullet Hit Masks")]
    public LayerMask damageMask; // Enemy
    public LayerMask blockMask;  // Wall

    public bool CanFire => _cd <= 0f;

    void Awake()
    {
        if (!bulletSpawner) bulletSpawner = GetComponent<InkBulletSpawner>();
        if (!bulletSpawner)
            Debug.LogWarning("[Weapon_Pistol] InkBulletSpawner가 없음. 같은 오브젝트에 추가해줘!");
    }

    public void Tick(float dt)
    {
        if (_cd > 0f) _cd = Mathf.Max(0f, _cd - dt);
    }

    public bool TryFire(PlayerShoot shooter, Vector3 aimPoint)
    {
        if (!CanFire) return false;
        if (shooter == null || shooter.disk == null) return false;
        if (!bulletSpawner) return false;

        float cd = Mathf.Max(0.01f, fireCooldown + shooter.CooldownAddSeconds);

        // 총알 스폰(공용 스포너)
        bulletSpawner.SpawnInkBullet(
            owner: shooter.disk,
            aimPoint: aimPoint,
            speed: bulletSpeed,
            directDamage: directHitDamage,
            paintRadiusWorld: paintRadiusWorld,
            clearMask: clearEnemyMask,
            damageMask: damageMask,
            blockMask: blockMask
        );

        _cd = cd;
        return true;
    }
}
