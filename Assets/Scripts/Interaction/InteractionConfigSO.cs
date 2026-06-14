using UnityEngine;

[CreateAssetMenu(
    fileName = "InteractionConfig",
    menuName = "Game/Interaction/Interaction Config")]
public class InteractionConfigSO : ScriptableObject
{
    [Header("Portal")]
    [SerializeField, Min(0f)] private float _portalInteractCooldownSeconds = 1f;

    public float PortalInteractCooldownSeconds =>
        Mathf.Max(0f, _portalInteractCooldownSeconds);
}
