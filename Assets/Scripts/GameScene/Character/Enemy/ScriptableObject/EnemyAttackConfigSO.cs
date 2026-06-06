using UnityEngine;
using UnityEngine.Serialization;

public abstract class EnemyAttackConfigSO : ScriptableObject
{
    [Header("Attack Identity")]
    [Tooltip("Inspector/debug name. If empty, the asset name is used.")]
    [SerializeField] private string _debugName;

    [Header("Flow")]
    [Tooltip("Delay after this attack's actual work is complete before the selected attack is cleared.")]
    [SerializeField, Min(0f)] private float _postAttackDelaySeconds = 2f;

    [Header("Attack Animator")]
    [Tooltip("If true, this attack sets an Animator trigger every time it actually fires a shot.")]
    [SerializeField] private bool _triggerAnimatorOnEachShot;

    [Tooltip("Animator trigger parameter used for each fired shot. Leave empty to skip.")]
    [SerializeField] private string _shotAnimatorTrigger;

    [Header("Attack Facing")]
    [Tooltip("If true, the enemy rotates toward its target while this attack is being performed.")]
    [SerializeField] private bool _faceTargetOnAttack = true;

    [Tooltip("If true, the enemy immediately snaps to the attack target when the attack starts or fires.")]
    [FormerlySerializedAs("_snapRotationOnChargeStart")]
    [SerializeField] private bool _snapFacingOnAttackStart = true;

    [Tooltip("If true, the enemy keeps rotating toward the target while the attack is active.")]
    [FormerlySerializedAs("_rotateWhileCharging")]
    [SerializeField] private bool _faceTargetWhileAttacking = true;

    [Tooltip("Rotation speed used when Snap Facing On Attack Start is false or while tracking during the attack.")]
    [FormerlySerializedAs("_rotationSpeedDegPerSec")]
    [SerializeField, Min(1f)] private float _attackFacingRotationSpeedDegPerSec = 1440f;

    public string DebugName =>
        !string.IsNullOrWhiteSpace(_debugName) ? _debugName : name;

    public float PostAttackDelaySeconds => Mathf.Max(0f, _postAttackDelaySeconds);
    public bool TriggerAnimatorOnEachShot => _triggerAnimatorOnEachShot;
    public string ShotAnimatorTrigger => _shotAnimatorTrigger;
    public bool FaceTargetOnAttack => _faceTargetOnAttack;
    public bool SnapFacingOnAttackStart => _snapFacingOnAttackStart;
    public bool FaceTargetWhileAttacking => _faceTargetWhileAttacking;
    public float AttackFacingRotationSpeedDegPerSec => Mathf.Max(1f, _attackFacingRotationSpeedDegPerSec);

    public bool TryFaceWorldPoint(Transform owner, Vector3 worldPoint, bool snap, float deltaTime)
    {
        if (owner == null)
            return false;

        return TryFaceDirection(owner, worldPoint - owner.position, snap, deltaTime);
    }

    public bool TryFaceDirection(Transform owner, Vector3 direction, bool snap, float deltaTime)
    {
        if (!_faceTargetOnAttack || owner == null)
            return false;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return false;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        owner.rotation = snap
            ? targetRotation
            : Quaternion.RotateTowards(
                owner.rotation,
                targetRotation,
                AttackFacingRotationSpeedDegPerSec * Mathf.Max(0f, deltaTime));

        return true;
    }
}
