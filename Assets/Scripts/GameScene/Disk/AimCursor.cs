using UnityEngine;

[DisallowMultipleComponent]
public class AimCursor : MonoBehaviour
{
    public PlayerDisk disk;
    public float yOffset = 0.03f;
    public float radiusWorld = 0.35f;

    [Header("Auto Visual")]
    public bool autoCreateCursor = true;
    public Transform cursor;

    void Awake()
    {
        if (!disk) disk = GetComponent<PlayerDisk>();

        if (autoCreateCursor && cursor == null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "AimCursor(Temp)";
            Destroy(go.GetComponent<Collider>());

            go.transform.localScale = new Vector3(radiusWorld * 2f, 0.01f, radiusWorld * 2f);

            // 머티리얼(임시)
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                if (mat.shader == null) mat = new Material(Shader.Find("Unlit/Color"));
                mat.color = new Color(0.2f, 1f, 0.6f, 0.85f);
                r.sharedMaterial = mat;
            }

            cursor = go.transform;
        }
    }

    void Update()
    {
        if (!disk || cursor == null) return;
        if (!disk.TryGetAimPoint(out var p)) return;

        cursor.position = new Vector3(p.x, disk.GroundY + yOffset, p.z);
    }
}
