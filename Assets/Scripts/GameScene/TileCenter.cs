using System.Collections.Generic;
using UnityEngine;
public enum TileType {
    Safe,
    Random,
    Face1,
    Face2,
    Face3,
    Face4,
    Face5, Face6

}

public class TileCenter : MonoBehaviour
{
    public static readonly List<TileCenter> Registry = new();  // ← 레지스트리
    public TileType type = TileType.Safe;
    public int index;

    void OnEnable() { if (!Registry.Contains(this)) Registry.Add(this); }
    void OnDisable() { Registry.Remove(this); }
    
    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.15f);
    }
    #endif
}