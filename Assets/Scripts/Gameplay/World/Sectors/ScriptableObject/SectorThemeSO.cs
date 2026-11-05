using UnityEngine;

/// <summary>Appearance only. Never owns combat props, collision, portals or navigation.</summary>
[CreateAssetMenu(fileName = "SectorTheme", menuName = "Game/Stage/Sector Theme")]
public sealed class SectorThemeSO : ScriptableObject
{
    [Tooltip("Optional single-MeshFilter prefab. Only its root mesh/materials are used; no scripts or colliders are instantiated.")]
    [SerializeField] private GameObject _floorVisual;
    [SerializeField] private Material _floorMaterial;
    [Tooltip("Optional panel appearance. Fits each registered wall panel's original bounds. Doors and portals are excluded.")]
    [SerializeField] private GameObject _wallVisual;
    [SerializeField] private Material _wallMaterial;

    public GameObject FloorVisual => _floorVisual;
    public Material FloorMaterial => _floorMaterial;
    public GameObject WallVisual => _wallVisual;
    public Material WallMaterial => _wallMaterial;

    public static bool IsVisualValid(GameObject visual)
    {
        if (visual == null) return true;
        var filter = visual.GetComponent<MeshFilter>();
        return filter != null && filter.sharedMesh != null && visual.GetComponent<MeshRenderer>() != null &&
            visual.GetComponentsInChildren<MeshFilter>(true).Length == 1 &&
            filter.sharedMesh.bounds.size.x > .00001f && filter.sharedMesh.bounds.size.z > .00001f;
    }
}
