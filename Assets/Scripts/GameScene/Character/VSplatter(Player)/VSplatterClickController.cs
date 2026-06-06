using UnityEngine;

[DisallowMultipleComponent]
public class VSplatterClickController : MonoBehaviour
{
    public enum FireMode
    {
        None,
        Attack,
        Paint
    }

    [Header("Refs")]
    [SerializeField] private VSplatter_Character _character;
    [SerializeField] private VSplatterAimAction _aimAction;
    [SerializeField] private VSplatterAttack _attack;
    [SerializeField] private VSplatterPaint _paint;
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

        if (_attack == null)
            _attack = GetComponent<VSplatterAttack>();

        if (_paint == null)
            _paint = GetComponent<VSplatterPaint>();

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

        if (_character == null ||
            _aimAction == null ||
            _bulletLoadout == null)
        {
            _wasHoldingLastFrame = false;
            return;
        }

        if (!_character.shootInput ||
            !TryGetSelectedFireMode(out FireMode mode) ||
            !CanUseMode(mode))
        {
            _wasHoldingLastFrame = false;
            return;
        }

        if (!_wasHoldingLastFrame)
        {
            _wasHoldingLastFrame = true;
            return;
        }

        VSplatterAimAction.FireKind fireKind =
            mode == FireMode.Paint
                ? VSplatterAimAction.FireKind.Paint
                : VSplatterAimAction.FireKind.Attack;

        if (!_aimAction.CanFireNowFor(fireKind))
            return;

        switch (mode)
        {
            case FireMode.Attack:
                _attack?.TryFireOnce();
                break;

            case FireMode.Paint:
                _paint?.TryFireOnce();
                break;
        }
    }

    private bool TryGetSelectedFireMode(out FireMode mode)
    {
        mode = FireMode.None;

        if (!_bulletLoadout.TryGetSelectedAmmoType(
                out BulletAmmoType ammoType))
        {
            return false;
        }

        mode = ammoType switch
        {
            BulletAmmoType.AttackAndPaint => FireMode.Attack,
            BulletAmmoType.Attack => FireMode.Attack,
            BulletAmmoType.Paint => FireMode.Paint,
            _ => FireMode.None
        };

        return mode != FireMode.None;
    }

    private bool CanUseMode(FireMode mode)
    {
        if (_actionGate == null)
            return true;

        return mode == FireMode.Paint
            ? _actionGate.CanUsePaint
            : _actionGate.CanUseAttack;
    }
}