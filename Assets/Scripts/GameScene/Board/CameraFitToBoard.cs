using UnityEngine;

/// 카메라가 비추는 직사각형(ViewRect)에서 '보드 외곽 Rect'를 제외한
/// 바깥 4영역을 3D 쿼드로 깔아 가립니다.
[ExecuteAlways]
public class BoardPerimeterBg3D : MonoBehaviour
{
    [Header("Refs")]
    public BoardGrid board;               // GetWallOuterRectXZ() 제공
    public Camera cam;                    // 탑다운 Orthographic 카메라

    [Header("Background")]
    public Sprite backgroundSprite;       // 비우면 color 사용
    public Color  backgroundColor = Color.black;
    [Tooltip("보드 경계에서 바깥쪽으로 추가 여유(월드 단위)")]
    public float worldPadding = 0f;
    [Tooltip("쿼드를 깔 높이(Y). 바닥보다 아주 약간 아래(-0.01) 권장")]
    public float yLevel = 0f;
    [Tooltip("머티리얼을 URP Unlit/Texture 또는 Built-in Unlit/Texture로 자동 생성")]
    public bool autoCreateMaterial = true;

    Material _mat;                        // 생성한 머티리얼(런타임)
    Mesh _quad;                           // 1x1 XZ 쿼드
    Transform tTop, tBottom, tLeft, tRight;

    int _sw, _sh;
    Rect _lastBoardRect;
    float _lastY;

    void OnEnable()
    {
        if (!cam) cam = Camera.main;
        BuildQuadMeshIfNeeded();
        CreateOrGetChild(ref tTop,   "BG_Top");
        CreateOrGetChild(ref tBottom,"BG_Bottom");
        CreateOrGetChild(ref tLeft,  "BG_Left");
        CreateOrGetChild(ref tRight, "BG_Right");
        ApplyMaterial();
        UpdateNow();
    }

    void OnDisable()
    {
        // 생성한 머티리얼만 파괴(유저가 할당한 건 안 건드림)
        if (_mat) { if (Application.isPlaying) Destroy(_mat); else DestroyImmediate(_mat); _mat = null; }
    }

    void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) UpdateNow();
#endif
        if (_sw != Screen.width || _sh != Screen.height || _lastY != yLevel)
            UpdateNow();
    }

    void BuildQuadMeshIfNeeded()
    {
        if (_quad) return;
        _quad = new Mesh { name = "QuadXZ_Unit" };
        // XZ 평면(크기 1x1), 중심 (0,0), +Y를 향함
        _quad.vertices = new Vector3[] {
            new Vector3(-0.5f, 0f, -0.5f),
            new Vector3(-0.5f, 0f,  0.5f),
            new Vector3( 0.5f, 0f,  0.5f),
            new Vector3( 0.5f, 0f, -0.5f),
        };
        _quad.uv = new Vector2[] {
            new Vector2(0,0), new Vector2(0,1),
            new Vector2(1,1), new Vector2(1,0)
        };
        _quad.triangles = new int[] { 0,1,2, 0,2,3 };
        _quad.RecalculateNormals();
    }

    void CreateOrGetChild(ref Transform tr, string name)
    {
        var child = transform.Find(name);
        if (!child) {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.layer = LayerMask.NameToLayer("Default"); // 필요시 전용 레이어
            child = go.transform;
            child.SetParent(transform, false);
        }
        tr = child;
        var mf = tr.GetComponent<MeshFilter>();
        mf.sharedMesh = _quad;
        var mr = tr.GetComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        mr.allowOcclusionWhenDynamic = false;
    }

    void ApplyMaterial()
    {
        var mrArr = new[] {
            tTop?.GetComponent<MeshRenderer>(),
            tBottom?.GetComponent<MeshRenderer>(),
            tLeft?.GetComponent<MeshRenderer>(),
            tRight?.GetComponent<MeshRenderer>()
        };

        if (autoCreateMaterial)
        {
            // 적절한 Unlit 셰이더 찾기(URP 우선)
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (!sh) sh = Shader.Find("Unlit/Texture");
            if (!sh) sh = Shader.Find("Unlit/Color");

            if (_mat == null || _mat.shader != sh)
            {
                if (_mat) { if (Application.isPlaying) Destroy(_mat); else DestroyImmediate(_mat); }
                _mat = new Material(sh) { name = "BG_Fill_Auto" };
            }

            if (backgroundSprite)
            {
                if (_mat.HasProperty("_BaseMap")) _mat.SetTexture("_BaseMap", backgroundSprite.texture);
                if (_mat.HasProperty("_MainTex")) _mat.SetTexture("_MainTex", backgroundSprite.texture);
                if (_mat.HasProperty("_BaseColor")) _mat.SetColor("_BaseColor", Color.white);
                if (_mat.HasProperty("_Color")) _mat.SetColor("_Color", Color.white);
            }
            else
            {
                if (_mat.HasProperty("_BaseColor")) _mat.SetColor("_BaseColor", backgroundColor);
                if (_mat.HasProperty("_Color")) _mat.SetColor("_Color", backgroundColor);
            }

            foreach (var mr in mrArr) if (mr) mr.sharedMaterial = _mat;
        }
        else
        {
            // autoCreateMaterial 끄면, 각 MeshRenderer의 material은 사용자가 직접 할당
        }
    }

    [ContextMenu("Update Now")]
    public void UpdateNow()
    {
        _sw = Screen.width; _sh = Screen.height; _lastY = yLevel;
        if (!cam || !board) return;

        ApplyMaterial();

        // 카메라가 보는 월드 사각형 (XZ)
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        var cpos = cam.transform.position;
        float vL = cpos.x - halfW, vR = cpos.x + halfW;
        float vB = cpos.z - halfH, vT = cpos.z + halfH;

        // 보드 외곽 (XZ) + 여유
        var br = board.GetWallOuterRectXZ();
        br.xMin -= worldPadding; br.xMax += worldPadding;
        br.yMin -= worldPadding; br.yMax += worldPadding;

        // 기억해서 불필요 업데이트 줄이기
        if (_lastBoardRect == br) { /*pass*/ } else _lastBoardRect = br;

        // 각 영역의 실제 폭/높이 계산 (<=0 이면 숨김)
        float topH    = Mathf.Max(0f, vT - br.yMax);
        float bottomH = Mathf.Max(0f, br.yMin - vB);
        float leftW   = Mathf.Max(0f, br.xMin - vL);
        float rightW  = Mathf.Max(0f, vR - br.xMax);

        // TOP : (vL..vR, br.top..vT)
        PlaceRect(tTop,    vL, br.yMax, vR, vT, topH);
        // BOTTOM : (vL..vR, vB..br.bottom)
        PlaceRect(tBottom, vL, vB, vR, br.yMin, bottomH);
        // LEFT : (vL..br.left, max(vB,br.bottom)..min(vT,br.top))
        PlaceRect(tLeft,   vL, Mathf.Max(vB, br.yMin), br.xMin, Mathf.Min(vT, br.yMax), leftW);
        // RIGHT : (br.right..vR, max(vB,br.bottom)..min(vT,br.top))
        PlaceRect(tRight,  br.xMax, Mathf.Max(vB, br.yMin), vR, Mathf.Min(vT, br.yMax), rightW);

        // 안 쓰는 면은 끄기
        ToggleIfEmpty(tTop,    topH   <= 0f);
        ToggleIfEmpty(tBottom, bottomH<= 0f);
        ToggleIfEmpty(tLeft,   leftW  <= 0f);
        ToggleIfEmpty(tRight,  rightW <= 0f);
    }

    void PlaceRect(Transform tr, float x0, float z0, float x1, float z1, float thick)
    {
        if (!tr) return;
        if (thick <= 0f) return;
        float w = Mathf.Max(0f, x1 - x0);
        float h = Mathf.Max(0f, z1 - z0);
        if (w <= 0f || h <= 0f) return;

        tr.gameObject.SetActive(true);
        tr.position = new Vector3((x0 + x1) * 0.5f, yLevel, (z0 + z1) * 0.5f);
        tr.rotation = Quaternion.identity;
        tr.localScale = new Vector3(w, 1f, h);
    }

    void ToggleIfEmpty(Transform tr, bool off)
    {
        if (!tr) return;
        if (off && tr.gameObject.activeSelf) tr.gameObject.SetActive(false);
    }
}
