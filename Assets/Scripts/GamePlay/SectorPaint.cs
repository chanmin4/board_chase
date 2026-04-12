using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SectorPaint : MonoBehaviour
{
    [Serializable]
    public struct StoredCirclePaint
    {
        public MaskRenderManager.PaintChannel channel;
        public Vector3 worldPos;
        public float radiusWorld;
        public int priority;
        public float appliedTime;
        public object sender;
    }

    [Header("Refs")]
    [SerializeField] private SectorRuntime runtime;
    [Header("AutoRefs Don't Touch")]
    [SerializeField]private MaskRenderManager MaskRenderManager;


    [Header("Options")]
    [SerializeField] private bool allowPaintWhenClosed = false;
    [SerializeField] private float boundsPadding = 0.1f;

    [HideInInspector] public Bounds worldBounds;
    [HideInInspector] public bool initialized;

    [HideInInspector] public Texture2D vaccineMask;
    [HideInInspector] public Texture2D virusMask;

    [HideInInspector] public Color32[] vaccineBuffer;
    [HideInInspector] public Color32[] virusBuffer;

    [HideInInspector] public bool vaccineDirty;
    [HideInInspector] public bool virusDirty;

    [HideInInspector] public Material vaccineMaterialInstance;
    [HideInInspector] public Material virusMaterialInstance;

    [HideInInspector] public MeshRenderer vaccineRenderer;
    [HideInInspector] public MeshRenderer virusRenderer;

    [HideInInspector] public MeshFilter vaccineFilter;
    [HideInInspector] public MeshFilter virusFilter;

    [HideInInspector] public int textureWidth;
    [HideInInspector] public int textureHeight;

    private readonly List<StoredCirclePaint> _playerPaints = new();
    private readonly List<StoredCirclePaint> _enemyPaints = new();

    public event Action<SectorPaint, StoredCirclePaint> OnCircleApplied;

    public int PlayerPaintCount => _playerPaints.Count;
    public int EnemyPaintCount => _enemyPaints.Count;
    public SectorRuntime Runtime => runtime;
    public bool AllowPaintWhenClosed => allowPaintWhenClosed;
    public float BoundsPadding => boundsPadding;

    private void Reset()
    {
        if (!runtime)
            runtime = GetComponent<SectorRuntime>();

        if (!MaskRenderManager)
            MaskRenderManager = FindAnyObjectByType<MaskRenderManager>();
    }

    private void Awake()
    {
        if (!runtime)
            runtime = GetComponent<SectorRuntime>();

        if (!MaskRenderManager)
            MaskRenderManager = FindAnyObjectByType<MaskRenderManager>();
    }

    private void OnEnable()
    {
        if (MaskRenderManager != null)
            MaskRenderManager.RegisterSector(this);
    }

    private void OnDisable()
    {
        if (MaskRenderManager != null)
            MaskRenderManager.UnregisterSector(this);
    }

    public Bounds GetWorldBounds()
    {
        if (runtime != null)
            return runtime.GetWorldBounds();

        return new Bounds(transform.position, Vector3.one);
    }

    public bool CanPaintNow()
    {
        if (allowPaintWhenClosed)
            return true;

        if (runtime == null)
            return true;

        return runtime.isOpened;
    }

    public bool ContainsPoint(Vector3 worldPos)
    {
        Bounds bounds = GetWorldBounds();

        return worldPos.x >= bounds.min.x &&
               worldPos.x <= bounds.max.x &&
               worldPos.z >= bounds.min.z &&
               worldPos.z <= bounds.max.z;
    }

    public bool IntersectsCircle(Vector3 worldPos, float radiusWorld)
    {
        Bounds bounds = GetWorldBounds();

        float closestX = Mathf.Clamp(worldPos.x, bounds.min.x, bounds.max.x);
        float closestZ = Mathf.Clamp(worldPos.z, bounds.min.z, bounds.max.z);

        float dx = worldPos.x - closestX;
        float dz = worldPos.z - closestZ;

        return (dx * dx) + (dz * dz) <= radiusWorld * radiusWorld;
    }

    public bool CanAcceptCircle(MaskRenderManager.CirclePaintRequest request)
    {
        if (!CanPaintNow())
            return false;

        return IntersectsCircle(request.worldPos, request.radiusWorld + boundsPadding);
    }

    public void ApplyCircle(MaskRenderManager.CirclePaintRequest request)
    {
        if (MaskRenderManager == null)
            return;

        StoredCirclePaint stored = new StoredCirclePaint
        {
            channel = request.channel,
            worldPos = request.worldPos,
            radiusWorld = request.radiusWorld,
            priority = request.priority,
            appliedTime = Time.time,
            sender = request.sender
        };

        if (request.channel == MaskRenderManager.PaintChannel.Vaccine)
            _playerPaints.Add(stored);
        else
            _enemyPaints.Add(stored);

        MaskRenderManager.StampCircle(this, request);
        OnCircleApplied?.Invoke(this, stored);
    }

    public IReadOnlyList<StoredCirclePaint> GetStoredPaints(MaskRenderManager.PaintChannel channel)
    {
        if (channel == MaskRenderManager.PaintChannel.Vaccine)
            return _playerPaints;

        return _enemyPaints;
    }

    public void ClearAllStoredPaint()
    {
        _playerPaints.Clear();
        _enemyPaints.Clear();
    }

    public void ClearStoredPaint(MaskRenderManager.PaintChannel channel)
    {
        if (channel == MaskRenderManager.PaintChannel.Vaccine)
            _playerPaints.Clear();
        else
            _enemyPaints.Clear();
    }

    public void ClearRuntimeData()
    {
        initialized = false;
        vaccineMask = null;
        virusMask = null;
        vaccineBuffer = null;
        virusBuffer = null;
        vaccineDirty = false;
        virusDirty = false;
        vaccineMaterialInstance = null;
        virusMaterialInstance = null;
        vaccineRenderer = null;
        virusRenderer = null;
        vaccineFilter = null;
        virusFilter = null;
        textureWidth = 0;
        textureHeight = 0;
    }
}