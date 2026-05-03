using UnityEngine;

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
    [SerializeField] private Animator _animator;
    [SerializeField] private VSplatterAimAction _aimAction;
    [SerializeField] private VSplatterAttack _attack;
    [SerializeField] private VSplatterPaint _paint;
    [SerializeField] private VSplatterActionGate _actionGate;

    private bool _wasHoldingLastFrame;

    //private static readonly int ShootHash = Animator.StringToHash("Shoot");
    //private static readonly int IsShootingHash = Animator.StringToHash("IsShooting");

    private void Awake()
    {
        if (_actionGate == null)
            _actionGate = GetComponent<VSplatterActionGate>();
    }

    private void OnEnable()
    {
        if (_attack != null)
            _attack.Fired += OnFired;

        if (_paint != null)
            _paint.Fired += OnFired;
    }

    private void OnDisable()
    {
        if (_attack != null)
            _attack.Fired -= OnFired;

        if (_paint != null)
            _paint.Fired -= OnFired;
    }

    private void LateUpdate()
    {
        if (_character == null || _animator == null || _aimAction == null)
            return;

        bool holdAttack = _character.attackInput && (_actionGate == null || _actionGate.CanUseAttack);
        bool holdPaint = _character.paintInput && (_actionGate == null || _actionGate.CanUsePaint);
        bool holdAny = holdAttack || holdPaint;

        //_animator.SetBool(IsShootingHash, holdAny);

        if (!holdAny)
        {
            _wasHoldingLastFrame = false;
            return;
        }

        if (!_wasHoldingLastFrame)
        {
            _wasHoldingLastFrame = true;
            return;
        }

        if (!_aimAction.CanFireNow)
            return;

        FireMode mode = holdPaint ? FireMode.Paint : FireMode.Attack;

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

    private void OnFired()
    {
        if (_animator == null)
            return;

       // _animator.ResetTrigger(ShootHash);
        //_animator.SetTrigger(ShootHash);
    }
}
