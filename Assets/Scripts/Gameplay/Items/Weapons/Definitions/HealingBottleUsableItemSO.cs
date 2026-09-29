/*
 * Role hierarchy: Gameplay > Items > Weapons > Definitions
 * Primary responsibility: Defines the HealingBottleUsableItemSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "HealingBottle",
    menuName = "Game/Character Item/Usable/Throwable/Healing Bottle")]
public sealed class HealingBottleUsableItemSO : ThrowableUsableItemSO
{
    [Header("Healing Zone")]
    [Tooltip("World radius healed by the bottle's temporary area.")]
    [SerializeField, Min(0.01f)] private float _healingRadius = 3f;

    [Tooltip("Seconds before the healing zone, particle root, and temporary MaskRender visual are removed together.")]
    [SerializeField, Min(0.01f)] private float _healingDurationSeconds = 5f;

    [Tooltip("Seconds between healing pulses.")]
    [SerializeField, Min(0.01f)] private float _healingTickInterval = 0.2f;

    [Tooltip("Health restored to each living target inside the zone per pulse.")]
    [SerializeField, Min(0f)] private float _healPerTick = 2f;

    [Tooltip("Layers containing the Player/Enemy hurtboxes that this healing zone may heal.")]
    [SerializeField] private LayerMask _healingTargetMask = ~0;

    [Tooltip("Required zone prefab. Put HealingZoneRuntime on its root and healing particles below it.")]
    [SerializeField] private GameObject _healingZonePrefab;

    [Tooltip("Temporary healing paint visual parented below the zone root so it shares the exact same lifetime.")]
    [SerializeField] private MaskRenderSO _healingMaskRender;

    [Header("Readable Healing Zone")]
    [Tooltip("Independent fallback circle color. Kept separate from MaskRender so the healing area remains visible even when paint rendering is unavailable.")]
    [SerializeField] private Color _healingZoneVisualColor =
        new Color(0.12f, 1f, 0.32f, 0.45f);

    public float HealingRadius => Mathf.Max(0.01f, _healingRadius);
    public float HealingDurationSeconds => Mathf.Max(0.01f, _healingDurationSeconds);
    public float HealingTickInterval => Mathf.Max(0.01f, _healingTickInterval);
    public float HealPerTick => Mathf.Max(0f, _healPerTick);
    public LayerMask HealingTargetMask => _healingTargetMask;
    public GameObject HealingZonePrefab => _healingZonePrefab;
    public MaskRenderSO HealingMaskRender => _healingMaskRender;
    public override bool IsHostileThreat => false;
    public override Color ImpactZoneVisualColor => _healingZoneVisualColor;
    public override float ImpactZoneVisualDurationSeconds =>
        HealingDurationSeconds;
}
