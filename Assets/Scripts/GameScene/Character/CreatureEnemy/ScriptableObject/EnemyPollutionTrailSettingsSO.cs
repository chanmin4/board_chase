using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy/Enemy Pollution Trail Settings")]
public class EnemyPollutionTrailSettingsSO : ScriptableObject
{
    [Header("Stamp")]
    [Tooltip("Trail thickness on the ground mask.")]
    public float trailRadius = 0.8f;

    [Tooltip("Minimum movement distance required before sending a trail request.")]
    public float minSegmentDistance = 0.25f;


    [Tooltip("Paint priority passed to the mask manager.")]
    public int paintPriority = 10;

    [Header("Filtering")]
    [Tooltip("Movement slower than this is ignored.")]
    public float minMoveSpeed = 0.05f;

    [Tooltip("Movement larger than this in one frame is treated as teleport/spawn and does not paint.")]
    public float teleportResetDistance = 10f;
}
