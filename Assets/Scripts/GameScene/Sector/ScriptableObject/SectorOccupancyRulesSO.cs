using UnityEngine;

[CreateAssetMenu(fileName = "SectorOccupancyRules", menuName = "Game/Sector/Occupancy Rules")]
public class SectorOccupancyRulesSO : ScriptableObject
{
    [Tooltip("How often to sample the sector occupancy for capture progress calculation.")]
    public float sampleInterval = 0.25f;
    [Tooltip("How often to evaluate the sector occupancy for capture progress calculation.")]
   
    public float judgeInterval = 0.25f;
    [Tooltip("The ratio threshold for a player or virus to be considered dominant in the sector.")]
    public float captureThreshold = 0.5f;
    [Tooltip("How long the dominant side needs to hold the sector to capture it.")]
    public float captureHoldSeconds = 5f;
    [Tooltip("The stride for sampling the occupancy texture. Higher values mean less accuracy but better performance.")]
    public int sampleStride = 2;
}