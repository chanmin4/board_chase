using UnityEngine;

/// <summary>
/// 섹터 단위 마스크 렌더러.
/// 
/// 역할:
/// 1. SectorRuntime.GetWorldBounds()를 기준으로 섹터 내부 영역을 잡는다.
/// 2. 백신/바이러스 마스크 텍스처를 생성한다.
/// 3. 백신/바이러스 오버레이 쿼드를 자동 생성한다.
/// 4. 원형 스탬프를 찍는다.
/// 5. 덮어씌우기 규칙(상대 마스크 지우기)을 처리한다.
/// 
/// 중요:
/// - 중립 = 두 마스크 모두 0
/// - 백신 = 백신 마스크 1, 바이러스 마스크 0
/// - 바이러스 = 바이러스 마스크 1, 백신 마스크 0
/// 
/// 즉, "싸움"은 코드가 만든다.
/// </summary>
[DisallowMultipleComponent]
public class MaskRenderer : MonoBehaviour
{
    /// <summary>
    /// 현재 픽셀이 어떤 상태인지 표현.
    /// </summary>
    public enum PaintState
    {
        Neutral,
        Vaccine,
        Virus
    }

    [Header("Refs")]
    [Tooltip("섹터 bounds를 제공하는 런타임 정보")]
    [SerializeField] private SectorRuntime runtime;

    [Header("Base Materials")]
    [Tooltip("백신 영역용 기본 머티리얼. _MaskTex 프로퍼티를 가져야 함")]
    [SerializeField] private Material vaccineBaseMaterial;

    [Tooltip("바이러스 영역용 기본 머티리얼. _MaskTex 프로퍼티를 가져야 함")]
    [SerializeField] private Material virusBaseMaterial;

    [Header("Material Property")]
    [Tooltip("마스크 텍스처를 셰이더에 넘길 프로퍼티 이름")]
    [SerializeField] private string maskTextureProperty = "_MaskTex";

    [Header("Mask Resolution")]
    [Tooltip("월드 1유닛당 픽셀 수. 높을수록 더 부드럽고 무거움")]
    [SerializeField] private int pixelsPerUnit = 16;

    [Header("Overlay")]
    [Tooltip("바닥보다 살짝 띄워서 Z-Fighting 방지")]
    [SerializeField] private float yOffset = 0.02f;

    [Tooltip("바이러스 오버레이가 위에 오도록 renderQueue를 더 크게 줄지 여부")]
    [SerializeField] private bool virusAboveVaccine = true;

    /// <summary>
    /// 실제 섹터 영역 bounds 캐시.
    /// SectorRuntime.GetWorldBounds()에서 가져온다.
    /// </summary>
    private Bounds _worldBounds;

    /// <summary>
    /// 백신/바이러스 마스크 텍스처.
    /// 알파 0=비어있음, 알파 255=칠해짐.
    /// </summary>
    private Texture2D _vaccineMask;
    private Texture2D _virusMask;

    /// <summary>
    /// 텍스처 쓰기용 버퍼.
    /// 실제 수정은 버퍼에 하고, LateUpdate에서 Texture2D에 Apply 한다.
    /// </summary>
    private Color32[] _vaccineBuffer;
    private Color32[] _virusBuffer;

    /// <summary>
    /// dirty 상태인 경우만 Apply 하도록 플래그 유지.
    /// </summary>
    private bool _vaccineDirty;
    private bool _virusDirty;

    /// <summary>
    /// 런타임 머티리얼 인스턴스.
    /// base material을 그대로 건드리지 않기 위해 복사해서 사용.
    /// </summary>
    private Material _vaccineMaterialInstance;
    private Material _virusMaterialInstance;

    /// <summary>
    /// 오버레이 렌더링용 자식 오브젝트.
    /// </summary>
    private MeshRenderer _vaccineRenderer;
    private MeshRenderer _virusRenderer;

    private MeshFilter _vaccineFilter;
    private MeshFilter _virusFilter;

    private int _textureWidth;
    private int _textureHeight;

    private void Reset()
    {
        if (!runtime)
            runtime = GetComponent<SectorRuntime>();
    }

    private void Awake()
    {
        if (!runtime)
            runtime = GetComponent<SectorRuntime>();

        if (runtime == null)
        {
            Debug.LogError("[MaskRenderer] SectorRuntime reference is missing.");
            enabled = false;
            return;
        }

        RefreshFromSectorBounds();
    }

    private void LateUpdate()
    {
        ApplyDirtyMasks();
    }

    private void OnDestroy()
    {
        ReleaseObject(_vaccineMask);
        ReleaseObject(_virusMask);
        ReleaseObject(_vaccineMaterialInstance);
        ReleaseObject(_virusMaterialInstance);
    }

    /// <summary>
    /// SectorRuntime의 bounds를 다시 읽어서
    /// 오버레이 크기/위치/텍스처를 다시 세팅한다.
    /// 
    /// 섹터 크기가 바뀔 일이 있으면 외부에서 이 함수를 다시 호출하면 된다.
    /// </summary>
    public void RefreshFromSectorBounds()
    {
        _worldBounds = runtime.GetWorldBounds();

        BuildOrUpdateOverlayObjects();
        BuildOrRebuildMaskTextures();
        BindTexturesToMaterials();
        ClearAllToNeutral();
        ApplyDirtyMasks();
    }

    /// <summary>
    /// 지정 위치가 현재 섹터 영역 안쪽인지 확인한다.
    /// </summary>
    public bool ContainsWorldPoint(Vector3 worldPos)
    {
        return worldPos.x >= _worldBounds.min.x &&
               worldPos.x <= _worldBounds.max.x &&
               worldPos.z >= _worldBounds.min.z &&
               worldPos.z <= _worldBounds.max.z;
    }

    /// <summary>
    /// 현재 월드 위치의 상태를 반환한다.
    /// Neutral / Vaccine / Virus
    /// </summary>
    public PaintState GetStateAtWorld(Vector3 worldPos)
    {
        int px, py;
        if (!TryWorldToPixel(worldPos, out px, out py))
            return PaintState.Neutral;

        int index = py * _textureWidth + px;

        bool vaccine = _vaccineBuffer != null && _vaccineBuffer[index].a > 0;
        bool virus = _virusBuffer != null && _virusBuffer[index].a > 0;

        if (vaccine && !virus) return PaintState.Vaccine;
        if (!vaccine && virus) return PaintState.Virus;

        // 원래 overwrite 규칙상 둘 다 true가 되면 안 됨.
        // 혹시라도 꼬였으면 Neutral보다 마지막 우선순위를 정해야 하지만,
        // 지금은 디버깅 쉽게 Virus 우선으로 두지 않고 Neutral로 본다.
        return PaintState.Neutral;
    }

    /// <summary>
    /// 원형 영역을 "백신" 상태로 만든다.
    /// 
    /// overwriteOther=true면 같은 영역의 바이러스 마스크를 같이 지운다.
    /// 즉 "백신이 바이러스를 덮어쓴다"는 규칙.
    /// </summary>
    public void StampVaccineCircle(Vector3 centerWorld, float radiusWorld, bool overwriteOther = true)
    {
        StampCircleAlpha(_vaccineBuffer, centerWorld, radiusWorld, 255, markVaccineDirty: true);

        if (overwriteOther)
            StampCircleAlpha(_virusBuffer, centerWorld, radiusWorld, 0, markVirusDirty: true);
    }

    /// <summary>
    /// 원형 영역을 "바이러스" 상태로 만든다.
    /// 
    /// overwriteOther=true면 같은 영역의 백신 마스크를 같이 지운다.
    /// 즉 "바이러스가 백신을 덮어쓴다"는 규칙.
    /// </summary>
    public void StampVirusCircle(Vector3 centerWorld, float radiusWorld, bool overwriteOther = true)
    {
        StampCircleAlpha(_virusBuffer, centerWorld, radiusWorld, 255, markVirusDirty: true);

        if (overwriteOther)
            StampCircleAlpha(_vaccineBuffer, centerWorld, radiusWorld, 0, markVaccineDirty: true);
    }

    /// <summary>
    /// 원형 영역을 중립 상태로 만든다.
    /// 즉 두 진영 모두 지운다.
    /// </summary>
    public void ClearToNeutralCircle(Vector3 centerWorld, float radiusWorld)
    {
        StampCircleAlpha(_vaccineBuffer, centerWorld, radiusWorld, 0, markVaccineDirty: true);
        StampCircleAlpha(_virusBuffer, centerWorld, radiusWorld, 0, markVirusDirty: true);
    }

    /// <summary>
    /// 전달된 상태대로 원형 영역을 적용한다.
    /// 외부에서 enum 기반으로 쓰고 싶을 때 사용.
    /// </summary>
    public void StampStateCircle(PaintState state, Vector3 centerWorld, float radiusWorld, bool overwriteOther = true)
    {
        switch (state)
        {
            case PaintState.Vaccine:
                StampVaccineCircle(centerWorld, radiusWorld, overwriteOther);
                break;

            case PaintState.Virus:
                StampVirusCircle(centerWorld, radiusWorld, overwriteOther);
                break;

            default:
                ClearToNeutralCircle(centerWorld, radiusWorld);
                break;
        }
    }

    /// <summary>
    /// 섹터 전체를 중립 상태로 초기화한다.
    /// </summary>
    public void ClearAllToNeutral()
    {
        ClearBuffer(_vaccineBuffer);
        ClearBuffer(_virusBuffer);
        _vaccineDirty = true;
        _virusDirty = true;
    }

    /// <summary>
    /// 백신 마스크 텍스처를 외부 디버깅용으로 반환.
    /// </summary>
    public Texture2D GetVaccineMaskTexture()
    {
        return _vaccineMask;
    }

    /// <summary>
    /// 바이러스 마스크 텍스처를 외부 디버깅용으로 반환.
    /// </summary>
    public Texture2D GetVirusMaskTexture()
    {
        return _virusMask;
    }

    /// <summary>
    /// 오버레이 쿼드와 렌더러를 만든다.
    /// 
    /// 백신/바이러스는 같은 위치의 쿼드 2장으로 렌더링한다.
    /// 중요한 건 "시각적으로 2장"이 아니라,
    /// 실제 픽셀 상태를 코드에서 반대편 지워주므로 겹쳐도 싸움 규칙이 유지된다는 점.
    /// </summary>
    private void BuildOrUpdateOverlayObjects()
    {
        CreateOverlayIfNeeded(
            childName: "VaccineOverlay",
            out _vaccineRenderer,
            out _vaccineFilter,
            ref _vaccineMaterialInstance,
            vaccineBaseMaterial);

        CreateOverlayIfNeeded(
            childName: "VirusOverlay",
            out _virusRenderer,
            out _virusFilter,
            ref _virusMaterialInstance,
            virusBaseMaterial);

        UpdateOverlayTransform(_vaccineRenderer.transform, _worldBounds, yOffset);
        UpdateOverlayTransform(_virusRenderer.transform, _worldBounds, yOffset + 0.001f);

        if (_vaccineMaterialInstance != null && _virusMaterialInstance != null)
        {
            int vaccineQueue = Mathf.Max(_vaccineMaterialInstance.renderQueue, 3600);
            int virusQueue = Mathf.Max(_virusMaterialInstance.renderQueue, virusAboveVaccine ? 3700 : 3599);

            _vaccineMaterialInstance.renderQueue = vaccineQueue;
            _virusMaterialInstance.renderQueue = virusQueue;
        }
    }

    /// <summary>
    /// 섹터 bounds 기준으로 텍스처 크기를 만들고,
    /// 백신/바이러스 마스크를 생성한다.
    /// 기존 텍스처가 있으면 폐기 후 재생성.
    /// </summary>
    private void BuildOrRebuildMaskTextures()
    {
        _textureWidth = Mathf.Max(1, Mathf.CeilToInt(_worldBounds.size.x * Mathf.Max(1, pixelsPerUnit)));
        _textureHeight = Mathf.Max(1, Mathf.CeilToInt(_worldBounds.size.z * Mathf.Max(1, pixelsPerUnit)));

        ReleaseObject(_vaccineMask);
        ReleaseObject(_virusMask);

        _vaccineMask = CreateMaskTexture(_textureWidth, _textureHeight, "VaccineMask");
        _virusMask = CreateMaskTexture(_textureWidth, _textureHeight, "VirusMask");

        _vaccineBuffer = CreateClearBuffer(_textureWidth, _textureHeight);
        _virusBuffer = CreateClearBuffer(_textureWidth, _textureHeight);

        _vaccineDirty = true;
        _virusDirty = true;
    }

    /// <summary>
    /// 생성한 마스크 텍스처를 머티리얼 인스턴스에 연결한다.
    /// </summary>
    private void BindTexturesToMaterials()
    {
        if (_vaccineMaterialInstance != null && _vaccineMaterialInstance.HasProperty(maskTextureProperty))
            _vaccineMaterialInstance.SetTexture(maskTextureProperty, _vaccineMask);

        if (_virusMaterialInstance != null && _virusMaterialInstance.HasProperty(maskTextureProperty))
            _virusMaterialInstance.SetTexture(maskTextureProperty, _virusMask);
    }

    /// <summary>
    /// 특정 버퍼에 원형 영역 알파를 쓴다.
    /// 
    /// alpha=255면 칠하기
    /// alpha=0이면 지우기
    /// 
    /// 이 함수 자체는 "무슨 팀인지" 모르고,
    /// 그냥 버퍼 하나에 원을 찍는 공용 함수다.
    /// </summary>
    private void StampCircleAlpha(Color32[] buffer, Vector3 centerWorld, float radiusWorld, byte alpha, bool markVaccineDirty = false, bool markVirusDirty = false)
    {
        if (buffer == null)
            return;

        int cx, cy;
        if (!TryWorldToPixel(centerWorld, out cx, out cy))
            return;

        float pixelsPerMeterX = _textureWidth / Mathf.Max(0.0001f, _worldBounds.size.x);
        float pixelsPerMeterY = _textureHeight / Mathf.Max(0.0001f, _worldBounds.size.z);
        float pixelsPerMeter = Mathf.Min(pixelsPerMeterX, pixelsPerMeterY);

        int radiusPx = Mathf.Max(1, Mathf.RoundToInt(radiusWorld * pixelsPerMeter));

        int minX = Mathf.Max(0, cx - radiusPx);
        int maxX = Mathf.Min(_textureWidth - 1, cx + radiusPx);
        int minY = Mathf.Max(0, cy - radiusPx);
        int maxY = Mathf.Min(_textureHeight - 1, cy + radiusPx);

        float rr = (radiusPx + 0.5f) * (radiusPx + 0.5f);

        for (int y = minY; y <= maxY; y++)
        {
            int dy = y - cy;
            int row = y * _textureWidth;

            for (int x = minX; x <= maxX; x++)
            {
                int dx = x - cx;
                if ((dx * dx) + (dy * dy) > rr)
                    continue;

                int index = row + x;
                Color32 c = buffer[index];
                c.a = alpha;
                buffer[index] = c;
            }
        }

        if (markVaccineDirty) _vaccineDirty = true;
        if (markVirusDirty) _virusDirty = true;
    }

    /// <summary>
    /// dirty 상태인 버퍼만 실제 Texture2D에 Apply 한다.
    /// 매 프레임 SetPixels32/Apply를 안 하기 위한 최적화용.
    /// </summary>
    private void ApplyDirtyMasks()
    {
        if (_vaccineDirty && _vaccineMask != null && _vaccineBuffer != null)
        {
            _vaccineMask.SetPixels32(_vaccineBuffer);
            _vaccineMask.Apply(false, false);
            _vaccineDirty = false;
        }

        if (_virusDirty && _virusMask != null && _virusBuffer != null)
        {
            _virusMask.SetPixels32(_virusBuffer);
            _virusMask.Apply(false, false);
            _virusDirty = false;
        }
    }

    /// <summary>
    /// 월드 좌표를 현재 섹터 마스크 픽셀 좌표로 변환한다.
    /// 
    /// SectorRuntime.GetWorldBounds()로 얻은 섹터 내부 직사각형 기준으로 계산한다.
    /// </summary>
    private bool TryWorldToPixel(Vector3 worldPos, out int px, out int py)
    {
        float x01 = Mathf.InverseLerp(_worldBounds.min.x, _worldBounds.max.x, worldPos.x);
        float y01 = Mathf.InverseLerp(_worldBounds.min.z, _worldBounds.max.z, worldPos.z);

        bool inside = x01 >= 0f && x01 <= 1f && y01 >= 0f && y01 <= 1f;

        px = Mathf.RoundToInt(x01 * (_textureWidth - 1));
        py = Mathf.RoundToInt(y01 * (_textureHeight - 1));

        px = Mathf.Clamp(px, 0, _textureWidth - 1);
        py = Mathf.Clamp(py, 0, _textureHeight - 1);

        return inside;
    }

    /// <summary>
    /// 오버레이 자식 오브젝트가 없으면 생성하고,
    /// 쿼드 메쉬와 머티리얼 인스턴스를 연결한다.
    /// </summary>
    private void CreateOverlayIfNeeded(string childName, out MeshRenderer mr, out MeshFilter mf, ref Material materialInstance, Material baseMaterial)
    {
        Transform child = transform.Find(childName);

        if (child == null)
        {
            GameObject go = new GameObject(childName);
            go.transform.SetParent(transform, false);

            mf = go.AddComponent<MeshFilter>();
            mr = go.AddComponent<MeshRenderer>();

            mf.sharedMesh = CreateUnitQuadXZMesh();
        }
        else
        {
            mf = child.GetComponent<MeshFilter>();
            mr = child.GetComponent<MeshRenderer>();

            if (mf == null) mf = child.gameObject.AddComponent<MeshFilter>();
            if (mr == null) mr = child.gameObject.AddComponent<MeshRenderer>();

            if (mf.sharedMesh == null)
                mf.sharedMesh = CreateUnitQuadXZMesh();
        }

        if (baseMaterial != null)
        {
            if (materialInstance == null)
                materialInstance = new Material(baseMaterial);

            mr.sharedMaterial = materialInstance;
        }

        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    /// <summary>
    /// unit quad를 XZ 평면용으로 생성한다.
    /// scale로 bounds 크기를 맞춘다.
    /// </summary>
    private Mesh CreateUnitQuadXZMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "MaskOverlayQuad_XZ";

        mesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, 0f, -0.5f),
            new Vector3( 0.5f, 0f, -0.5f),
            new Vector3( 0.5f, 0f,  0.5f),
            new Vector3(-0.5f, 0f,  0.5f)
        };

        mesh.uv = new Vector2[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        };

        mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    /// <summary>
    /// 오버레이 쿼드를 bounds 중심/크기에 맞게 배치한다.
    /// </summary>
    private void UpdateOverlayTransform(Transform target, Bounds bounds, float heightOffset)
    {
        target.position = new Vector3(bounds.center.x, bounds.center.y + heightOffset, bounds.center.z);
        target.rotation = Quaternion.identity;
        target.localScale = new Vector3(bounds.size.x, 1f, bounds.size.z);
    }

    /// <summary>
    /// Alpha8 기반 마스크 텍스처를 생성한다.
    /// </summary>
    private Texture2D CreateMaskTexture(int width, int height, string textureName)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.Alpha8, false, true);
        tex.name = textureName;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }

    /// <summary>
    /// 전부 투명(중립) 상태 버퍼 생성.
    /// </summary>
    private Color32[] CreateClearBuffer(int width, int height)
    {
        Color32[] buffer = new Color32[width * height];
        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = new Color32(0, 0, 0, 0);
        return buffer;
    }

    /// <summary>
    /// 버퍼를 전부 중립 상태로 초기화한다.
    /// </summary>
    private void ClearBuffer(Color32[] buffer)
    {
        if (buffer == null)
            return;

        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = new Color32(0, 0, 0, 0);
    }

    /// <summary>
    /// 런타임 생성 리소스를 안전하게 해제한다.
    /// </summary>
    private void ReleaseObject(Object obj)
    {
        if (obj == null)
            return;

        if (Application.isPlaying)
            Destroy(obj);
        else
            DestroyImmediate(obj);
    }
}