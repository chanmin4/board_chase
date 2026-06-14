using UnityEngine;

[DisallowMultipleComponent]
public class VSplatterClickController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private VSplatter_Character _character;
    [SerializeField] private VSplatterAimAction _aimAction;
    [SerializeField] private VSplatterShoot _shoot;
    [SerializeField] private VSplatterActionGate _actionGate;
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

    private void ResolveRefs()
    {
        if (_character == null)
            _character = GetComponent<VSplatter_Character>();

        if (_aimAction == null)
            _aimAction = GetComponent<VSplatterAimAction>();

        if (_shoot == null)
            _shoot = GetComponent<VSplatterShoot>();

        if (_actionGate == null)
            _actionGate = GetComponent<VSplatterActionGate>();

        if (_bulletLoadout == null)
            _bulletLoadout = GetComponent<PlayerBulletLoadoutRuntime>();
    }

    private void OnDisable()
    {
        _wasHoldingLastFrame = false;
    }

    private void LateUpdate()
    {
        if (GamePause.IsPaused || Time.timeScale <= 0f)
        {
            _wasHoldingLastFrame = false;
            return;
        }

        if (GameplayAttackInputBlocker.IsBlocked)
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
            !CanUseAmmoType(ammoType))
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

    private bool TryGetSelectedShootAmmoType(out BulletAmmoType ammoType)
    {
        ammoType = default;

        if (!_bulletLoadout.TryGetSelectedAmmoType(out ammoType))
            return false;

        return ammoType == BulletAmmoType.AttackAndPaint ||
               ammoType == BulletAmmoType.Attack ||
               ammoType == BulletAmmoType.Paint;
    }

    private bool CanUseAmmoType(BulletAmmoType ammoType)
    {
        if (_actionGate == null)
            return true;

        return ammoType == BulletAmmoType.Paint
            ? _actionGate.CanUsePaint
            : _actionGate.CanUseAttack;
    }
}
