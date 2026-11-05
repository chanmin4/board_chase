/*
 * Role hierarchy: Presentation > Camera
 * Primary responsibility: Handles CameraEntityOcclusionFadeController camera, vision, range, or aiming calculations and presentation.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

[DefaultExecutionOrder(1200)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class CameraEntityOcclusionFadeController : MonoBehaviour
{
    // Keep the existing Resources path and .meta GUID so scene/prefab references remain stable.
    private const string SilhouetteShaderResourcePath = "CameraOccluderDither";
    private const string OverlayObjectSuffix = " [Camera Occlusion Silhouette]";
    private const float DefaultDepthBias = 0.0002f;

    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int SilhouetteColorId = Shader.PropertyToID("_SilhouetteColor");
    private static readonly int AlphaCutoffId = Shader.PropertyToID("_AlphaCutoff");
    private static readonly int DepthBiasId = Shader.PropertyToID("_DepthBias");

    private static readonly string[] SourceTexturePropertyNames =
    {
        "_BaseMap",
        "_BaseColorMap",
        "_MainTex"
    };

    [Header("Refs")]
    [Tooltip("World camera that owns the character-occlusion silhouette pass. Defaults to the Camera on this GameObject.")]
    [SerializeField] private Camera _worldCamera;

    [Header("Character Silhouette")]
    [Tooltip("Only these structure layers can activate a Character silhouette. Pixel depth then limits the overlay to the actually hidden body area.")]
    [SerializeField] private LayerMask _occluderMask = 1 << 7;

    [Tooltip("Color shown only on Character pixels hidden behind an occluder. Ordinary objects and structures never receive this material.")]
    [SerializeField] private Color _silhouetteColor = new Color(0.25f, 0.85f, 1f, 0.78f);

    [Tooltip("Minimum raw camera-depth difference required before a Character pixel is treated as hidden. This should remain a very small value.")]
    [SerializeField, Min(0.00001f)] private float _depthBias = DefaultDepthBias;

    [Tooltip("Seconds between PlayerCharacter, Enemy, and Shooter hierarchy scans. Runtime weapon views and corpse visuals are picked up automatically.")]
    [FormerlySerializedAs("_entityRefreshInterval")]
    [SerializeField, Min(0.05f)] private float _characterRefreshInterval = 0.25f;

    [Header("Runtime Don't Touch")]
    [ReadOnly] [SerializeField] private int _trackedCharacterCount;
    [ReadOnly] [SerializeField] private int _silhouetteRendererCount;

    private readonly List<CharacterTarget> _characterTargets = new List<CharacterTarget>();
    private readonly HashSet<Transform> _characterRoots = new HashSet<Transform>();
    private readonly HashSet<Renderer> _seenSources = new HashSet<Renderer>();
    private readonly HashSet<Renderer> _overlayRenderers = new HashSet<Renderer>();
    private readonly Dictionary<Renderer, OverlayBinding> _bindings =
        new Dictionary<Renderer, OverlayBinding>();
    private readonly List<Renderer> _bindingRemovalBuffer = new List<Renderer>();
    private readonly Dictionary<Material, Material> _silhouetteMaterials =
        new Dictionary<Material, Material>();

    private Shader _silhouetteShader;
    private Material _fallbackSilhouetteMaterial;
    private float _nextCharacterRefreshTime;
    private bool _includePlayerSilhouette = true;

    // The environment cutout replaces only the Player overlay. Enemy visibility and silhouettes stay unchanged.
    internal void SetPlayerSilhouetteEnabled(bool enabled)
    {
        if (_includePlayerSilhouette == enabled) return;
        _includePlayerSilhouette = enabled;
        if (isActiveAndEnabled && _silhouetteShader != null) RefreshCharacterTargets();
    }

    // Purpose: Ensures for is ready before the workflow continues.
    public static CameraEntityOcclusionFadeController EnsureFor(Camera worldCamera)
    {
        if (worldCamera == null)
            return null;

        if (worldCamera.TryGetComponent(out CameraEntityOcclusionFadeController existing))
            return existing;

        return worldCamera.gameObject.AddComponent<CameraEntityOcclusionFadeController>();
    }

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        _worldCamera = GetComponent<Camera>();

        int structureLayer = LayerMask.NameToLayer("SectorStructure");
        _occluderMask = structureLayer >= 0 ? 1 << structureLayer : 0;
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ResolveCameraAndShader();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        ResolveCameraAndShader();

        if (_silhouetteShader == null)
            return;

        RefreshCharacterTargets();
        _nextCharacterRefreshTime = Time.unscaledTime + _characterRefreshInterval;
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        ReleaseRuntimeResources();
    }

    // Purpose: Releases owned resources before this component is destroyed.
    private void OnDestroy()
    {
        ReleaseRuntimeResources();
    }

    // Purpose: Processes frame work that must run after the regular Update pass.
    private void LateUpdate()
    {
        if (_worldCamera == null || _silhouetteShader == null)
            return;

        if (Time.unscaledTime >= _nextCharacterRefreshTime)
        {
            RefreshCharacterTargets();
            _nextCharacterRefreshTime = Time.unscaledTime + _characterRefreshInterval;
        }

        Vector3 cameraPosition = _worldCamera.transform.position;
        int cameraCullingMask = _worldCamera.cullingMask;

        for (int i = _characterTargets.Count - 1; i >= 0; i--)
        {
            CharacterTarget target = _characterTargets[i];

            if (!target.IsValid)
            {
                target.SetSilhouetteEnabled(false, cameraCullingMask);
                _characterTargets.RemoveAt(i);
                continue;
            }

            bool shouldShow =
                target.TryGetVisibleBounds(cameraCullingMask, out Bounds bounds) &&
                IsInsideCameraFrustum(bounds.center) &&
                IsOccludedByStructure(cameraPosition, bounds);

            target.SetSilhouetteEnabled(shouldShow, cameraCullingMask);
        }

        _trackedCharacterCount = _characterTargets.Count;
        _silhouetteRendererCount = _bindings.Count;
    }

    // Purpose: Refreshes the displayed or cached character targets.
    private void RefreshCharacterTargets()
    {
        _characterTargets.Clear();
        _characterRoots.Clear();
        _seenSources.Clear();

        PlayerCharacter[] players = FindObjectsByType<PlayerCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < players.Length; i++)
            RegisterCharacter(players[i]);

        Enemy[] enemies = FindObjectsByType<Enemy>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < enemies.Length; i++)
            RegisterCharacter(enemies[i]);

        // Player and current Enemy Shooters are already covered above. This scan also
        // keeps future neutral/friendly Shooter implementations inside the same rule.
        EntityShootCore[] shooters = FindObjectsByType<EntityShootCore>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < shooters.Length; i++)
            RegisterCharacter(shooters[i]);

        RemoveStaleBindings();
        ApplySilhouetteSettings();

        _trackedCharacterCount = _characterTargets.Count;
        _silhouetteRendererCount = _bindings.Count;
    }

    // Purpose: Registers character with its owning runtime service.
    private void RegisterCharacter(Component characterMarker)
    {
        if (characterMarker == null || !characterMarker.gameObject.activeInHierarchy)
            return;

        Transform characterRoot = EntityComponentResolver.ResolveEntityRoot(characterMarker);

        if (!_includePlayerSilhouette && (characterMarker is PlayerCharacter ||
            (characterRoot != null && characterRoot.GetComponent<PlayerCharacter>() != null))) return;

        if (characterRoot == null ||
            !characterRoot.gameObject.activeInHierarchy ||
            !_characterRoots.Add(characterRoot))
        {
            return;
        }

        VisionVisibilityTarget visibilityTarget =
            characterRoot.GetComponentInChildren<VisionVisibilityTarget>(true);
        CharacterTarget target = new CharacterTarget(characterRoot, visibilityTarget);
        Renderer[] renderers = characterRoot.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer source = renderers[i];

            if (source == null || _overlayRenderers.Contains(source))
                continue;

            if (!(source is MeshRenderer) && !(source is SkinnedMeshRenderer))
                continue;

            if (!TryGetOrCreateBinding(source, out OverlayBinding binding))
                continue;

            _seenSources.Add(source);
            target.Add(binding);
            RefreshBindingGeometryAndMaterials(binding);
        }

        if (target.BindingCount > 0)
            _characterTargets.Add(target);
    }

    // Purpose: Attempts to get or create binding and reports whether the request succeeded.
    private bool TryGetOrCreateBinding(Renderer source, out OverlayBinding binding)
    {
        if (_bindings.TryGetValue(source, out binding) && binding != null && binding.IsValid)
            return true;

        binding = CreateBinding(source);

        if (binding == null)
            return false;

        _bindings[source] = binding;
        _overlayRenderers.Add(binding.Overlay);
        return true;
    }

    // Purpose: Creates the binding owned by this workflow.
    private OverlayBinding CreateBinding(Renderer source)
    {
        if (source == null)
            return null;

        GameObject overlayObject = new GameObject(source.name + OverlayObjectSuffix)
        {
            hideFlags = HideFlags.HideAndDontSave,
            layer = source.gameObject.layer
        };

        Transform overlayTransform = overlayObject.transform;
        overlayTransform.SetParent(source.transform, false);
        overlayTransform.localPosition = Vector3.zero;
        overlayTransform.localRotation = Quaternion.identity;
        overlayTransform.localScale = Vector3.one;

        OverlayBinding binding = null;

        if (source is SkinnedMeshRenderer sourceSkinned)
        {
            SkinnedMeshRenderer overlaySkinned =
                overlayObject.AddComponent<SkinnedMeshRenderer>();

            binding = new OverlayBinding(
                sourceSkinned,
                overlaySkinned,
                null,
                null);
        }
        else if (source is MeshRenderer sourceMeshRenderer)
        {
            MeshFilter sourceMeshFilter = source.GetComponent<MeshFilter>();

            if (sourceMeshFilter != null)
            {
                MeshFilter overlayMeshFilter = overlayObject.AddComponent<MeshFilter>();
                MeshRenderer overlayMeshRenderer = overlayObject.AddComponent<MeshRenderer>();

                binding = new OverlayBinding(
                    sourceMeshRenderer,
                    overlayMeshRenderer,
                    sourceMeshFilter,
                    overlayMeshFilter);
            }
        }

        if (binding == null)
        {
            DestroyRuntimeObject(overlayObject);
            return null;
        }

        ConfigureOverlayRenderer(source, binding.Overlay);
        return binding;
    }

    // Purpose: Configures this component''s overlay renderer.
    private static void ConfigureOverlayRenderer(Renderer source, Renderer overlay)
    {
        overlay.enabled = false;
        overlay.shadowCastingMode = ShadowCastingMode.Off;
        overlay.receiveShadows = false;
        overlay.lightProbeUsage = LightProbeUsage.Off;
        overlay.reflectionProbeUsage = ReflectionProbeUsage.Off;
        overlay.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        overlay.allowOcclusionWhenDynamic = false;
        overlay.renderingLayerMask = source.renderingLayerMask;
        overlay.rendererPriority = source.rendererPriority;
        overlay.sortingLayerID = source.sortingLayerID;
        overlay.sortingOrder = source.sortingOrder;

        if (overlay is SkinnedMeshRenderer overlaySkinned)
        {
            overlaySkinned.updateWhenOffscreen = true;
            overlaySkinned.skinnedMotionVectors = false;
        }
    }

    // Purpose: Refreshes the displayed or cached binding geometry and materials.
    private void RefreshBindingGeometryAndMaterials(OverlayBinding binding)
    {
        if (binding == null || !binding.IsValid)
            return;

        Mesh mesh = binding.RefreshGeometry();

        if (mesh == null)
        {
            binding.SetEnabled(false, _worldCamera != null ? _worldCamera.cullingMask : ~0);
            return;
        }

        Material[] sourceMaterials = binding.Source.sharedMaterials;

        if (!binding.RequiresMaterialRefresh(mesh, sourceMaterials))
            return;

        int subMeshCount = Mathf.Max(1, mesh.subMeshCount);
        Material[] overlayMaterials = new Material[subMeshCount];

        for (int i = 0; i < subMeshCount; i++)
        {
            Material sourceMaterial = null;

            if (sourceMaterials != null && sourceMaterials.Length > 0)
                sourceMaterial = sourceMaterials[Mathf.Min(i, sourceMaterials.Length - 1)];

            overlayMaterials[i] = GetOrCreateSilhouetteMaterial(sourceMaterial);
        }

        binding.ApplyMaterials(mesh, sourceMaterials, overlayMaterials);
    }

    // Purpose: Returns the current or create silhouette material.
    private Material GetOrCreateSilhouetteMaterial(Material source)
    {
        if (source == null)
        {
            if (_fallbackSilhouetteMaterial == null)
                _fallbackSilhouetteMaterial = CreateSilhouetteMaterial(null);

            return _fallbackSilhouetteMaterial;
        }

        if (_silhouetteMaterials.TryGetValue(source, out Material existing) && existing != null)
            return existing;

        Material created = CreateSilhouetteMaterial(source);
        _silhouetteMaterials[source] = created;
        return created;
    }

    // Purpose: Creates the silhouette material owned by this workflow.
    private Material CreateSilhouetteMaterial(Material source)
    {
        Material material = new Material(_silhouetteShader)
        {
            name = source != null
                ? source.name + " (Character Occlusion Silhouette)"
                : "Character Occlusion Silhouette (Fallback)",
            hideFlags = HideFlags.HideAndDontSave,
            enableInstancing = source != null && source.enableInstancing
        };

        ResolveSourceTexture(source, out Texture texture, out Vector2 scale, out Vector2 offset);
        material.SetTexture(BaseMapId, texture);
        material.SetTextureScale("_BaseMap", scale);
        material.SetTextureOffset("_BaseMap", offset);
        material.SetColor(SilhouetteColorId, _silhouetteColor);
        material.SetFloat(AlphaCutoffId, ResolveAlphaCutoff(source));
        material.SetFloat(DepthBiasId, _depthBias);
        return material;
    }

    // Purpose: Checks whether occluded by structure is currently true.
    private bool IsOccludedByStructure(Vector3 cameraPosition, Bounds bounds)
    {
        if (_occluderMask.value == 0)
            return false;

        Vector3 center = bounds.center;
        float minY = bounds.min.y;
        float maxY = bounds.max.y;

        Vector3 lowerSample = new Vector3(center.x, Mathf.Lerp(minY, maxY, 0.2f), center.z);
        Vector3 upperSample = new Vector3(center.x, Mathf.Lerp(minY, maxY, 0.85f), center.z);

        return HasStructureBetween(cameraPosition, center) ||
               HasStructureBetween(cameraPosition, lowerSample) ||
               HasStructureBetween(cameraPosition, upperSample);
    }

    // Purpose: Checks whether this component has structure between.
    private bool HasStructureBetween(Vector3 cameraPosition, Vector3 targetPoint)
    {
        Vector3 direction = targetPoint - cameraPosition;
        float distance = direction.magnitude;

        if (distance <= 0.01f)
            return false;

        return Physics.Raycast(
            cameraPosition,
            direction / distance,
            Mathf.Max(0f, distance - 0.01f),
            _occluderMask,
            QueryTriggerInteraction.Ignore);
    }

    // Purpose: Checks whether inside camera frustum is currently true.
    private bool IsInsideCameraFrustum(Vector3 worldPoint)
    {
        Vector3 viewport = _worldCamera.WorldToViewportPoint(worldPoint);

        return viewport.z > 0f &&
               viewport.x >= -0.05f && viewport.x <= 1.05f &&
               viewport.y >= -0.05f && viewport.y <= 1.05f;
    }

    // Purpose: Removes the requested stale bindings from this component.
    private void RemoveStaleBindings()
    {
        _bindingRemovalBuffer.Clear();

        foreach (KeyValuePair<Renderer, OverlayBinding> pair in _bindings)
        {
            if (pair.Key == null ||
                pair.Value == null ||
                !pair.Value.IsValid ||
                !_seenSources.Contains(pair.Key))
            {
                _bindingRemovalBuffer.Add(pair.Key);
            }
        }

        for (int i = 0; i < _bindingRemovalBuffer.Count; i++)
        {
            Renderer source = _bindingRemovalBuffer[i];

            if (!_bindings.TryGetValue(source, out OverlayBinding binding))
                continue;

            DestroyBinding(binding);
            _bindings.Remove(source);
        }

        _bindingRemovalBuffer.Clear();
    }

    // Purpose: Removes binding from the active runtime context.
    private void DestroyBinding(OverlayBinding binding)
    {
        if (binding == null)
            return;

        Renderer overlay = binding.Overlay;

        if (overlay != null)
        {
            overlay.enabled = false;
            _overlayRenderers.Remove(overlay);
            DestroyRuntimeObject(overlay.gameObject);
        }
    }

    // Purpose: Applies the requested silhouette settings change.
    private void ApplySilhouetteSettings()
    {
        foreach (Material material in _silhouetteMaterials.Values)
        {
            if (material == null)
                continue;

            material.SetColor(SilhouetteColorId, _silhouetteColor);
            material.SetFloat(DepthBiasId, _depthBias);
        }

        if (_fallbackSilhouetteMaterial != null)
        {
            _fallbackSilhouetteMaterial.SetColor(SilhouetteColorId, _silhouetteColor);
            _fallbackSilhouetteMaterial.SetFloat(DepthBiasId, _depthBias);
        }
    }

    // Purpose: Releases the owned runtime resources resource.
    private void ReleaseRuntimeResources()
    {
        foreach (OverlayBinding binding in _bindings.Values)
            DestroyBinding(binding);

        _bindings.Clear();
        _overlayRenderers.Clear();
        _seenSources.Clear();
        _characterTargets.Clear();
        _characterRoots.Clear();
        _bindingRemovalBuffer.Clear();

        foreach (Material material in _silhouetteMaterials.Values)
            DestroyRuntimeObject(material);

        _silhouetteMaterials.Clear();

        if (_fallbackSilhouetteMaterial != null)
            DestroyRuntimeObject(_fallbackSilhouetteMaterial);

        _fallbackSilhouetteMaterial = null;
        _trackedCharacterCount = 0;
        _silhouetteRendererCount = 0;
    }

    // Purpose: Resolves the camera and shader needed by this component.
    private void ResolveCameraAndShader()
    {
        if (_worldCamera == null)
            _worldCamera = GetComponent<Camera>();

        if (_worldCamera != null)
            _worldCamera.depthTextureMode |= DepthTextureMode.Depth;

        if (_occluderMask.value == 0)
        {
            int structureLayer = LayerMask.NameToLayer("SectorStructure");
            _occluderMask = structureLayer >= 0 ? 1 << structureLayer : 0;
        }

        // Migrates the earlier eye-depth default during script hot reload.
        if (_depthBias > 0.01f)
            _depthBias = DefaultDepthBias;

        if (_silhouetteShader == null)
            _silhouetteShader = Resources.Load<Shader>(SilhouetteShaderResourcePath);

        if (_silhouetteShader == null)
        {
            Debug.LogError(
                $"[{nameof(CameraEntityOcclusionFadeController)}] Required shader is missing at Resources/{SilhouetteShaderResourcePath}.shader. Character occlusion silhouettes are disabled.",
                this);
        }
    }

    // Purpose: Resolves the source texture needed by this component.
    private static void ResolveSourceTexture(
        Material source,
        out Texture texture,
        out Vector2 scale,
        out Vector2 offset)
    {
        if (source != null)
        {
            for (int i = 0; i < SourceTexturePropertyNames.Length; i++)
            {
                string propertyName = SourceTexturePropertyNames[i];

                if (!source.HasProperty(propertyName))
                    continue;

                Texture candidate = source.GetTexture(propertyName);

                if (candidate == null)
                    continue;

                texture = candidate;
                scale = source.GetTextureScale(propertyName);
                offset = source.GetTextureOffset(propertyName);
                return;
            }
        }

        texture = Texture2D.whiteTexture;
        scale = Vector2.one;
        offset = Vector2.zero;
    }

    // Purpose: Resolves the alpha cutoff needed by this component.
    private static float ResolveAlphaCutoff(Material source)
    {
        if (source == null)
            return 0.01f;

        if (source.HasProperty("_Cutoff"))
            return Mathf.Clamp01(source.GetFloat("_Cutoff"));

        if (source.HasProperty("_AlphaClipThreshold"))
            return Mathf.Clamp01(source.GetFloat("_AlphaClipThreshold"));

        return 0.01f;
    }

    // Purpose: Removes runtime object from the active runtime context.
    private static void DestroyRuntimeObject(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }

    // Purpose: Normalizes Inspector-authored data after it changes in the Editor.
    private void OnValidate()
    {
        _characterRefreshInterval = Mathf.Max(0.05f, _characterRefreshInterval);
        _depthBias = _depthBias > 0.01f
            ? DefaultDepthBias
            : Mathf.Max(0.00001f, _depthBias);
        _silhouetteColor.a = Mathf.Clamp01(_silhouetteColor.a);
        ApplySilhouetteSettings();
    }

    private sealed class CharacterTarget
    {
        private readonly Transform _root;
        private readonly VisionVisibilityTarget _visibilityTarget;
        private readonly List<OverlayBinding> _bindings = new List<OverlayBinding>();

        public CharacterTarget(
            Transform root,
            VisionVisibilityTarget visibilityTarget)
        {
            _root = root;
            _visibilityTarget = visibilityTarget;
        }

        public bool IsValid => _root != null && _root.gameObject.activeInHierarchy;
        public int BindingCount => _bindings.Count;

        // Purpose: Adds the requested add to this component.
        public void Add(OverlayBinding binding)
        {
            if (binding != null)
                _bindings.Add(binding);
        }

        // Purpose: Attempts to get visible bounds and reports whether the request succeeded.
        public bool TryGetVisibleBounds(int cameraCullingMask, out Bounds bounds)
        {
            bounds = default;

            // Renderer lists can lag behind runtime weapon/corpse view changes. The
            // authoritative vision state prevents any newly created visual from leaking.
            if (_visibilityTarget != null && !_visibilityTarget.IsVisible)
                return false;

            bool hasBounds = false;

            for (int i = 0; i < _bindings.Count; i++)
            {
                OverlayBinding binding = _bindings[i];

                if (binding == null || !binding.CanSourceRender(cameraCullingMask))
                    continue;

                Bounds sourceBounds = binding.Source.bounds;

                if (!hasBounds)
                {
                    bounds = sourceBounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(sourceBounds);
                }
            }

            return hasBounds;
        }

        // Purpose: Updates the silhouette enabled state.
        public void SetSilhouetteEnabled(bool enabled, int cameraCullingMask)
        {
            for (int i = 0; i < _bindings.Count; i++)
            {
                OverlayBinding binding = _bindings[i];

                if (binding != null)
                    binding.SetEnabled(enabled, cameraCullingMask);
            }
        }
    }

    private sealed class OverlayBinding
    {
        private readonly MeshFilter _sourceMeshFilter;
        private readonly MeshFilter _overlayMeshFilter;
        private Mesh _lastMesh;
        private Material[] _lastSourceMaterials;

        public OverlayBinding(
            Renderer source,
            Renderer overlay,
            MeshFilter sourceMeshFilter,
            MeshFilter overlayMeshFilter)
        {
            Source = source;
            Overlay = overlay;
            _sourceMeshFilter = sourceMeshFilter;
            _overlayMeshFilter = overlayMeshFilter;
        }

        public Renderer Source { get; }
        public Renderer Overlay { get; }

        public bool IsValid => Source != null && Overlay != null;

        // Purpose: Refreshes the displayed or cached geometry.
        public Mesh RefreshGeometry()
        {
            if (!IsValid)
                return null;

            Overlay.gameObject.layer = Source.gameObject.layer;
            Overlay.renderingLayerMask = Source.renderingLayerMask;
            Overlay.rendererPriority = Source.rendererPriority;
            Overlay.sortingLayerID = Source.sortingLayerID;
            Overlay.sortingOrder = Source.sortingOrder;

            if (Source is SkinnedMeshRenderer sourceSkinned &&
                Overlay is SkinnedMeshRenderer overlaySkinned)
            {
                overlaySkinned.sharedMesh = sourceSkinned.sharedMesh;
                overlaySkinned.bones = sourceSkinned.bones;
                overlaySkinned.rootBone = sourceSkinned.rootBone;
                overlaySkinned.localBounds = sourceSkinned.localBounds;
                overlaySkinned.quality = sourceSkinned.quality;
                return sourceSkinned.sharedMesh;
            }

            if (_sourceMeshFilter != null && _overlayMeshFilter != null)
            {
                _overlayMeshFilter.sharedMesh = _sourceMeshFilter.sharedMesh;
                return _sourceMeshFilter.sharedMesh;
            }

            return null;
        }

        // Purpose: Performs the requires material refresh workflow for this component.
        public bool RequiresMaterialRefresh(Mesh mesh, Material[] sourceMaterials)
        {
            if (_lastMesh != mesh)
                return true;

            int currentLength = sourceMaterials != null ? sourceMaterials.Length : 0;
            int previousLength = _lastSourceMaterials != null ? _lastSourceMaterials.Length : 0;

            if (currentLength != previousLength)
                return true;

            for (int i = 0; i < currentLength; i++)
            {
                if (_lastSourceMaterials[i] != sourceMaterials[i])
                    return true;
            }

            return false;
        }

        // Purpose: Applies the requested materials change.
        public void ApplyMaterials(
            Mesh mesh,
            Material[] sourceMaterials,
            Material[] overlayMaterials)
        {
            if (!IsValid)
                return;

            Overlay.sharedMaterials = overlayMaterials;
            _lastMesh = mesh;
            _lastSourceMaterials = sourceMaterials != null
                ? (Material[])sourceMaterials.Clone()
                : null;
        }

        // Purpose: Checks whether source render is currently permitted.
        public bool CanSourceRender(int cameraCullingMask)
        {
            if (!IsValid ||
                !Source.enabled ||
                Source.forceRenderingOff ||
                !Source.gameObject.activeInHierarchy)
            {
                return false;
            }

            int layer = Source.gameObject.layer;
            return layer >= 0 && (cameraCullingMask & (1 << layer)) != 0;
        }

        // Purpose: Updates the enabled state.
        public void SetEnabled(bool enabled, int cameraCullingMask)
        {
            if (Overlay == null)
                return;

            bool shouldRender = enabled && CanSourceRender(cameraCullingMask);

            if (shouldRender &&
                Source is SkinnedMeshRenderer sourceSkinned &&
                Overlay is SkinnedMeshRenderer overlaySkinned &&
                sourceSkinned.sharedMesh != null)
            {
                int blendShapeCount = sourceSkinned.sharedMesh.blendShapeCount;

                for (int i = 0; i < blendShapeCount; i++)
                {
                    overlaySkinned.SetBlendShapeWeight(
                        i,
                        sourceSkinned.GetBlendShapeWeight(i));
                }
            }

            Overlay.enabled = shouldRender;
        }
    }
}
