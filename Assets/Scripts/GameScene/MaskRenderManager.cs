using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class MaskRenderManager : MonoBehaviour
{

    [Serializable]
    public struct CirclePaintRequest
    {
        public PaintChannel channel;
        public Vector3 worldPos;
        public float radiusWorld;
        public int priority;
        public object sender;
        public PoisonPuddleDamageConfigSO poisonPuddleDamageConfig;
    }

    [Serializable]
    public struct CirclePaintImpact
    {
        public PaintChannel channel;
        public Vector3 worldPos;
        public float radiusWorld;
        public object sender;

        public int totalCells;
        public int neutralCells;
        public int overwrittenVirusCells;
        public int alreadyVaccineCells;
        public int alreadyVirusCells;

        public float neutralArea;
        public float overwrittenVirusArea;
        public float alreadyVaccineArea;
        public float totalArea;

        public float ValidArea => neutralArea + overwrittenVirusArea;
        public int ValidCells => neutralCells + overwrittenVirusCells;
    }

    [Header("Base Materials")]
    [SerializeField] private Material vaccineBaseMaterial;
    [SerializeField] private Material virusBaseMaterial;
    [SerializeField] private Material poisonPuddleBaseMaterial;

    [Header("Material Property")]
    [SerializeField] private string maskTextureProperty = "_MaskTex";

    [Header("Mask Resolution")]
    [SerializeField] private int pixelsPerUnit = 16;

    [Header("Broadcasting On")]
    [SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;

    [Header("Overlay")]
    [SerializeField] private float yOffset = 0.3f;
    [SerializeField] private bool virusAboveVaccine = true;

    [Header("GPU Paint")]
    [Tooltip("Optional. If empty, Hidden/VSplatter/PaintMaskStamp is created at runtime.")]
    [SerializeField] private Material _paintStampMaterial;
    [SerializeField, Range(0.001f, 0.5f)] private float _paintStampEdgeSoftness = 0.08f;

    [Header("Paint VFX")]
    [Tooltip("Particle spawned when a Vaccine paint request is accepted.")]
    [SerializeField] private GameObject _vaccinePaintBubbleParticlePrefab;

    [Tooltip("Particle spawned when a Virus paint request is accepted.")]
    [SerializeField] private GameObject _virusPaintBubbleParticlePrefab;

    [Tooltip("Particle spawned when a PoisonPuddle paint request is accepted.")]
    [SerializeField] private GameObject _poisonPuddlePaintBubbleParticlePrefab;

    [Tooltip("Particle spawned when a Special paint request is accepted.")]
    [SerializeField] private GameObject _specialPaintBubbleParticlePrefab;

    [Tooltip("Optional parent for runtime paint VFX instances.")]
    [SerializeField] private Transform _paintVfxRoot;

    [SerializeField] private Vector3 _paintBubbleWorldOffset = Vector3.zero;
    [SerializeField, Min(0.1f)] private float _paintBubbleFallbackLifetime = 1f;

    [Header("Performance")]
    [FormerlySerializedAs("_maxFastTrailCirclesPerFrame")]
    [SerializeField, Min(0)] private int _maxTrailCirclesPerFrame = 128;

    private static readonly int StampShaderId = Shader.PropertyToID("_Stamp");
    private static readonly int StampSoftnessShaderId = Shader.PropertyToID("_StampSoftness");

    private readonly List<SectorPaint> _registeredSectors = new();
    private int _trailCircleBudgetFrame = -1;
    private int _trailCircleBudgetCount;
    private bool _missingPaintStampMaterialLogged;

    public event Action<CirclePaintRequest> OnCircleRequestAccepted;
    public event Action<CirclePaintRequest> OnCircleRequestRejected;
    public event Action<CirclePaintImpact> OnCirclePaintImpactAccepted;

    private void OnEnable()
    {
        if (_maskRenderManagerReadyChannel != null)
            _maskRenderManagerReadyChannel.RaiseEvent(this);
    }

    private void OnDisable()
    {
        if (_maskRenderManagerReadyChannel != null)
            _maskRenderManagerReadyChannel.Clear(this);
    }

    public void RegisterSector(SectorPaint sector)
    {
        if (sector == null)
            return;

        if (_registeredSectors.Contains(sector))
            return;

        _registeredSectors.Add(sector);
        RefreshSector(sector);
    }

    public void UnregisterSector(SectorPaint sector)
    {
        if (sector == null)
            return;

        _registeredSectors.Remove(sector);
    }

    public bool CanRequestCircle(
        PaintChannel channel,
        Vector3 worldPos,
        float radiusWorld,
        object sender = null)
    {
        CirclePaintRequest request = BuildCircleRequest(
            channel,
            worldPos,
            radiusWorld,
            0,
            sender,
            null);

        for (int i = 0; i < _registeredSectors.Count; i++)
        {
            SectorPaint sector = _registeredSectors[i];
            if (sector == null)
                continue;

            if (sector.CanAcceptCircle(request))
                return true;
        }

        return false;
    }

    public bool RequestVirusTrailSegment(
        Vector3 fromWorld,
        Vector3 toWorld,
        float radiusWorld,
        int priority = 0,
        object sender = null,
        float spacingWorld = 0f,
        int maxSteps = 3)
    {
        Vector3 from = fromWorld;
        Vector3 to = toWorld;

        from.y = 0f;
        to.y = 0f;

        float distance = Vector3.Distance(from, to);

        if (distance <= 0.001f)
        {
            CirclePaintRequest singleRequest = BuildCircleRequest(
                PaintChannel.Virus,
                toWorld,
                radiusWorld,
                priority,
                sender,
                null);

            return RequestVirusTrailCircleInternal(singleRequest);
        }

        float spacing = spacingWorld > 0f
            ? spacingWorld
            : Mathf.Max(radiusWorld * 1.5f, 0.25f);

        int steps = Mathf.Clamp(
            Mathf.CeilToInt(distance / Mathf.Max(0.001f, spacing)),
            1,
            Mathf.Max(1, maxSteps));

        bool acceptedAny = false;

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector3 position = Vector3.Lerp(fromWorld, toWorld, t);

            CirclePaintRequest request = BuildCircleRequest(
                PaintChannel.Virus,
                position,
                radiusWorld,
                priority,
                sender,
                null);

            if (RequestVirusTrailCircleInternal(request))
                acceptedAny = true;
        }

        return acceptedAny;
    }

    public bool RequestCircle(
        PaintChannel channel,
        Vector3 worldPos,
        float radiusWorld,
        int priority = 0,
        object sender = null)
    {
        CirclePaintRequest request = BuildCircleRequest(
            channel,
            worldPos,
            radiusWorld,
            priority,
            sender,
            null);

        return RequestCircleInternal(request);
    }

    public bool RequestCircle(
        PaintChannel channel,
        Vector3 worldPos,
        float radiusWorld,
        int priority,
        object sender,
        PoisonPuddleDamageConfigSO poisonPuddleDamageConfig)
    {
        CirclePaintRequest request = BuildCircleRequest(
            channel,
            worldPos,
            radiusWorld,
            priority,
            sender,
            poisonPuddleDamageConfig);

        return RequestCircleInternal(request);
    }

    private bool RequestCircleInternal(CirclePaintRequest request)
    {
        bool accepted = false;
        CirclePaintImpact totalImpact = CreateEmptyImpact(request);

        for (int i = 0; i < _registeredSectors.Count; i++)
        {
            SectorPaint sector = _registeredSectors[i];

            if (sector == null)
                continue;

            if (!sector.CanAcceptCircle(request))
                continue;

            RefreshSector(sector);

            CirclePaintImpact sectorImpact = EvaluateCircleImpact(sector, request);
            AddImpact(ref totalImpact, sectorImpact);

            sector.ApplyCircle(request);
            accepted = true;
        }

        if (accepted)
        {
            OnCircleRequestAccepted?.Invoke(request);
            OnCirclePaintImpactAccepted?.Invoke(totalImpact);
            SpawnPaintBubbleParticle(request);
        }
        else
        {
            OnCircleRequestRejected?.Invoke(request);
        }

        return accepted;
    }

    private bool RequestVirusTrailCircleInternal(CirclePaintRequest request)
    {
        if (!TryConsumeTrailCircleBudget())
            return false;

        bool accepted = false;

        for (int i = 0; i < _registeredSectors.Count; i++)
        {
            SectorPaint sector = _registeredSectors[i];

            if (sector == null)
                continue;

            if (!CanSectorAcceptCircle(sector, request, out _))
                continue;

            RefreshSector(sector);
            PaintGpuCircle(sector.virusMask, sector, request.worldPos, request.radiusWorld, true);
            PaintGpuCircle(sector.vaccineMask, sector, request.worldPos, request.radiusWorld, false);

            if (sector.AddVirusTrailCircle(request.worldPos, request.radiusWorld))
                accepted = true;
        }

        return accepted;
    }

    private static bool CanSectorAcceptCircle(
        SectorPaint sector,
        CirclePaintRequest request,
        out Bounds bounds)
    {
        bounds = default;

        if (sector == null || !sector.CanPaintNow())
            return false;

        bounds = sector.GetWorldBounds();

        float radius = request.radiusWorld + sector.BoundsPadding;
        float closestX = Mathf.Clamp(request.worldPos.x, bounds.min.x, bounds.max.x);
        float closestZ = Mathf.Clamp(request.worldPos.z, bounds.min.z, bounds.max.z);
        float dx = request.worldPos.x - closestX;
        float dz = request.worldPos.z - closestZ;

        return dx * dx + dz * dz <= radius * radius;
    }

    private bool TryConsumeTrailCircleBudget()
    {
        if (_maxTrailCirclesPerFrame <= 0)
            return true;

        int frame = Time.frameCount;

        if (_trailCircleBudgetFrame != frame)
        {
            _trailCircleBudgetFrame = frame;
            _trailCircleBudgetCount = 0;
        }

        if (_trailCircleBudgetCount >= _maxTrailCirclesPerFrame)
            return false;

        _trailCircleBudgetCount++;
        return true;
    }

    public void RefreshSector(SectorPaint sector)
    {
        if (sector == null)
            return;

        Bounds bounds = sector.GetWorldBounds();
        bool rebuildTextures = !sector.initialized || NeedRenderTextureRebuild(sector, bounds);

        sector.worldBounds = bounds;

        BuildOrUpdateOverlayObjects(sector);

        if (rebuildTextures)
        {
            BuildOrRebuildMaskRenderTextures(sector);
            BindTexturesToMaterials(sector);
            ClearAllToNeutral(sector);
        }

        sector.initialized = true;
    }

    public void StampCircle(SectorPaint sector, CirclePaintRequest request)
    {
        if (sector == null)
            return;

        RefreshSector(sector);

        switch (request.channel)
        {
            case PaintChannel.Vaccine:
                PaintGpuCircle(sector.vaccineMask, sector, request.worldPos, request.radiusWorld, true);
                PaintGpuCircle(sector.virusMask, sector, request.worldPos, request.radiusWorld, false);
                PaintGpuCircle(sector.poisonPuddleMask, sector, request.worldPos, request.radiusWorld, false);
                break;

            case PaintChannel.Virus:
                PaintGpuCircle(sector.virusMask, sector, request.worldPos, request.radiusWorld, true);
                PaintGpuCircle(sector.vaccineMask, sector, request.worldPos, request.radiusWorld, false);
                break;

            case PaintChannel.PoisonPuddle:
                PaintGpuCircle(sector.virusMask, sector, request.worldPos, request.radiusWorld, true);
                PaintGpuCircle(sector.poisonPuddleMask, sector, request.worldPos, request.radiusWorld, true);
                PaintGpuCircle(sector.vaccineMask, sector, request.worldPos, request.radiusWorld, false);
                break;
        }

        sector.ApplyGameplayCircle(request.channel, request.worldPos, request.radiusWorld, true);
    }

    private void SpawnPaintBubbleParticle(CirclePaintRequest request)
    {
        GameObject prefab = ResolvePaintBubbleParticlePrefab(request.channel);

        if (prefab == null)
            return;

        GameObject instance = Instantiate(
            prefab,
            request.worldPos + _paintBubbleWorldOffset,
            Quaternion.identity,
            _paintVfxRoot);

        Destroy(instance, ResolveParticleLifetime(instance));
    }

    private GameObject ResolvePaintBubbleParticlePrefab(PaintChannel channel)
    {
        switch (channel)
        {
            case PaintChannel.Vaccine:
                return _vaccinePaintBubbleParticlePrefab;

            case PaintChannel.Virus:
                return _virusPaintBubbleParticlePrefab;

            case PaintChannel.PoisonPuddle:
                return _poisonPuddlePaintBubbleParticlePrefab;

            case PaintChannel.Special:
                return _specialPaintBubbleParticlePrefab;

            default:
                return null;
        }
    }

    private float ResolveParticleLifetime(GameObject instance)
    {
        if (instance == null)
            return _paintBubbleFallbackLifetime;

        ParticleSystem[] particles = instance.GetComponentsInChildren<ParticleSystem>(true);
        float lifetime = 0f;

        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem.MainModule main = particles[i].main;
            lifetime = Mathf.Max(lifetime, main.duration + main.startLifetime.constantMax);
        }

        return Mathf.Max(_paintBubbleFallbackLifetime, lifetime);
    }

    public bool TryGetPoisonPuddleAtWorld(Vector3 worldPos, bool requireOpened = true)
    {
        for (int i = 0; i < _registeredSectors.Count; i++)
        {
            SectorPaint sector = _registeredSectors[i];

            if (sector == null)
                continue;

            if (requireOpened && !sector.CanQueryPaintNow())
                continue;

            if (!sector.ContainsPoint(worldPos))
                continue;

            return sector.HasPaintAtWorld(PaintChannel.PoisonPuddle, worldPos);
        }

        return false;
    }

    public bool TryGetPoisonPuddleAtWorld(
        Vector3 worldPos,
        out PoisonPuddleDamageConfigSO poisonPuddleDamageConfig,
        bool requireOpened = true)
    {
        poisonPuddleDamageConfig = null;

        for (int i = 0; i < _registeredSectors.Count; i++)
        {
            SectorPaint sector = _registeredSectors[i];

            if (sector == null)
                continue;

            if (requireOpened && !sector.CanQueryPaintNow())
                continue;

            if (!sector.ContainsPoint(worldPos))
                continue;

            if (!sector.HasPaintAtWorld(PaintChannel.PoisonPuddle, worldPos))
                return false;

            poisonPuddleDamageConfig = ResolvePoisonPuddleDamageConfig(sector, worldPos);
            return true;
        }

        return false;
    }

    private PoisonPuddleDamageConfigSO ResolvePoisonPuddleDamageConfig(
        SectorPaint sector,
        Vector3 worldPos)
    {
        IReadOnlyList<SectorPaint.StoredCirclePaint> paints =
            sector.GetStoredPaints(PaintChannel.PoisonPuddle);

        PoisonPuddleDamageConfigSO bestConfig = null;
        int bestPriority = int.MinValue;
        float bestAppliedTime = float.MinValue;

        for (int i = 0; i < paints.Count; i++)
        {
            SectorPaint.StoredCirclePaint paint = paints[i];

            if (paint.channel != PaintChannel.PoisonPuddle)
                continue;

            Vector3 delta = worldPos - paint.worldPos;
            delta.y = 0f;

            if (delta.sqrMagnitude > paint.radiusWorld * paint.radiusWorld)
                continue;

            bool better =
                paint.priority > bestPriority ||
                paint.priority == bestPriority && paint.appliedTime > bestAppliedTime;

            if (!better)
                continue;

            bestConfig = paint.poisonPuddleDamageConfig;
            bestPriority = paint.priority;
            bestAppliedTime = paint.appliedTime;
        }

        return bestConfig;
    }

    public bool TryGetStateAtWorld(
        Vector3 worldPos,
        out PaintSurfaceState  state,
        bool requireOpened = true)
    {
        state = PaintSurfaceState .Neutral;

        for (int i = 0; i < _registeredSectors.Count; i++)
        {
            SectorPaint sector = _registeredSectors[i];

            if (sector == null)
                continue;

            if (requireOpened && !sector.CanQueryPaintNow())
                continue;

            if (!sector.ContainsPoint(worldPos))
                continue;

            state = GetStateAtWorld(sector, worldPos);
            return true;
        }

        return false;
    }

    public PaintSurfaceState  GetStateAtWorld(SectorPaint sector, Vector3 worldPos)
    {
        if (sector == null)
            return PaintSurfaceState .Neutral;

        return sector.GetPaintStateAtWorld(worldPos);
    }

    public void ClearAllToNeutral(SectorPaint sector)
    {
        if (sector == null)
            return;

        sector.ClearAllPaintCoverage();
        ClearRenderTexture(sector.vaccineMask, 0f);
        ClearRenderTexture(sector.virusMask, 0f);
        ClearRenderTexture(sector.poisonPuddleMask, 0f);
    }

    public bool FillSector(SectorPaint sector, PaintChannel channel, bool clearOtherChannel = true)
    {
        if (sector == null)
            return false;

        RefreshSector(sector);
        sector.FillGameplay(channel, clearOtherChannel);

        switch (channel)
        {
            case PaintChannel.Vaccine:
                ClearRenderTexture(sector.vaccineMask, 1f);

                if (clearOtherChannel)
                {
                    ClearRenderTexture(sector.virusMask, 0f);
                    ClearRenderTexture(sector.poisonPuddleMask, 0f);
                }

                break;

            case PaintChannel.Virus:
                ClearRenderTexture(sector.virusMask, 1f);

                if (clearOtherChannel)
                    ClearRenderTexture(sector.vaccineMask, 0f);

                break;

            case PaintChannel.PoisonPuddle:
                ClearRenderTexture(sector.virusMask, 1f);
                ClearRenderTexture(sector.poisonPuddleMask, 1f);

                if (clearOtherChannel)
                    ClearRenderTexture(sector.vaccineMask, 0f);

                break;
        }

        sector.ClearAllStoredPaint();
        return true;
    }

    private CirclePaintRequest BuildCircleRequest(
        PaintChannel channel,
        Vector3 worldPos,
        float radiusWorld,
        int priority,
        object sender,
        PoisonPuddleDamageConfigSO poisonPuddleDamageConfig)
    {
        return new CirclePaintRequest
        {
            channel = channel,
            worldPos = worldPos,
            radiusWorld = Mathf.Max(0.001f, radiusWorld),
            priority = priority,
            sender = sender,
            poisonPuddleDamageConfig = poisonPuddleDamageConfig
        };
    }

    private bool NeedRenderTextureRebuild(SectorPaint sector, Bounds newBounds)
    {
        int newWidth = Mathf.Max(1, Mathf.CeilToInt(newBounds.size.x * Mathf.Max(1, pixelsPerUnit)));
        int newHeight = Mathf.Max(1, Mathf.CeilToInt(newBounds.size.z * Mathf.Max(1, pixelsPerUnit)));

        return newWidth != sector.textureWidth || newHeight != sector.textureHeight;
    }

    private void BuildOrUpdateOverlayObjects(SectorPaint sector)
    {
        CreateOverlayIfNeeded(
            sector,
            "VaccineOverlay",
            out sector.vaccineRenderer,
            out sector.vaccineFilter,
            ref sector.vaccineMaterialInstance,
            vaccineBaseMaterial);

        CreateOverlayIfNeeded(
            sector,
            "VirusOverlay",
            out sector.virusRenderer,
            out sector.virusFilter,
            ref sector.virusMaterialInstance,
            virusBaseMaterial);

        CreateOverlayIfNeeded(
            sector,
            "PoisonPuddleOverlay",
            out sector.poisonPuddleRenderer,
            out sector.poisonPuddleFilter,
            ref sector.poisonPuddleMaterialInstance,
            poisonPuddleBaseMaterial);

        UpdateOverlayTransform(sector.vaccineRenderer.transform, sector.worldBounds, yOffset);
        UpdateOverlayTransform(sector.virusRenderer.transform, sector.worldBounds, yOffset + 0.001f);
        UpdateOverlayTransform(sector.poisonPuddleRenderer.transform, sector.worldBounds, yOffset + 0.002f);

        if (sector.vaccineMaterialInstance != null)
            sector.vaccineMaterialInstance.renderQueue = Mathf.Max(sector.vaccineMaterialInstance.renderQueue, 3600);

        if (sector.virusMaterialInstance != null)
            sector.virusMaterialInstance.renderQueue = Mathf.Max(sector.virusMaterialInstance.renderQueue, virusAboveVaccine ? 3700 : 3599);

        if (sector.poisonPuddleMaterialInstance != null)
            sector.poisonPuddleMaterialInstance.renderQueue = Mathf.Max(sector.poisonPuddleMaterialInstance.renderQueue, 3710);
    }

    private void BuildOrRebuildMaskRenderTextures(SectorPaint sector)
    {
        sector.textureWidth = Mathf.Max(1, Mathf.CeilToInt(sector.worldBounds.size.x * Mathf.Max(1, pixelsPerUnit)));
        sector.textureHeight = Mathf.Max(1, Mathf.CeilToInt(sector.worldBounds.size.z * Mathf.Max(1, pixelsPerUnit)));

        ReleaseObject(sector.vaccineMask);
        ReleaseObject(sector.virusMask);
        ReleaseObject(sector.poisonPuddleMask);

        sector.vaccineMask = CreateMaskRenderTexture(sector.textureWidth, sector.textureHeight, "VaccineMaskRT");
        sector.virusMask = CreateMaskRenderTexture(sector.textureWidth, sector.textureHeight, "VirusMaskRT");
        sector.poisonPuddleMask = CreateMaskRenderTexture(sector.textureWidth, sector.textureHeight, "PoisonPuddleMaskRT");

        ClearRenderTexture(sector.vaccineMask, 0f);
        ClearRenderTexture(sector.virusMask, 0f);
        ClearRenderTexture(sector.poisonPuddleMask, 0f);
    }

    private void BindTexturesToMaterials(SectorPaint sector)
    {
        if (sector.vaccineMaterialInstance != null && sector.vaccineMaterialInstance.HasProperty(maskTextureProperty))
            sector.vaccineMaterialInstance.SetTexture(maskTextureProperty, sector.vaccineMask);

        if (sector.virusMaterialInstance != null && sector.virusMaterialInstance.HasProperty(maskTextureProperty))
            sector.virusMaterialInstance.SetTexture(maskTextureProperty, sector.virusMask);

        if (sector.poisonPuddleMaterialInstance != null && sector.poisonPuddleMaterialInstance.HasProperty(maskTextureProperty))
            sector.poisonPuddleMaterialInstance.SetTexture(maskTextureProperty, sector.poisonPuddleMask);
    }

    private void PaintGpuCircle(
        RenderTexture target,
        SectorPaint sector,
        Vector3 centerWorld,
        float radiusWorld,
        bool add)
    {
        if (target == null || sector == null || !EnsurePaintStampMaterial())
            return;

        sector.TryWorldToMaskUV(centerWorld, out Vector2 uv);

        Bounds bounds = sector.worldBounds;
        float radiusU = radiusWorld / Mathf.Max(0.0001f, bounds.size.x);
        float radiusV = radiusWorld / Mathf.Max(0.0001f, bounds.size.z);

        _paintStampMaterial.SetVector(StampShaderId, new Vector4(uv.x, uv.y, radiusU, radiusV));
        _paintStampMaterial.SetFloat(StampSoftnessShaderId, _paintStampEdgeSoftness);

        Graphics.Blit(Texture2D.whiteTexture, target, _paintStampMaterial, add ? 0 : 1);
    }

    private bool EnsurePaintStampMaterial()
    {
        if (_paintStampMaterial != null)
            return true;

        Shader shader = Shader.Find("Hidden/VSplatter/PaintMaskStamp");

        if (shader != null)
        {
            _paintStampMaterial = new Material(shader)
            {
                name = "Runtime_PaintMaskStamp"
            };
            return true;
        }

        if (!_missingPaintStampMaterialLogged)
        {
            _missingPaintStampMaterialLogged = true;
            Debug.LogError("[MaskRenderManager] Paint stamp shader not found: Hidden/VSplatter/PaintMaskStamp", this);
        }

        return false;
    }

    private RenderTexture CreateMaskRenderTexture(int width, int height, string textureName)
    {
        RenderTexture texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
        {
            name = textureName,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            useMipMap = false,
            autoGenerateMips = false
        };

        texture.Create();
        return texture;
    }

    private void ClearRenderTexture(RenderTexture texture, float value)
    {
        if (texture == null)
            return;

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = texture;
        GL.Clear(false, true, new Color(value, value, value, value));
        RenderTexture.active = previous;
    }

    private void CreateOverlayIfNeeded(
        SectorPaint sector,
        string childName,
        out MeshRenderer mr,
        out MeshFilter mf,
        ref Material materialInstance,
        Material baseMaterial)
    {
        Transform child = sector.transform.Find(childName);

        if (child == null)
        {
            GameObject go = new GameObject(childName);
            go.transform.SetParent(sector.transform, false);

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

    private Mesh CreateUnitQuadXZMesh()
    {
        Mesh mesh = new Mesh
        {
            name = "MaskOverlayQuad_XZ",
            vertices = new Vector3[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, 0.5f),
                new Vector3(-0.5f, 0f, 0.5f)
            },
            uv = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            },
            triangles = new[] { 0, 1, 2, 0, 2, 3 }
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void UpdateOverlayTransform(Transform target, Bounds bounds, float heightOffset)
    {
        target.position = new Vector3(bounds.center.x, bounds.center.y + heightOffset, bounds.center.z);
        target.rotation = Quaternion.identity;
        target.localScale = new Vector3(bounds.size.x, 1f, bounds.size.z);
    }

    private void ReleaseObject(UnityEngine.Object obj)
    {
        if (obj == null)
            return;

        if (obj is RenderTexture renderTexture && renderTexture.IsCreated())
            renderTexture.Release();

        if (Application.isPlaying)
            Destroy(obj);
        else
            DestroyImmediate(obj);
    }

    private CirclePaintImpact EvaluateCircleImpact(SectorPaint sector, CirclePaintRequest request)
    {
        CirclePaintImpact impact = CreateEmptyImpact(request);

        if (sector == null)
            return impact;

        if (!sector.TryGetPaintCellRange(
                request.worldPos,
                request.radiusWorld,
                out int minX,
                out int maxX,
                out int minY,
                out int maxY))
        {
            return impact;
        }

        float cellWorldWidth = sector.worldBounds.size.x / Mathf.Max(1, sector.PaintGridWidth);
        float cellWorldHeight = sector.worldBounds.size.z / Mathf.Max(1, sector.PaintGridHeight);
        float cellArea = cellWorldWidth * cellWorldHeight;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float paintCoverage = sector.EstimatePaintCoverage(request.worldPos, request.radiusWorld, x, y);

                if (paintCoverage <= 0f)
                    continue;

                float vaccine = sector.GetCoverageAtCell(PaintChannel.Vaccine, x, y);
                float virus = sector.GetCoverageAtCell(PaintChannel.Virus, x, y);
                float area = paintCoverage * cellArea;

                impact.totalCells++;
                impact.totalArea += area;

                if (request.channel == PaintChannel.Vaccine)
                {
                    float overwritten = Mathf.Min(paintCoverage, virus);
                    float already = Mathf.Min(paintCoverage, vaccine);
                    float neutral = Mathf.Max(0f, paintCoverage - Mathf.Max(vaccine, virus));

                    if (overwritten > 0f)
                    {
                        impact.overwrittenVirusCells++;
                        impact.overwrittenVirusArea += overwritten * cellArea;
                    }
                    else if (neutral > 0f)
                    {
                        impact.neutralCells++;
                        impact.neutralArea += neutral * cellArea;
                    }
                    else if (already > 0f)
                    {
                        impact.alreadyVaccineCells++;
                        impact.alreadyVaccineArea += already * cellArea;
                    }
                }
                else if (request.channel == PaintChannel.Virus ||
                    request.channel == PaintChannel.PoisonPuddle)
                {
                    float overwritten = Mathf.Min(paintCoverage, vaccine);
                    float neutral = Mathf.Max(0f, paintCoverage - Mathf.Max(vaccine, virus));

                    if (overwritten > 0f)
                    {
                        impact.overwrittenVirusCells++;
                        impact.overwrittenVirusArea += overwritten * cellArea;
                    }
                    else if (neutral > 0f)
                    {
                        impact.neutralCells++;
                        impact.neutralArea += neutral * cellArea;
                    }
                    else
                    {
                        impact.alreadyVirusCells++;
                    }
                }
            }
        }

        return impact;
    }

    private CirclePaintImpact CreateEmptyImpact(CirclePaintRequest request)
    {
        return new CirclePaintImpact
        {
            channel = request.channel,
            worldPos = request.worldPos,
            radiusWorld = request.radiusWorld,
            sender = request.sender
        };
    }

    private void AddImpact(ref CirclePaintImpact total, CirclePaintImpact add)
    {
        total.totalCells += add.totalCells;
        total.neutralCells += add.neutralCells;
        total.overwrittenVirusCells += add.overwrittenVirusCells;
        total.alreadyVaccineCells += add.alreadyVaccineCells;
        total.alreadyVirusCells += add.alreadyVirusCells;

        total.neutralArea += add.neutralArea;
        total.overwrittenVirusArea += add.overwrittenVirusArea;
        total.alreadyVaccineArea += add.alreadyVaccineArea;
        total.totalArea += add.totalArea;
    }
}
