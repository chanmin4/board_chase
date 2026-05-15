using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class MaskRenderManager : MonoBehaviour
{
    public enum PaintChannel
    {
        Vaccine,
        Virus,
        PoisonPuddle
    }

    public enum PaintState
    {
        Neutral,
        Vaccine,
        Virus
    }

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

        public int totalPixels;
        public int neutralPixels;
        public int overwrittenVirusPixels;
        public int alreadyVaccinePixels;
        public int alreadyVirusPixels;

        public float neutralArea;
        public float overwrittenVirusArea;
        public float alreadyVaccineArea;
        public float totalArea;

        public float ValidArea => neutralArea + overwrittenVirusArea;
        public int ValidPixels => neutralPixels + overwrittenVirusPixels;
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

    private readonly List<SectorPaint> _registeredSectors = new();

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
    private void LateUpdate()
    {
        for (int i = 0; i < _registeredSectors.Count; i++)
        {
            SectorPaint sector = _registeredSectors[i];
            if (sector == null)
                continue;

            ApplyDirtyMasks(sector);
        }
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
        channel,worldPos,radiusWorld,0,sender,null);
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
    public bool RequestCapsuleTrail(
    PaintChannel channel,
    Vector3 fromWorld,
    Vector3 toWorld,
    float radiusWorld,
    int priority = 0,
    object sender = null)
    {
        Vector3 from = fromWorld;
        Vector3 to = toWorld;

        from.y = 0f;
        to.y = 0f;

        float distance = Vector3.Distance(from, to);

        if (distance <= 0.001f)
            return RequestCircle(channel, toWorld, radiusWorld, priority, sender);

        float spacing = Mathf.Max(radiusWorld * 0.75f, 0.05f);
        int steps = Mathf.Max(1, Mathf.CeilToInt(distance / spacing));

        bool acceptedAny = false;

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector3 position = Vector3.Lerp(fromWorld, toWorld, t);

            if (RequestCircle(channel, position, radiusWorld, priority, sender))
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
        }
        else
        {
            OnCircleRequestRejected?.Invoke(request);
        }

        return accepted;
    }

    public void RefreshSector(SectorPaint sector)
    {
        if (sector == null)
            return;

        Bounds bounds = sector.GetWorldBounds();
        bool rebuildTextures = !sector.initialized || NeedTextureRebuild(sector, bounds);

        sector.worldBounds = bounds;

        BuildOrUpdateOverlayObjects(sector);

        if (rebuildTextures)
        {
            BuildOrRebuildMaskTextures(sector);
            BindTexturesToMaterials(sector);
            ClearAllToNeutral(sector);
            ApplyDirtyMasks(sector);
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
                StampVaccineCircle(sector, request.worldPos, request.radiusWorld, true);
                break;

            case PaintChannel.Virus:
                StampVirusCircle(sector, request.worldPos, request.radiusWorld, true);
                break;
            case PaintChannel.PoisonPuddle:
                StampPoisonPuddleCircle(sector, request.worldPos, request.radiusWorld, true);
                break;

        }
    }
    public bool TryGetPoisonPuddleAtWorld(Vector3 worldPos, bool requireOpened = true)
    {
        for (int i = 0; i < _registeredSectors.Count; i++)
        {
            SectorPaint sector = _registeredSectors[i];

            if (sector == null)
                continue;

            if (requireOpened && !sector.CanPaintNow())
                continue;

            if (!sector.ContainsPoint(worldPos))
                continue;

            RefreshSector(sector);

            if (!TryWorldToPixel(sector, worldPos, out int px, out int py))
                return false;

            int index = py * sector.textureWidth + px;

            return sector.poisonPuddleBuffer != null &&
                sector.poisonPuddleBuffer[index].a > 0;
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

            if (requireOpened && !sector.CanPaintNow())
                continue;

            if (!sector.ContainsPoint(worldPos))
                continue;

            RefreshSector(sector);

            if (!TryWorldToPixel(sector, worldPos, out int px, out int py))
                return false;

            int index = py * sector.textureWidth + px;

            bool hasPoisonPuddle =
                sector.poisonPuddleBuffer != null &&
                sector.poisonPuddleBuffer[index].a > 0;

            if (!hasPoisonPuddle)
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
        out PaintState state,
        bool requireOpened = true)
    {
        state = PaintState.Neutral;

        for (int i = 0; i < _registeredSectors.Count; i++)
        {
            SectorPaint sector = _registeredSectors[i];

            if (sector == null)
                continue;

            if (requireOpened && !sector.CanPaintNow())
                continue;

            if (!sector.ContainsPoint(worldPos))
                continue;

            RefreshSector(sector);

            state = GetStateAtWorld(sector, worldPos);
            return true;
        }

        return false;
    }
    public PaintState GetStateAtWorld(SectorPaint sector, Vector3 worldPos)
    {
        if (sector == null || !sector.initialized)
            return PaintState.Neutral;

        if (!TryWorldToPixel(sector, worldPos, out int px, out int py))
            return PaintState.Neutral;

        int index = py * sector.textureWidth + px;

        bool vaccine = sector.vaccineBuffer != null && sector.vaccineBuffer[index].a > 0;
        bool virus = sector.virusBuffer != null && sector.virusBuffer[index].a > 0;

        if (vaccine && !virus) return PaintState.Vaccine;
        if (!vaccine && virus) return PaintState.Virus;
        return PaintState.Neutral;
    }

    public void ClearAllToNeutral(SectorPaint sector)
    {
        if (sector == null)
            return;

        ClearBuffer(sector.vaccineBuffer);
        ClearBuffer(sector.virusBuffer);
        ClearBuffer(sector.poisonPuddleBuffer);

        sector.vaccineDirty = true;
        sector.virusDirty = true;
        sector.poisonPuddleDirty = true;
    }
    public bool FillSector(SectorPaint sector, PaintChannel channel, bool clearOtherChannel = true)
    {
        if (sector == null)
            return false;

        RefreshSector(sector);

        if (sector.vaccineBuffer == null || sector.virusBuffer == null)
            return false;

        switch (channel)
        {
            case PaintChannel.Vaccine:
                FillBufferAlpha(sector.vaccineBuffer, 255);
                sector.vaccineDirty = true;

                if (clearOtherChannel)
                {
                    FillBufferAlpha(sector.virusBuffer, 0);
                    sector.virusDirty = true;
                }

                break;

            case PaintChannel.Virus:
                FillBufferAlpha(sector.virusBuffer, 255);
                sector.virusDirty = true;

                if (clearOtherChannel)
                {
                    FillBufferAlpha(sector.vaccineBuffer, 0);
                    sector.vaccineDirty = true;
                }

                break;
        }

        sector.ClearAllStoredPaint();
        return true;
    }

    private void FillBufferAlpha(Color32[] buffer, byte alpha)
    {
        if (buffer == null)
            return;

        for (int i = 0; i < buffer.Length; i++)
        {
            Color32 color = buffer[i];
            color.a = alpha;
            buffer[i] = color;
        }
    }


   private CirclePaintRequest BuildCircleRequest(
    PaintChannel channel,
    Vector3 worldPos,
    float radiusWorld,
    int priority,
    object sender,
    PoisonPuddleDamageConfigSO poisonPuddleDamageConfig)
    {
        CirclePaintRequest request = new CirclePaintRequest
        {
            channel = channel,
            worldPos = worldPos,
            radiusWorld = Mathf.Max(0.001f, radiusWorld),
            priority = priority,
            sender = sender,
            poisonPuddleDamageConfig = poisonPuddleDamageConfig
        };

        return request;
    }

    private bool NeedTextureRebuild(SectorPaint sector, Bounds newBounds)
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

    private void BuildOrRebuildMaskTextures(SectorPaint sector)
    {
        sector.textureWidth = Mathf.Max(1, Mathf.CeilToInt(sector.worldBounds.size.x * Mathf.Max(1, pixelsPerUnit)));
        sector.textureHeight = Mathf.Max(1, Mathf.CeilToInt(sector.worldBounds.size.z * Mathf.Max(1, pixelsPerUnit)));

        ReleaseObject(sector.vaccineMask);
        ReleaseObject(sector.virusMask);
        ReleaseObject(sector.poisonPuddleMask);
        sector.vaccineMask = CreateMaskTexture(sector.textureWidth, sector.textureHeight, "VaccineMask");
        sector.virusMask = CreateMaskTexture(sector.textureWidth, sector.textureHeight, "VirusMask");
        sector.poisonPuddleMask = CreateMaskTexture(sector.textureWidth, sector.textureHeight, "PoisonPuddleMask");
        sector.vaccineBuffer = CreateClearBuffer(sector.textureWidth, sector.textureHeight);
        sector.virusBuffer = CreateClearBuffer(sector.textureWidth, sector.textureHeight);
        sector.poisonPuddleBuffer = CreateClearBuffer(sector.textureWidth, sector.textureHeight);
        sector.vaccineDirty = true;
        sector.virusDirty = true;
        sector.poisonPuddleDirty = true;
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

    private void StampVaccineCircle(SectorPaint sector, Vector3 centerWorld, float radiusWorld, bool overwriteOther = true)
    {
        StampCircleAlpha(sector, sector.vaccineBuffer, centerWorld, radiusWorld, 255, markVaccineDirty: true);

        if (overwriteOther)
        {
            StampCircleAlpha(sector, sector.virusBuffer, centerWorld, radiusWorld, 0, markVirusDirty: true);
            StampCircleAlpha(sector, sector.poisonPuddleBuffer, centerWorld, radiusWorld, 0, markPoisonPuddleDirty: true);
        }
    }

    private void StampVirusCircle(SectorPaint sector, Vector3 centerWorld, float radiusWorld, bool overwriteOther = true)
    {
        StampCircleAlpha(sector, sector.virusBuffer, centerWorld, radiusWorld, 255, markVirusDirty: true);

        if (overwriteOther)
            StampCircleAlpha(sector, sector.vaccineBuffer, centerWorld, radiusWorld, 0, markVaccineDirty: true);
    }
    private void StampPoisonPuddleCircle(SectorPaint sector, Vector3 centerWorld, float radiusWorld, bool overwriteOther = true)
    {
        StampVirusCircle(sector, centerWorld, radiusWorld, overwriteOther);
        StampCircleAlpha(sector, sector.poisonPuddleBuffer, centerWorld, radiusWorld, 255, markPoisonPuddleDirty: true);
    }

    private void StampCircleAlpha(
        SectorPaint sector,
        Color32[] buffer,
        Vector3 centerWorld,
        float radiusWorld,
        byte alpha,
        bool markVaccineDirty = false,
        bool markVirusDirty = false,
        bool markPoisonPuddleDirty = false
        )
    {
        if (sector == null || buffer == null)
            return;

        if (!TryWorldToPixel(sector, centerWorld, out int cx, out int cy))
            return;

        float pixelsPerMeterX = sector.textureWidth / Mathf.Max(0.0001f, sector.worldBounds.size.x);
        float pixelsPerMeterY = sector.textureHeight / Mathf.Max(0.0001f, sector.worldBounds.size.z);
        float pixelsPerMeter = Mathf.Min(pixelsPerMeterX, pixelsPerMeterY);

        int radiusPx = Mathf.Max(1, Mathf.RoundToInt(radiusWorld * pixelsPerMeter));

        int minX = Mathf.Max(0, cx - radiusPx);
        int maxX = Mathf.Min(sector.textureWidth - 1, cx + radiusPx);
        int minY = Mathf.Max(0, cy - radiusPx);
        int maxY = Mathf.Min(sector.textureHeight - 1, cy + radiusPx);

        float rr = (radiusPx + 0.5f) * (radiusPx + 0.5f);

        for (int y = minY; y <= maxY; y++)
        {
            int dy = y - cy;
            int row = y * sector.textureWidth;

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

        if (markVaccineDirty) sector.vaccineDirty = true;
        if (markVirusDirty) sector.virusDirty = true;
        if (markPoisonPuddleDirty) sector.poisonPuddleDirty = true;
    }

    private void ApplyDirtyMasks(SectorPaint sector)
    {
        if (sector == null)
            return;

        if (sector.vaccineDirty && sector.vaccineMask != null && sector.vaccineBuffer != null)
        {
            sector.vaccineMask.SetPixels32(sector.vaccineBuffer);
            sector.vaccineMask.Apply(false, false);
            sector.vaccineDirty = false;
        }

        if (sector.virusDirty && sector.virusMask != null && sector.virusBuffer != null)
        {
            sector.virusMask.SetPixels32(sector.virusBuffer);
            sector.virusMask.Apply(false, false);
            sector.virusDirty = false;
        }
        if (sector.poisonPuddleDirty && sector.poisonPuddleMask != null && sector.poisonPuddleBuffer != null)
        {
            sector.poisonPuddleMask.SetPixels32(sector.poisonPuddleBuffer);
            sector.poisonPuddleMask.Apply(false, false);
            sector.poisonPuddleDirty = false;
        }
    }

    private bool TryWorldToPixel(SectorPaint sector, Vector3 worldPos, out int px, out int py)
    {
        float x01 = Mathf.InverseLerp(sector.worldBounds.min.x, sector.worldBounds.max.x, worldPos.x);
        float y01 = Mathf.InverseLerp(sector.worldBounds.min.z, sector.worldBounds.max.z, worldPos.z);

        bool inside = x01 >= 0f && x01 <= 1f && y01 >= 0f && y01 <= 1f;

        px = Mathf.RoundToInt(x01 * (sector.textureWidth - 1));
        py = Mathf.RoundToInt(y01 * (sector.textureHeight - 1));

        px = Mathf.Clamp(px, 0, sector.textureWidth - 1);
        py = Mathf.Clamp(py, 0, sector.textureHeight - 1);

        return inside;
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

    private void UpdateOverlayTransform(Transform target, Bounds bounds, float heightOffset)
    {
        target.position = new Vector3(bounds.center.x, bounds.center.y + heightOffset, bounds.center.z);
        target.rotation = Quaternion.identity;
        target.localScale = new Vector3(bounds.size.x, 1f, bounds.size.z);
    }

    private Texture2D CreateMaskTexture(int width, int height, string textureName)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.Alpha8, false, true);
        tex.name = textureName;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }

    private Color32[] CreateClearBuffer(int width, int height)
    {
        Color32[] buffer = new Color32[width * height];
        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = new Color32(0, 0, 0, 0);
        return buffer;
    }

    private void ClearBuffer(Color32[] buffer)
    {
        if (buffer == null)
            return;

        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = new Color32(0, 0, 0, 0);
    }

    private void ReleaseObject(UnityEngine.Object obj)
    {
        if (obj == null)
            return;

        if (Application.isPlaying)
            Destroy(obj);
        else
            DestroyImmediate(obj);
    }
    private CirclePaintImpact EvaluateCircleImpact(SectorPaint sector, CirclePaintRequest request)
    {
        CirclePaintImpact impact = CreateEmptyImpact(request);

        if (sector == null || !sector.initialized)
            return impact;

        if (!TryWorldToPixel(sector, request.worldPos, out int cx, out int cy))
            return impact;

        float pixelsPerMeterX = sector.textureWidth / Mathf.Max(0.0001f, sector.worldBounds.size.x);
        float pixelsPerMeterY = sector.textureHeight / Mathf.Max(0.0001f, sector.worldBounds.size.z);
        float pixelsPerMeter = Mathf.Min(pixelsPerMeterX, pixelsPerMeterY);

        int radiusPx = Mathf.Max(1, Mathf.RoundToInt(request.radiusWorld * pixelsPerMeter));

        int minX = Mathf.Max(0, cx - radiusPx);
        int maxX = Mathf.Min(sector.textureWidth - 1, cx + radiusPx);
        int minY = Mathf.Max(0, cy - radiusPx);
        int maxY = Mathf.Min(sector.textureHeight - 1, cy + radiusPx);

        float rr = (radiusPx + 0.5f) * (radiusPx + 0.5f);

        float pixelWorldWidth = sector.worldBounds.size.x / Mathf.Max(1, sector.textureWidth);
        float pixelWorldHeight = sector.worldBounds.size.z / Mathf.Max(1, sector.textureHeight);
        float pixelArea = pixelWorldWidth * pixelWorldHeight;

        for (int y = minY; y <= maxY; y++)
        {
            int dy = y - cy;
            int row = y * sector.textureWidth;

            for (int x = minX; x <= maxX; x++)
            {
                int dx = x - cx;

                if ((dx * dx) + (dy * dy) > rr)
                    continue;

                int index = row + x;

                bool hasVaccine =
                    sector.vaccineBuffer != null &&
                    sector.vaccineBuffer[index].a > 0;

                bool hasVirus =
                    sector.virusBuffer != null &&
                    sector.virusBuffer[index].a > 0;

                impact.totalPixels++;
                impact.totalArea += pixelArea;

                if (request.channel == PaintChannel.Vaccine)
                {
                    if (hasVirus)
                    {
                        impact.overwrittenVirusPixels++;
                        impact.overwrittenVirusArea += pixelArea;
                    }
                    else if (!hasVaccine)
                    {
                        impact.neutralPixels++;
                        impact.neutralArea += pixelArea;
                    }
                    else
                    {
                        impact.alreadyVaccinePixels++;
                        impact.alreadyVaccineArea += pixelArea;
                    }
                }
                else if (request.channel == PaintChannel.Virus ||
                    request.channel == PaintChannel.PoisonPuddle)
                {
                    if (hasVaccine)
                    {
                        impact.overwrittenVirusPixels++;
                        impact.overwrittenVirusArea += pixelArea;
                    }
                    else if (!hasVirus)
                    {
                        impact.neutralPixels++;
                        impact.neutralArea += pixelArea;
                    }
                    else
                    {
                        impact.alreadyVirusPixels++;
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
        total.totalPixels += add.totalPixels;
        total.neutralPixels += add.neutralPixels;
        total.overwrittenVirusPixels += add.overwrittenVirusPixels;
        total.alreadyVaccinePixels += add.alreadyVaccinePixels;
        total.alreadyVirusPixels += add.alreadyVirusPixels;

        total.neutralArea += add.neutralArea;
        total.overwrittenVirusArea += add.overwrittenVirusArea;
        total.alreadyVaccineArea += add.alreadyVaccineArea;
        total.totalArea += add.totalArea;
    }

}