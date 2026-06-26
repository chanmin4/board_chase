using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerVisionFogOverlayUI : MonoBehaviour
{
    private static readonly int FogColorId = Shader.PropertyToID("_FogColor");
    private static readonly int CenterUVId = Shader.PropertyToID("_CenterUV");
    private static readonly int RadiusPixelsId = Shader.PropertyToID("_RadiusPixels");
    private static readonly int SoftnessPixelsId = Shader.PropertyToID("_SoftnessPixels");

    [Header("Refs")]
    [Tooltip("Full-screen Image used as the fog overlay. If empty, this object's Image is used.")]
    [SerializeField] private Image _fogImage;
    [Header("Auto Refs")]
    [Tooltip("Player EntityVisionController. If empty, the component tries to find one through VisionVisibilitySystem or scene search.")]
    
    [ReadOnly][SerializeField] private EntityVisionController _viewer;

    [Tooltip("World camera used to convert player vision range into screen pixels.")]
    [ReadOnly][SerializeField] private Camera _worldCamera;

    [Header("Listening To")]
    [SerializeField] private WorldCameraEventChannelSO _worldCameraReadyChannel;

    [Header("Fog")]
    [SerializeField] private Color _fogColor = new Color(0f, 0f, 0f, 0.55f);
    [SerializeField, Min(0f)] private float _edgeSoftnessPixels = 80f;

    [Tooltip("Optional direct shader reference. Assign UIVisionFog.shader here to avoid shader stripping in builds.")]
    [SerializeField] private Shader _fogShader;

    [Tooltip("Fallback shader name. Keep this unless the shader file was renamed.")]
    [SerializeField] private string _shaderName = "UI/VisionFog";

    [Header("Options")]
    [SerializeField] private bool _autoFindViewer = true;
    [SerializeField] private bool _hideWhenMissingRefs = true;

    private Material _runtimeMaterial;

    private void Reset()
    {
        ResolveRefs();
    }

    private void Awake()
    {
        ResolveRefs();
        EnsureMaterial();
    }

    private void OnEnable()
    {
        if (_worldCameraReadyChannel != null)
        {
            _worldCameraReadyChannel.OnEventRaised += OnWorldCameraReady;

            if (_worldCameraReadyChannel.Current != null)
                OnWorldCameraReady(_worldCameraReadyChannel.Current);
        }

        ResolveRefs();
        EnsureMaterial();
        SetImageVisible(true);
    }

    private void OnDisable()
    {
        if (_worldCameraReadyChannel != null)
            _worldCameraReadyChannel.OnEventRaised -= OnWorldCameraReady;
    }

    private void LateUpdate()
    {
        if (_viewer == null && _autoFindViewer)
            ResolveViewer();

        if (_worldCamera == null)
            _worldCamera = Camera.main;

        if (_fogImage == null || _runtimeMaterial == null || _viewer == null || _worldCamera == null)
        {
            if (_hideWhenMissingRefs)
                SetImageVisible(false);

            return;
        }

        SetImageVisible(true);
        UpdateFogMaterial();
    }

    private void OnDestroy()
    {
        if (_runtimeMaterial != null)
            Destroy(_runtimeMaterial);
    }

    private void UpdateFogMaterial()
    {
        Vector3 centerWorld = _viewer.Origin.position;
        Vector3 centerScreen = _worldCamera.WorldToScreenPoint(centerWorld);

        if (centerScreen.z <= 0f)
        {
            SetImageVisible(false);
            return;
        }

        float range = _viewer.VisionRange;
        Vector3 worldRight = _worldCamera.transform.right;
        worldRight.y = 0f;

        if (worldRight.sqrMagnitude <= 0.001f)
            worldRight = Vector3.right;
        else
            worldRight.Normalize();

        Vector3 worldForward = Vector3.ProjectOnPlane(_worldCamera.transform.forward, Vector3.up);

        if (worldForward.sqrMagnitude <= 0.001f)
            worldForward = Vector3.forward;
        else
            worldForward.Normalize();

        Vector3 rightScreen = _worldCamera.WorldToScreenPoint(centerWorld + worldRight * range);
        Vector3 forwardScreen = _worldCamera.WorldToScreenPoint(centerWorld + worldForward * range);

        float radiusX = Mathf.Max(1f, Vector2.Distance(
            new Vector2(centerScreen.x, centerScreen.y),
            new Vector2(rightScreen.x, rightScreen.y)));

        float radiusY = Mathf.Max(1f, Vector2.Distance(
            new Vector2(centerScreen.x, centerScreen.y),
            new Vector2(forwardScreen.x, forwardScreen.y)));

        Vector2 centerUV = new Vector2(
            Mathf.Clamp01(centerScreen.x / Mathf.Max(1f, Screen.width)),
            Mathf.Clamp01(centerScreen.y / Mathf.Max(1f, Screen.height)));

        _runtimeMaterial.SetColor(FogColorId, _fogColor);
        _runtimeMaterial.SetVector(CenterUVId, centerUV);
        _runtimeMaterial.SetVector(RadiusPixelsId, new Vector4(radiusX, radiusY, 0f, 0f));
        _runtimeMaterial.SetFloat(SoftnessPixelsId, _edgeSoftnessPixels);
    }

    private void ResolveRefs()
    {
        if (_fogImage == null)
            _fogImage = GetComponent<Image>();

        if (_viewer == null && _autoFindViewer)
            ResolveViewer();

        if (_worldCamera == null)
            _worldCamera = Camera.main;
    }

    private void ResolveViewer()
    {
        if (VisionVisibilitySystem.Instance != null &&
            VisionVisibilitySystem.Instance.Viewer != null)
        {
            _viewer = VisionVisibilitySystem.Instance.Viewer;
            return;
        }

        EntityVisionController[] candidates = FindObjectsByType<EntityVisionController>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < candidates.Length; i++)
        {
            EntityVisionController candidate = candidates[i];

            if (candidate == null)
                continue;

            if (candidate.GetComponent<PlayerStatsRuntime>() != null ||
                candidate.GetComponentInParent<PlayerStatsRuntime>() != null)
            {
                _viewer = candidate;
                return;
            }
        }
    }

    private void EnsureMaterial()
    {
        if (_fogImage == null || _runtimeMaterial != null)
            return;

        Shader shader = _fogShader != null
            ? _fogShader
            : Shader.Find(_shaderName);

        if (shader == null)
        {
            Debug.LogWarning($"[PlayerVisionFogOverlayUI] Shader not found. shader={_shaderName}", this);
            return;
        }

        _runtimeMaterial = new Material(shader)
        {
            name = "Runtime_PlayerVisionFog"
        };

        _fogImage.material = _runtimeMaterial;
        _fogImage.raycastTarget = false;
    }

    private void SetImageVisible(bool visible)
    {
        if (_fogImage != null && _fogImage.enabled != visible)
            _fogImage.enabled = visible;
    }

    private void OnWorldCameraReady(Camera camera)
    {
        _worldCamera = camera;
    }
}
