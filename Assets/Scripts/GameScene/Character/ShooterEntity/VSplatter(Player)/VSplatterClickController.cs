using UnityEngine;

[DisallowMultipleComponent]
public class VSplatterClickController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private VSplatter_Character _character;
    [SerializeField] private PlayerAimAction _aimAction;
    [SerializeField] private PlayerShooterShoot _shoot;
    [SerializeField] private VSplatterDashController _dashController;
    [SerializeField] private PlayerBulletLoadoutRuntime _bulletLoadout;

    private bool _wasHoldingLastFrame;

    private void Reset()
    {
        ResolveRefs();
    }

    private void Awake()
    {
        ResolveRefs();
    }

    private void OnDisable()
    {
        _wasHoldingLastFrame = false;
    }

    private void LateUpdate()
    {
        if (GamePause.IsPaused ||
            Time.timeScale <= 0f ||
            GameplayAttackInputBlocker.IsBlocked)
        {
            _wasHoldingLastFrame = false;
            return;
        }

        if (_character == null ||
            _aimAction == null ||
            _bulletLoadout == null ||
            _shoot == null)
        {
            _wasHoldingLastFrame = false;
            return;
        }

        if (!_character.shootInput ||
            !TryGetSelectedShootAmmoType(out BulletAmmoType ammoType) ||
            !CanShoot())
        {
            _wasHoldingLastFrame = false;
            return;
        }

        if (!_wasHoldingLastFrame)
        {
            _wasHoldingLastFrame = true;
            return;
        }

        if (!_aimAction.CanFireNowFor(ammoType))
            return;

        _shoot.TryFireOnce(ammoType);
    }

    private void ResolveRefs()
    {
        if (_character == null)
            _character = GetComponent<VSplatter_Character>();

        if (_aimAction == null)
            _aimAction = GetComponent<PlayerAimAction>();

        if (_shoot == null)
            _shoot = GetComponent<PlayerShooterShoot>();

        if (_dashController == null)
            _dashController = GetComponent<VSplatterDashController>();

        if (_bulletLoadout == null)
            _bulletLoadout = GetComponent<PlayerBulletLoadoutRuntime>();
    }

    private bool TryGetSelectedShootAmmoType(out BulletAmmoType ammoType)
    {
        ammoType = default;

        if (!_bulletLoadout.TryGetSelectedAmmoType(out ammoType))
            return false;

        return ammoType == BulletAmmoType.AttackAndPaint ||
               ammoType == BulletAmmoType.Attack ||
               ammoType == BulletAmmoType.Paint;
    }

    private bool CanShoot()
    {
        return _dashController == null || !_dashController.IsDashing;
    }
}