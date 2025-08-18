using UnityEngine;

[ExecuteAlways]
public class BoardGrid : MonoBehaviour
{
    [Header("Grid")]
    public int width = 25;
    public int height = 25;
    public float tileSize = 1f;
    public Vector3 origin = Vector3.zero;   // 좌하단

    public bool InBounds(int ix, int iy) => ix >= 0 && iy >= 0 && ix < width && iy < height;

    public bool WorldToIndex(Vector3 world, out int ix, out int iy)
    {
        Vector3 local = world - origin;
        ix = Mathf.FloorToInt(local.x / tileSize);
        iy = Mathf.FloorToInt(local.z / tileSize);
        return InBounds(ix, iy);
    }

    public Vector3 IndexToWorld(int ix, int iy)
    {
        return origin + new Vector3((ix + 0.5f) * tileSize, 0f, (iy + 0.5f) * tileSize);
    }

    public bool SnapToNearest(ref Vector3 pos, out int ix, out int iy)
    {
        if (!WorldToIndex(pos, out ix, out iy))
        {
            Vector3 local = pos - origin;
            ix = Mathf.Clamp(Mathf.FloorToInt(local.x / tileSize), 0, width - 1);
            iy = Mathf.Clamp(Mathf.FloorToInt(local.z / tileSize), 0, height - 1);
        }
        pos = IndexToWorld(ix, iy);
        return true;
    }
}
