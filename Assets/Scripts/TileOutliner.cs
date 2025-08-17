using UnityEngine;

[ExecuteAlways]
public class TileOutliner : MonoBehaviour
{
    [Header("Tile size (local)")]
    public float sizeX = 1f;
    public float sizeZ =0.95f;
    public float sizeY = 1f;

    [Header("Border")]
    public float thickness = 0.01f;
    public float height = 0.02f;     // 윗면에서 살짝 띄워 Z-fighting 방지
    public Color color = Color.black;

    [Header("Material (optional)")]
    public Material customUnlit;      // 비워두면 자동 생성/캐싱

    const string ROOT_NAME = "_BorderRoot";
    const string FRONT = "Border_Front";
    const string BACK  = "Border_Back";
    const string RIGHT = "Border_Right";
    const string LEFT  = "Border_Left";

    Material _cachedMat;

    void OnEnable()   { BuildOrUpdate(); }
    void OnValidate() { BuildOrUpdate(); }

    [ContextMenu("Rebuild Border")]
    void RebuildMenu()
    {
        var old = transform.Find(ROOT_NAME);
        if (old) DestroyImmediate(old.gameObject);
        BuildOrUpdate();
    }

    void BuildOrUpdate()
    {
        // 루트 확보(없으면 생성)
        var rootTr = transform.Find(ROOT_NAME);
        if (rootTr == null)
        {
            rootTr = new GameObject(ROOT_NAME).transform;
            rootTr.SetParent(transform, false);
        }
        rootTr.localPosition = new Vector3(0, height, 0);

        // 머티리얼 준비(캐싱)
        if (_cachedMat == null)
        {
            _cachedMat = customUnlit != null
                ? customUnlit
                : new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        }
        _cachedMat.color = color;
        _cachedMat.renderQueue = 3001;

        // 4변 생성 또는 갱신
        CreateOrUpdateBar(rootTr, FRONT, new Vector3(0, sizeY,  sizeZ/2f), new Vector3(sizeX, thickness, thickness));
        CreateOrUpdateBar(rootTr, BACK,  new Vector3(0, sizeY, -sizeZ/2f), new Vector3(sizeX, thickness, thickness));
        CreateOrUpdateBar(rootTr, RIGHT, new Vector3( sizeX/2f, sizeY, 0), new Vector3(thickness, thickness, sizeZ));
        CreateOrUpdateBar(rootTr, LEFT,  new Vector3(-sizeX/2f, sizeY, 0), new Vector3(thickness, thickness, sizeZ));
    }

    void CreateOrUpdateBar(Transform parent, string name, Vector3 localPos, Vector3 localScale)
    {
        Transform t = parent.Find(name);
        if (t == null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            t = go.transform;

            // 콜라이더 불필요
            var col = go.GetComponent<Collider>();
            if (col) DestroyImmediate(col);
        }

        t.localPosition = localPos;
        t.localScale    = localScale;

        var mr = t.GetComponent<MeshRenderer>();
        if (mr == null) mr = t.gameObject.AddComponent<MeshRenderer>();

        // 머티리얼 적용(공유)
        if (mr.sharedMaterial != _cachedMat) mr.sharedMaterial = _cachedMat;
    }
}
