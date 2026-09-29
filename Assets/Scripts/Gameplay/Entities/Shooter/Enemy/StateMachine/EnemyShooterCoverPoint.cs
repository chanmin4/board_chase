/*
 * Role hierarchy: Gameplay > Entities > Shooter > Enemy > StateMachine
 * Primary responsibility: Provides an authored cover anchor and peek locations for tactical EnemyShooters.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Author this on a scene object that represents usable cover. The anchor stays
/// protected from the target while the left/right points are the positions an
/// EnemyShooter may briefly use to fire around the cover edge.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyShooterCoverPoint : MonoBehaviour
{
    private static readonly List<EnemyShooterCoverPoint> ActivePoints = new();

    [Header("Positions")]
    [Tooltip("Protected waiting position. Empty uses this transform.")]
    [SerializeField] private Transform _coverAnchor;
    [Tooltip("Optional left-side firing position. At least one peek point is required.")]
    [SerializeField] private Transform _leftPeekPoint;
    [Tooltip("Optional right-side firing position. At least one peek point is required.")]
    [SerializeField] private Transform _rightPeekPoint;

    [Header("Reservation")]
    [Tooltip("Seconds an inactive reservation is kept before another shooter can take this point.")]
    [SerializeField, Min(0.1f)] private float _reservationTimeoutSeconds = 4f;

    [Header("Debug")]
    [SerializeField] private bool _drawGizmos = true;

    private EnemyShooterCombatController _occupant;
    private float _reservationExpiresAt;

    public static IReadOnlyList<EnemyShooterCoverPoint> RegisteredPoints => ActivePoints;
    public Vector3 CoverPosition => _coverAnchor != null ? _coverAnchor.position : transform.position;
    public bool HasPeekPoint => _leftPeekPoint != null || _rightPeekPoint != null;
    public EnemyShooterCombatController Occupant => IsReservationValid() ? _occupant : null;

    /// <summary>
    /// Creates a play-mode-only cover point used by the Debug Monitor fallback
    /// when a test sector has physical obstacles but no authored cover markers.
    /// The object is owned and destroyed by the requesting combat controller.
    /// </summary>
    public static EnemyShooterCoverPoint CreateRuntime(Vector3 coverPosition, Vector3 peekPosition)
    {
        GameObject root = new GameObject("RuntimeAutoCoverPoint");
        root.transform.position = coverPosition;

        EnemyShooterCoverPoint point = root.AddComponent<EnemyShooterCoverPoint>();
        point._drawGizmos = false;
        point._coverAnchor = root.transform;

        GameObject peek = new GameObject("PeekPoint");
        peek.transform.SetParent(root.transform, false);
        peek.transform.position = peekPosition;
        point._leftPeekPoint = peek.transform;
        point._rightPeekPoint = peek.transform;
        return point;
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (!ActivePoints.Contains(this))
            ActivePoints.Add(this);
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        ActivePoints.Remove(this);
        _occupant = null;
        _reservationExpiresAt = 0f;
    }

    // Purpose: Attempts to reserve and reports whether the request succeeded.
    public bool TryReserve(EnemyShooterCombatController requester)
    {
        if (requester == null || !isActiveAndEnabled)
            return false;

        if (IsReservationValid() && _occupant != requester)
            return false;

        _occupant = requester;
        _reservationExpiresAt = Time.time + Mathf.Max(0.1f, _reservationTimeoutSeconds);
        return true;
    }

    // Purpose: Releases the owned release resource.
    public void Release(EnemyShooterCombatController requester)
    {
        if (requester == null || _occupant != requester)
            return;

        _occupant = null;
        _reservationExpiresAt = 0f;
    }

    // Purpose: Attempts to get peek position and reports whether the request succeeded.
    public bool TryGetPeekPosition(bool preferLeft, int attempt, out Vector3 position)
    {
        Transform preferred = preferLeft ? _leftPeekPoint : _rightPeekPoint;
        Transform alternate = preferLeft ? _rightPeekPoint : _leftPeekPoint;
        Transform selected = attempt == 0 ? preferred : alternate;

        if (selected == null)
        {
            position = default;
            return false;
        }

        position = selected.position;
        return true;
    }

    // Purpose: Checks whether reservation valid is currently true.
    private bool IsReservationValid()
    {
        if (_occupant == null || !_occupant.isActiveAndEnabled || Time.time > _reservationExpiresAt)
        {
            _occupant = null;
            _reservationExpiresAt = 0f;
            return false;
        }

        return true;
    }

    // Purpose: Responds to the draw gizmos selected callback.
    private void OnDrawGizmosSelected()
    {
        if (!_drawGizmos)
            return;

        Vector3 anchor = CoverPosition;
        Gizmos.color = Occupant != null
            ? new Color(1f, 0.7f, 0.1f, 0.9f)
            : new Color(0.2f, 0.95f, 0.4f, 0.9f);
        Gizmos.DrawWireSphere(anchor, 0.24f);

        DrawPeekGizmo(_leftPeekPoint, new Color(0.2f, 0.65f, 1f, 0.9f));
        DrawPeekGizmo(_rightPeekPoint, new Color(0.85f, 0.35f, 1f, 0.9f));
    }

    // Purpose: Draws the peek gizmo authoring UI.
    private void DrawPeekGizmo(Transform peekPoint, Color color)
    {
        if (peekPoint == null)
            return;

        Gizmos.color = color;
        Gizmos.DrawWireSphere(peekPoint.position, 0.18f);
        Gizmos.DrawLine(CoverPosition, peekPoint.position);
    }
}
