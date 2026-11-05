using UnityEngine;

/// <summary>Attach to an authored HOME device with a collider; never spawns or rebuilds a room.</summary>
[DisallowMultipleComponent]
public sealed class HomeInfectionPurifier : MonoBehaviour
{
    [SerializeField, Min(.1f)] private float _interactionRange = 3f;
    public bool CanInteract => isActiveAndEnabled && !GameProgressionRuntime.InRaid;
    public string PromptLabel => "감염 정화 · 최대 체력 복구";
    public bool TryInteract(Component actor)
    {
        if (!CanInteract || actor == null) return false;
        var infection = EntityComponentResolver.Resolve<PlayerShooterInfection>(actor);
        if (infection == null) return false;
        var collider = GetComponent<Collider>();
        Vector3 point = collider != null ? collider.ClosestPoint(actor.transform.position) : transform.position;
        if ((point - actor.transform.position).sqrMagnitude > _interactionRange * _interactionRange) return false;
        return infection.TryPurifyAtHome();
    }
}
