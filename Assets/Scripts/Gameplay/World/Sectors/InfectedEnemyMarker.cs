using UnityEngine;

/// <summary>Virus-room enemies reuse existing visual/equipment/HFSM and add exposure on bullet contact.</summary>
public sealed class InfectedEnemyMarker : MonoBehaviour
{
    public float BulletExposure { get; private set; }
    public void Configure(float exposure)=>BulletExposure=Mathf.Max(0,exposure);
}
