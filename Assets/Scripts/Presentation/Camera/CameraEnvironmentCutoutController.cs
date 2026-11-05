using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Presentation-only aperture in camera-side structures. Never changes colliders,
/// layers, renderer visibility, enemy discovery, interaction, or source material assets.
/// Runtime materials are installed only while the owning world camera renders.
/// </summary>
[DefaultExecutionOrder(1250)]
[DisallowMultipleComponent, RequireComponent(typeof(Camera))]
public sealed class CameraEnvironmentCutoutController : MonoBehaviour
{
    private static readonly int ApertureId = Shader.PropertyToID("_BCEnvironmentAperture");
    private static readonly int CameraParamsId = Shader.PropertyToID("_BCEnvironmentCameraParams");
    private static readonly int FadeId = Shader.PropertyToID("_BCEnvironmentFade");
    private static readonly int UnlitId = Shader.PropertyToID("_BCEnvironmentUnlit");
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private const string ShaderResource = "CameraEnvironmentCutout";

    [Header("Camera-side structures only")]
    [Tooltip("카메라 시야를 가리는 구조물/천장만 검사합니다. 캐릭터·바닥·아이템 레이어는 제외합니다.")]
    [SerializeField] private LayerMask _occluderMask = (1 << 7) | (1 << 11);
    [SerializeField, Min(.02f)] private float _queryInterval = .1f;
    [Tooltip("바닥 같은 얇고 낮은 메시를 투시 대상으로 잡지 않습니다. 높은 천장은 두께와 무관하게 허용합니다.")]
    [SerializeField, Min(.1f)] private float _minimumStructureHeight = 1.25f;

    private Camera _camera;
    private CameraManager _owner;
    private Shader _shader;
    private CameraEntityOcclusionFadeController _silhouettes;
    private RaycastHit[] _hits = new RaycastHit[64];
    private readonly Dictionary<Renderer, Binding> _bindings = new();
    private readonly Dictionary<Collider, Renderer[]> _colliderRenderers = new();
    private readonly HashSet<Renderer> _wanted = new();
    private readonly List<Renderer> _remove = new();
    private readonly List<Renderer> _candidates = new();
    private float _nextQuery;
    private Vector3 _lastFocus;
    private bool _hasFocus, _materialsInstalled, _reportedMissingShader, _reportedQueryLimit, _reportedUnsupportedMaterial;
    private Vector4 _aperture, _cameraParams;

    public bool IsOperational => isActiveAndEnabled && _owner != null && _shader != null && _shader.isSupported;

    internal void Configure(Camera camera, CameraManager owner)
    {
        _camera = camera;
        _owner = owner;
        ResolveShader();
        enabled = _shader != null && _shader.isSupported;
        _nextQuery = 0f;
        UpdatePlayerSilhouette();
    }

    private void Awake() { _camera = GetComponent<Camera>(); ResolveShader(); }
    private void OnEnable()
    {
        if (_camera == null) _camera = GetComponent<Camera>();
        ResolveShader();
        RenderPipelineManager.beginCameraRendering += BeginCameraRendering;
        RenderPipelineManager.endCameraRendering += EndCameraRendering;
        RenderPipelineManager.endContextRendering += EndContextRendering;
        _nextQuery = 0f;
        UpdatePlayerSilhouette();
    }
    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;
        RenderPipelineManager.endCameraRendering -= EndCameraRendering;
        RenderPipelineManager.endContextRendering -= EndContextRendering;
        ReleaseBindings();
        if (_silhouettes != null) _silhouettes.SetPlayerSilhouetteEnabled(true);
        _hasFocus = false;
    }
    private void OnDestroy() { ReleaseBindings(); }

    private void ResolveShader()
    {
        if (_shader == null) _shader = Resources.Load<Shader>(ShaderResource);
        if ((_shader == null || !_shader.isSupported) && !_reportedMissingShader)
        {
            _reportedMissingShader = true;
            Debug.LogWarning($"[{nameof(CameraEnvironmentCutoutController)}] Resources/{ShaderResource}.shader is missing or unsupported. Environment cutout stays off; original materials and silhouettes remain unchanged.", this);
        }
    }

    private void UpdatePlayerSilhouette()
    {
        if (_camera == null) return;
        if (_silhouettes == null) _silhouettes = _camera.GetComponent<CameraEntityOcclusionFadeController>();
        if (_silhouettes != null) _silhouettes.SetPlayerSilhouetteEnabled(!IsOperational);
    }

    private void LateUpdate()
    {
        if (!IsOperational || !_owner.isActiveAndEnabled || _camera == null) return;
        Transform target = _owner.CurrentFollowTarget;
        bool hasTarget = target != null && target.gameObject.activeInHierarchy;
        if (hasTarget)
        {
            Vector3 focus = target.position + Vector3.up;
            float radius = _owner.EnvironmentRevealRadius;
            if (_hasFocus && (focus - _lastFocus).sqrMagnitude > radius * radius * 16f)
            { ReleaseBindings(); _nextQuery = 0f; }
            _lastFocus = focus;
            _hasFocus = true;
            Vector3 viewport = _camera.WorldToViewportPoint(focus);
            hasTarget = viewport.z > _camera.nearClipPlane && viewport.x > -.25f && viewport.x < 1.25f && viewport.y > -.25f && viewport.y < 1.25f;
            if (hasTarget)
            {
                Vector3 top = _camera.WorldToViewportPoint(focus + _camera.transform.up * radius);
                float projectedRadius = Mathf.Max(.001f, Mathf.Abs(top.y - viewport.y));
                float softEdge = projectedRadius * Mathf.Clamp01(_owner.EnvironmentRevealSoftEdge / radius);
                _aperture = new Vector4(viewport.x, viewport.y, projectedRadius, Mathf.Max(.0001f, softEdge));
                _cameraParams = new Vector4(_camera.aspect, viewport.z + .25f, 1f, 0f);
                if (Time.unscaledTime >= _nextQuery)
                {
                    FindOccluders(focus, radius + _owner.EnvironmentRevealSoftEdge, target.position.y);
                    _nextQuery = Time.unscaledTime + _queryInterval;
                }
            }
        }
        if (!hasTarget) { _wanted.Clear(); _cameraParams.z = 0f; }
        UpdateBindings(Time.unscaledDeltaTime / _owner.EnvironmentRevealFadeSeconds);
    }

    private void FindOccluders(Vector3 focus, float radius, float groundY)
    {
        _wanted.Clear();
        bool needPlayerFallback = false;
        Vector3 origin = _camera.transform.position;
        Vector3 delta = focus - origin;
        float distance = delta.magnitude;
        if (distance < .001f || _occluderMask.value == 0) return;
        int count;
        do
        {
            count = Physics.SphereCastNonAlloc(origin, radius, delta / distance, _hits,
                distance, _occluderMask, QueryTriggerInteraction.Ignore);
            if (count < _hits.Length || _hits.Length >= 256) break;
            Array.Resize(ref _hits, _hits.Length * 2);
        } while (true);
        if (count == _hits.Length && !_reportedQueryLimit)
        {
            _reportedQueryLimit = true;
            Debug.LogWarning($"[{nameof(CameraEnvironmentCutoutController)}] Occluder query reached {count} hits. Some structures may remain opaque; reduce the reveal radius or narrow the structure mask.", this);
        }
        if (_colliderRenderers.Count > 256) _colliderRenderers.Clear();
        for (int i = 0; i < count; i++)
        {
            Collider collider = _hits[i].collider;
            if (collider == null) continue;
            if (!_colliderRenderers.TryGetValue(collider, out Renderer[] renderers))
            {
                renderers = ResolveStructureRenderers(collider);
                _colliderRenderers[collider] = renderers;
            }
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                Bounds bounds = renderer.bounds;
                if (bounds.size.y < _minimumStructureHeight && bounds.min.y < groundY + .75f) continue;
                if ((_camera.cullingMask & (1 << renderer.gameObject.layer)) == 0) continue;
                if (Binding.HasUnsupportedOpaqueMaterials(renderer)) needPlayerFallback = true;
                if (!_bindings.ContainsKey(renderer) && !Binding.SupportsRenderer(renderer))
                {
                    needPlayerFallback = true;
                    if (!_reportedUnsupportedMaterial)
                    {
                        _reportedUnsupportedMaterial = true;
                        Debug.LogWarning($"[{nameof(CameraEnvironmentCutoutController)}] '{renderer.name}' has no supported opaque texture/color material. It is left unchanged; Player silhouette remains as a fallback. Enemy visibility is unchanged.", renderer);
                    }
                    continue;
                }
                _wanted.Add(renderer);
                if (!_bindings.ContainsKey(renderer)) _bindings.Add(renderer, new Binding(renderer, _shader));
            }
        }
        if (_silhouettes != null) _silhouettes.SetPlayerSilhouetteEnabled(needPlayerFallback);
    }

    private Renderer[] ResolveStructureRenderers(Collider collider)
    {
        Bounds bounds = collider.bounds;
        bounds.Expand(.5f);
        Transform node = collider.transform;
        for (int level = 0; node != null && level < 4; level++, node = node.parent)
        {
            // Do not turn a separate invisible portal collider into a whole-sector material swap.
            if (node.GetComponent<SectorRuntime>() != null) break;
            _candidates.Clear();
            node.GetComponentsInChildren<Renderer>(true, _candidates);
            var found = new List<Renderer>();
            foreach (Renderer renderer in _candidates)
            {
                if (!(renderer is MeshRenderer) || !renderer.bounds.Intersects(bounds)) continue;
                if (renderer.GetComponentInParent<PlayerCharacter>() != null ||
                    renderer.GetComponentInParent<Enemy>() != null || renderer.GetComponentInParent<EntityShootCore>() != null) continue;
                found.Add(renderer);
            }
            if (found.Count > 0) return found.ToArray();
        }
        return Array.Empty<Renderer>();
    }

    private void UpdateBindings(float step)
    {
        _remove.Clear();
        foreach (var pair in _bindings)
        {
            Binding binding = pair.Value;
            binding.Fade = Mathf.MoveTowards(binding.Fade, _wanted.Contains(pair.Key) ? 1f : 0f, step);
            if (pair.Key == null || binding.Fade <= 0f)
            { binding.Dispose(); _remove.Add(pair.Key); }
        }
        foreach (Renderer renderer in _remove) _bindings.Remove(renderer);
    }

    private void BeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        // Also restore before any other camera, including minimap and Scene View.
        RestoreMaterials();
        Shader.SetGlobalVector(CameraParamsId, Vector4.zero);
        if (camera != _camera || !IsOperational || !_hasFocus || _cameraParams.z == 0f) return;
        Shader.SetGlobalVector(ApertureId, _aperture);
        Shader.SetGlobalVector(CameraParamsId, _cameraParams);
        _materialsInstalled = true;
        try
        {
            foreach (Binding binding in _bindings.Values) binding.Install();
        }
        catch
        {
            RestoreMaterials();
            throw;
        }
    }
    private void EndCameraRendering(ScriptableRenderContext context, Camera camera)
    { if (camera == _camera) RestoreMaterials(); }
    private void EndContextRendering(ScriptableRenderContext context, List<Camera> cameras) { RestoreMaterials(); }
    private void RestoreMaterials()
    {
        if (!_materialsInstalled) return;
        foreach (Binding binding in _bindings.Values) binding.Restore();
        _materialsInstalled = false;
        Shader.SetGlobalVector(CameraParamsId, Vector4.zero);
    }
    private void ReleaseBindings()
    {
        RestoreMaterials();
        foreach (Binding binding in _bindings.Values) binding.Dispose();
        _bindings.Clear(); _colliderRenderers.Clear(); _wanted.Clear(); _remove.Clear();
    }

    private sealed class Binding
    {
        private readonly Renderer _renderer;
        private readonly Shader _shader;
        private Material[] _original, _cutout;
        private bool _installed;
        public float Fade;
        public Binding(Renderer renderer, Shader shader) { _renderer = renderer; _shader = shader; }

        private static bool SupportsMaterial(Material material) => material != null && material.renderQueue <= 2500 &&
            (material.HasProperty("_BaseMap") || material.HasProperty("_MainTex") || material.HasProperty("_BaseColor") || material.HasProperty("_Color"));
        public static bool SupportsRenderer(Renderer renderer)
        {
            foreach (Material material in renderer.sharedMaterials) if (SupportsMaterial(material)) return true;
            return false;
        }
        public static bool HasUnsupportedOpaqueMaterials(Renderer renderer)
        {
            foreach (Material material in renderer.sharedMaterials)
                if (material != null && material.renderQueue <= 2500 && !SupportsMaterial(material)) return true;
            return false;
        }

        public void Install()
        {
            if (_renderer == null || !_renderer.enabled || Fade <= 0f) return;
            var current = _renderer.sharedMaterials;
            bool changed = _original == null || current.Length != _original.Length;
            if (!changed) for (int i = 0; i < current.Length; i++) if (current[i] != _original[i]) { changed = true; break; }
            if (changed) Rebuild(current);
            for (int i = 0; i < _cutout.Length; i++)
            {
                if (_cutout[i] == null || _cutout[i] == _original[i]) continue;
                // Original per-renderer property blocks stay in place. Never replace them.
                _cutout[i].SetFloat(FadeId, Fade);
            }
            _renderer.sharedMaterials = _cutout;
            _installed = true;
        }

        private void Rebuild(Material[] current)
        {
            DestroyMaterials();
            _original = current;
            _cutout = new Material[current.Length];
            for (int i = 0; i < current.Length; i++)
            {
                Material source = current[i];
                if (!SupportsMaterial(source))
                { _cutout[i] = source; continue; }
                var replacement = new Material(source)
                {
                    name = source.name + " (Camera-only Environment Cutout)",
                    hideFlags = HideFlags.HideAndDontSave,
                    shader = _shader
                };
                replacement.shaderKeywords = source.shaderKeywords;
                replacement.renderQueue = source.renderQueue;
                if (!source.HasProperty("_BaseMap") && source.HasProperty("_MainTex"))
                {
                    replacement.SetTexture(BaseMapId, source.GetTexture("_MainTex"));
                    replacement.SetTextureScale("_BaseMap", source.GetTextureScale("_MainTex"));
                    replacement.SetTextureOffset("_BaseMap", source.GetTextureOffset("_MainTex"));
                }
                if (!source.HasProperty("_BaseColor") && source.HasProperty("_Color")) replacement.SetColor(BaseColorId, source.GetColor("_Color"));
                if (!source.HasProperty("_Smoothness") && source.HasProperty("_Glossiness")) replacement.SetFloat("_Smoothness", source.GetFloat("_Glossiness"));
                replacement.SetFloat(UnlitId, source.shader != null && source.shader.name.IndexOf("Unlit", StringComparison.OrdinalIgnoreCase) >= 0 ? 1f : 0f);
                _cutout[i] = replacement;
            }
        }
        public void Restore()
        {
            if (_installed && _renderer != null) _renderer.sharedMaterials = _original;
            _installed = false;
        }
        private void DestroyMaterials()
        {
            if (_cutout == null) return;
            for (int i = 0; i < _cutout.Length; i++)
                if (_cutout[i] != null && (_original == null || _cutout[i] != _original[i]))
                { if (Application.isPlaying) Destroy(_cutout[i]); else DestroyImmediate(_cutout[i]); }
            _cutout = null;
        }
        public void Dispose() { Restore(); DestroyMaterials(); _original = null; }
    }
}
