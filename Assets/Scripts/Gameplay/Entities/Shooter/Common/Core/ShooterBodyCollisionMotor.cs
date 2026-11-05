using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>NavMesh plans; CharacterController commits collision-checked motion.
/// Hit/interaction triggers remain separate from the solid body.</summary>
[DisallowMultipleComponent, DefaultExecutionOrder(100)]
public sealed class ShooterBodyCollisionMotor : MonoBehaviour
{
    private NavMeshAgent _agent;
    private CharacterController _body;
    private bool _corpse;
    private bool _previousUpdatePosition;
    private const float BodyGap = .01f;
    private static readonly List<CharacterController> Bodies = new();
    private static readonly HashSet<CharacterController> RegisteredBodies = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetBodies()
    {
        Bodies.Clear();
        RegisteredBodies.Clear();
    }

    public static void RegisterBody(CharacterController body)
    {
        if (body != null && RegisteredBodies.Add(body)) Bodies.Add(body);
    }

    public static ShooterBodyCollisionMotor Ensure(NavMeshAgent agent)
    {
        if (agent == null) return null;
        var motor = agent.GetComponent<ShooterBodyCollisionMotor>();
        if (motor == null) motor = agent.gameObject.AddComponent<ShooterBodyCollisionMotor>();
        motor.Bind(agent);
        return motor;
    }

    public void Bind(NavMeshAgent agent)
    {
        if (_corpse || agent == null) return;
        if (_agent == null) _previousUpdatePosition = agent.updatePosition;
        _agent = agent;
        _body = GetComponent<CharacterController>();
        // Unity's missing native components can be fake-null in the Editor;
        // null-coalescing does not perform UnityEngine.Object's validity check.
        if (_body == null) _body = gameObject.AddComponent<CharacterController>();
        _body.radius = Mathf.Max(.1f, agent.radius);
        _body.height = Mathf.Max(_body.radius * 2, agent.height);
        _body.center = Vector3.up * (_body.height * .5f + agent.baseOffset);
        _body.skinWidth = Mathf.Min(.03f, _body.radius * .1f);
        _body.stepOffset = 0; // Never step onto another living actor.
        _body.minMoveDistance = 0;
        _body.detectCollisions = true;
        RegisterBody(_body);
        _agent.updatePosition = false;
        _agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        // Equal priorities can make coincident agents choose the same response.
        _agent.avoidancePriority = 30 + (int)((uint)GetInstanceID() % 40);
    }

    private void LateUpdate()
    {
        if (_corpse || _agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh ||
            _body == null || !_body.enabled) return;
        CommitPlannedPosition(_agent.nextPosition);
    }

    public void CommitPlannedPosition(Vector3 plannedPosition)
    {
        if (_body == null || !_body.enabled || _corpse) return;
        MoveWithBodyBlocking(_body, plannedPosition - transform.position);
        if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
            _agent.nextPosition = transform.position;
    }

    /// <summary>Stop at the first body contact, before committing movement.
    /// Read live Transform poses: no stale physics-frame positions, per-frame
    /// scene searches, allocations, or global Physics.SyncTransforms calls.</summary>
    public static CollisionFlags MoveWithBodyBlocking(CharacterController body, Vector3 delta)
    {
        if (body == null || !body.enabled || !body.gameObject.activeInHierarchy) return CollisionFlags.None;
        RegisterBody(body);
        GetBodyShape(body, out Vector3 center, out float radius, out float height);
        Vector3 horizontal = new Vector3(delta.x, 0f, delta.z);
        float fraction = LimitTravel(center, radius, height, horizontal, body);
        // Keep gravity/grounding independent from horizontal body contact.
        delta.x *= fraction;
        delta.z *= fraction;
        Vector3 previousPosition = body.transform.position;
        CollisionFlags flags = body.Move(delta);
        // Native wall sliding can redirect a move away from its requested sweep.
        // Reject that redirected pose before rendering if it enters another body.
        if (!CanPlaceControllerAt(body, body.transform.position) && CanPlaceControllerAt(body, previousPosition))
        {
            body.enabled = false;
            body.transform.position = previousPosition;
            body.enabled = true;
            return flags | CollisionFlags.Sides;
        }
        return fraction < 1f ? flags | CollisionFlags.Sides : flags;
    }

    private static void GetBodyShape(CharacterController body, out Vector3 center, out float radius, out float height)
    {
        Vector3 scale = body.transform.lossyScale;
        center = body.transform.TransformPoint(body.center);
        radius = body.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        height = Mathf.Max(radius * 2f, body.height * Mathf.Abs(scale.y));
    }

    private static bool IsLiveBody(Collider body)
    {
        return body != null && body.enabled && body.gameObject.activeInHierarchy;
    }

    private static float LimitTravel(Vector3 center, float radius, float height, Vector3 delta, CharacterController ownBody)
    {
        float lengthSquared = delta.sqrMagnitude;
        if (lengthSquared < .00000001f) return 1f;
        float contactInset = .001f / Mathf.Sqrt(lengthSquared);
        float fraction = 1f;
        for (int i = Bodies.Count - 1; i >= 0; i--)
        {
            var other = Bodies[i];
            if (other == null) { RegisteredBodies.Remove(other); Bodies.RemoveAt(i); continue; }
            if (other == ownBody || !IsLiveBody(other)) continue;
            GetBodyShape(other, out Vector3 otherCenter, out float otherRadius, out float otherHeight);
            if (Mathf.Abs(center.y - otherCenter.y) >= (height + otherHeight) * .5f) continue;
            Vector3 offset = center - otherCenter;
            offset.y = 0f;
            float combined = radius + otherRadius + BodyGap;
            float toward = Vector3.Dot(offset, delta);
            if (toward >= 0f) continue; // Separating or tangential movement is allowed.
            float outside = offset.sqrMagnitude - combined * combined;
            if (outside <= 0f) { fraction = 0f; continue; }
            float discriminant = toward * toward - lengthSquared * outside;
            if (discriminant < 0f) continue;
            float contact = (-toward - Mathf.Sqrt(discriminant)) / lengthSquared;
            if (contact >= 0f) fraction = Mathf.Min(fraction, Mathf.Max(0f, contact - contactInset));
        }
        return Mathf.Clamp01(fraction);
    }

    public static bool CanPlaceBodyAt(Vector3 center, float radius, float height, CharacterController ignore = null)
    {
        for (int i = Bodies.Count - 1; i >= 0; i--)
        {
            var other = Bodies[i];
            if (other == ignore || !IsLiveBody(other)) continue;
            GetBodyShape(other, out Vector3 otherCenter, out float otherRadius, out float otherHeight);
            if (Mathf.Abs(center.y - otherCenter.y) >= (height + otherHeight) * .5f) continue;
            Vector3 offset = center - otherCenter;
            offset.y = 0f;
            if (offset.sqrMagnitude < Mathf.Pow(radius + otherRadius + BodyGap, 2f)) return false;
        }
        return true;
    }

    public static bool CanPlaceControllerAt(CharacterController body, Vector3 position)
    {
        if (body == null) return true;
        GetBodyShape(body, out Vector3 center, out float radius, out float height);
        return CanPlaceBodyAt(center + position - body.transform.position, radius, height, body);
    }

    public static bool CanSpawnAt(NavMeshAgent template, Transform parent, Vector3 position, Quaternion rotation)
    {
        if (template == null) return false;
        Vector3 scale = Vector3.Scale(template.transform.lossyScale, parent != null ? parent.lossyScale : Vector3.one);
        float radius = Mathf.Max(.1f, template.radius) * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float height = Mathf.Max(radius * 2f, template.height * Mathf.Abs(scale.y));
        Vector3 center = position + rotation * Vector3.Scale(Vector3.up * (template.height * .5f + template.baseOffset), scale);
        return CanPlaceBodyAt(center, radius, height);
    }

    /// <summary>Death releases the entire actor's movement footprint. Corpse visuals
    /// and loot triggers remain, but no solid collider or carved path obstacle does.</summary>
    public static void MakeCorpsePassThrough(Component source)
    {
        // The death controller can live on the DeathCorpse child. Never use
        // source.gameObject or transform.root (which may be the entire sector).
        Transform owner = EntityComponentResolver.ResolveEntityRoot(source);
        if (owner == null) return;

        foreach (var motor in owner.GetComponentsInChildren<ShooterBodyCollisionMotor>(true))
        {
            motor._corpse = true;
            motor.enabled = false;
        }
        foreach (var agent in owner.GetComponentsInChildren<NavMeshAgent>(true))
            agent.enabled = false;
        foreach (var obstacle in owner.GetComponentsInChildren<NavMeshObstacle>(true))
            obstacle.enabled = false;
        foreach (var body in owner.GetComponentsInChildren<CharacterController>(true))
        {
            RegisteredBodies.Remove(body);
            Bodies.Remove(body);
            body.enabled = false;
        }
        // Includes authored corpse meshes and legacy CorpseSolidBody instances.
        // Keep triggers enabled so corpse looting still works.
        foreach (var collider in owner.GetComponentsInChildren<Collider>(true))
            if (!(collider is CharacterController) && !collider.isTrigger) collider.enabled = false;
    }

    private void OnDisable()
    {
        if (!_corpse && _agent != null) _agent.updatePosition = _previousUpdatePosition;
    }

    private void OnEnable()
    {
        if (!_corpse && _agent != null) Bind(_agent);
    }
}
