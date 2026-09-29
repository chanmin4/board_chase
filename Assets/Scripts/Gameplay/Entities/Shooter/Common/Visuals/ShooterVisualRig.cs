/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Visuals
 * Primary responsibility: Resolves stable visual sockets and applies non-gameplay shooter appearance profiles.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public sealed class ShooterVisualRig : MonoBehaviour
{
    [Header("Visual Roots")]
    [Tooltip("Visual-only body root. Do not assign a root that contains gameplay colliders if you plan to scale it.")]
    [SerializeField] private Transform _bodyVisualRoot;
    [Tooltip("Reference only. Appearance profiles never alter this root.")]
    [SerializeField] private Transform _gameplayColliderRoot;

    [Header("Stable Sockets")]
    [SerializeField] private Transform _armorSocket;
    [SerializeField] private Transform _rightHandSocket;
    [SerializeField] private Transform _leftHandSocket;
    [FormerlySerializedAs("_headSocket")]
    [SerializeField] private Transform _hairSocket;
    [SerializeField] private Transform _maskSocket;
    [SerializeField] private Transform _eyesSocket;
    [SerializeField] private Transform _mouthSocket;
    [SerializeField] private Transform _faceSocket;
    [SerializeField] private Transform _bodySocket;
    [SerializeField] private Transform _feetSocket;
    [SerializeField] private Transform _backSocket;

    [Header("Unified Customize")]
    [Tooltip("Preferred single asset for a complete shooter look. Legacy Appearance is used only when this is empty.")]
    [SerializeField] private ShooterCustomizeSO _defaultCustomize;

    [Header("Legacy Appearance")]
    [Tooltip("Compatibility fallback for older assets. New looks should use Default Customize.")]
    [SerializeField] private ShooterVisualAppearanceProfileSO _defaultAppearanceProfile;

    private readonly List<CosmeticInstance> _cosmeticInstances = new List<CosmeticInstance>();
    private readonly List<CustomizeCosmeticInstance> _customizeCosmeticInstances =
        new List<CustomizeCosmeticInstance>();
    private readonly Dictionary<string, ShooterVisualOccupancy> _equipmentOccupancies =
        new Dictionary<string, ShooterVisualOccupancy>();
    private readonly HashSet<string> _faceEquipmentSources = new HashSet<string>();
    private readonly Dictionary<Transform, ShooterVisualLocalTransform> _basePose =
        new Dictionary<Transform, ShooterVisualLocalTransform>();
    // Captured before runtime equipment and cosmetic instances are attached.
    // This includes sibling visual parts such as Feet, not only BodyVisualRoot.
    private Renderer[] _baseVisualRenderers;
    private bool _basePoseCached;

    public Transform BodyVisualRoot => _bodyVisualRoot;
    public Transform GameplayColliderRoot => _gameplayColliderRoot;
    public ShooterCustomizeSO DefaultCustomize => _defaultCustomize;
    public ShooterVisualAppearanceProfileSO DefaultAppearanceProfile => _defaultAppearanceProfile;
    public bool BodyVisualRootContainsCollider =>
        _bodyVisualRoot != null &&
        _bodyVisualRoot.GetComponentInChildren<Collider>(true) != null;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        AutoResolveReferences();
        CacheBasePose();

        if (_defaultCustomize != null)
            ApplyCustomize(_defaultCustomize);
        else if (_defaultAppearanceProfile != null)
            ApplyAppearance(_defaultAppearanceProfile);
    }

    // Purpose: Releases owned resources before this component is destroyed.
    private void OnDestroy()
    {
        ClearCosmeticAttachments();
    }

    [ContextMenu("Auto Resolve Visual Rig References")]
    // Purpose: Performs the auto resolve references workflow for this component.
    public void AutoResolveReferences()
    {
        _bodyVisualRoot ??= FindDescendant(transform, "Meshes");
        _gameplayColliderRoot ??= FindDescendant(transform, "GameplayColliderRoot");
        _armorSocket ??= FindDescendant(transform, "ArmorSocket");
        _rightHandSocket ??= FindDescendant(transform, "RightHand_WeaponSocket");
        _leftHandSocket ??= FindDescendant(transform, "LeftHandItemSocket") ??
                            FindDescendant(transform, "LeftHand");
        _hairSocket ??= FindDescendant(transform, "HairSocket") ??
                        FindDescendant(transform, "HeadSocket");
        _eyesSocket ??= FindDescendant(transform, "EyesSocket");
        _mouthSocket ??= FindDescendant(transform, "MouthSocket") ?? _maskSocket;
        _faceSocket ??= FindDescendant(transform, "FaceSocket") ?? _maskSocket ?? _eyesSocket;
        _maskSocket ??= FindDescendant(transform, "MaskSocket") ?? _faceSocket ?? _mouthSocket;
        _bodySocket ??= FindDescendant(transform, "BodySocket") ?? _bodyVisualRoot;
        _feetSocket ??= FindDescendant(transform, "FeetSocket");
        _backSocket ??= FindDescendant(transform, "BackAccessorySocket");
    }

    // Purpose: Updates the default appearance state.
    public void SetDefaultAppearance(ShooterVisualAppearanceProfileSO profile)
    {
        _defaultAppearanceProfile = profile;
    }

    // Purpose: Updates the default customize state.
    public void SetDefaultCustomize(ShooterCustomizeSO customize)
    {
        _defaultCustomize = customize;
    }

    // Purpose: Resolves the socket needed by this component.
    public Transform ResolveSocket(ShooterVisualSocketId socket)
    {
        return socket switch
        {
            ShooterVisualSocketId.Armor => _armorSocket,
            ShooterVisualSocketId.RightHand => _rightHandSocket,
            ShooterVisualSocketId.LeftHand => _leftHandSocket,
            ShooterVisualSocketId.Hair => _hairSocket,
            ShooterVisualSocketId.Mask => _maskSocket ?? _faceSocket ?? _mouthSocket,
            ShooterVisualSocketId.Eyes => _eyesSocket,
            ShooterVisualSocketId.Mouth => _mouthSocket ?? _maskSocket,
            ShooterVisualSocketId.Face => _faceSocket ?? _maskSocket ?? _eyesSocket,
            ShooterVisualSocketId.Body => _bodySocket ?? _bodyVisualRoot,
            ShooterVisualSocketId.Feet => _feetSocket,
            ShooterVisualSocketId.Back => _backSocket,
            _ => null
        };
    }

    /// <summary>
    /// Registers the visible regions currently claimed by dynamic equipment.
    /// Overlapping fixed cosmetics are hidden only while the equipment is worn.
    /// </summary>
    public void SetEquipmentOccupancy(string sourceId, ShooterVisualOccupancy occupancy)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            return;

        if (occupancy == ShooterVisualOccupancy.None)
            _equipmentOccupancies.Remove(sourceId);
        else
            _equipmentOccupancies[sourceId] = occupancy;

        RefreshCosmeticOccupancy();
    }

    // Purpose: Removes one dynamic equipment occupancy contribution.
    public void ClearEquipmentOccupancy(string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            return;

        bool occupancyChanged = _equipmentOccupancies.Remove(sourceId);
        bool faceSlotChanged = _faceEquipmentSources.Remove(sourceId);

        if (occupancyChanged || faceSlotChanged)
            RefreshCosmeticOccupancy();
    }

    /// <summary>
    /// Marks a dynamic Face-slot visual as active. While any Face visual is active,
    /// the Eyes and Mouth child sockets are hidden; clearing the final source restores them.
    /// </summary>
    public void SetEquipmentFaceSlotOccupied(string sourceId, bool occupied)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            return;

        bool changed = occupied
            ? _faceEquipmentSources.Add(sourceId)
            : _faceEquipmentSources.Remove(sourceId);

        if (changed)
            RefreshCosmeticOccupancy();
    }

    // Purpose: Applies the requested appearance change.
    public void ApplyAppearance(ShooterVisualAppearanceProfileSO appearance)
    {
        AutoResolveReferences();
        CacheBasePose();
        ResetBasePose();
        ClearCosmeticAttachments();

        if (appearance == null)
            return;

        ApplyBodyProfile(appearance.BodyProfile);
        ApplyBodyTint(appearance.BodyTint);

        IReadOnlyList<ShooterVisualAttachmentSO> accessories = appearance.Accessories;

        for (int i = 0; i < accessories.Count; i++)
            AttachCosmetic(accessories[i]);
    }

    // Purpose: Applies the requested customize change.
    public void ApplyCustomize(ShooterCustomizeSO customize)
    {
        AutoResolveReferences();
        CacheBasePose();
        ResetBasePose();
        ClearCosmeticAttachments();

        if (customize == null)
            return;

        customize.BodyVisualTransform.ApplyTo(_bodyVisualRoot);

        IReadOnlyList<ShooterVisualSocketPose> socketPoses = customize.SocketPoses;

        for (int i = 0; i < socketPoses.Count; i++)
            socketPoses[i].localTransform.ApplyTo(ResolveSocket(socketPoses[i].socket));

        ApplyBodyTint(customize.BodyTint);

        IReadOnlyList<ShooterCustomizeAttachment> attachments = customize.Attachments;

        for (int i = 0; i < attachments.Count; i++)
            AttachCustomizeCosmetic(attachments[i]);
    }

    // Purpose: Applies the requested body profile change.
    public void ApplyBodyProfile(ShooterVisualBodyProfileSO profile)
    {
        if (profile == null)
            return;

        profile.BodyVisualTransform.ApplyTo(_bodyVisualRoot);

        IReadOnlyList<ShooterVisualSocketPose> socketPoses = profile.SocketPoses;

        for (int i = 0; i < socketPoses.Count; i++)
        {
            Transform socket = ResolveSocket(socketPoses[i].socket);
            socketPoses[i].localTransform.ApplyTo(socket);
        }
    }

    // Purpose: Applies the requested body tint change.
    public void ApplyBodyTint(Color color)
    {
        CacheBaseVisualRenderers();

        for (int i = 0; i < _baseVisualRenderers.Length; i++)
        {
            Renderer renderer = _baseVisualRenderers[i];

            if (renderer == null)
                continue;

            Material[] materials = renderer.sharedMaterials;

            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];

                if (material == null)
                    continue;

                string propertyName = material.HasProperty("_BaseColor")
                    ? "_BaseColor"
                    : material.HasProperty("_Color")
                        ? "_Color"
                        : null;

                if (propertyName == null)
                    continue;

                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block, materialIndex);
                block.SetColor(propertyName, color);
                renderer.SetPropertyBlock(block, materialIndex);
            }
        }
    }

    // Purpose: Attempts to get cosmetic instance and reports whether the request succeeded.
    public bool TryGetCosmeticInstance(
        ShooterVisualAttachmentSO attachment,
        out Transform instance)
    {
        for (int i = 0; i < _cosmeticInstances.Count; i++)
        {
            CosmeticInstance entry = _cosmeticInstances[i];

            if (entry.attachment == attachment && entry.instance != null)
            {
                instance = entry.instance.transform;
                return true;
            }
        }

        instance = null;
        return false;
    }

    // Purpose: Performs the attach cosmetic workflow for this component.
    private void AttachCosmetic(ShooterVisualAttachmentSO attachment)
    {
        if (attachment == null || attachment.VisualPrefab == null)
            return;

        Transform socket = ResolveSocket(attachment.Socket);

        if (socket == null)
        {
            Debug.LogWarning(
                $"[ShooterVisualRig] Missing socket '{attachment.Socket}' for cosmetic '{attachment.name}'.",
                this);
            return;
        }

        GameObject instance = Instantiate(attachment.VisualPrefab, socket);
        instance.name = $"Cosmetic_{attachment.name}";
        attachment.ApplyLocalTransform(instance.transform);
        RegisterRuntimeVisualWithVision(instance.transform);
        _cosmeticInstances.Add(new CosmeticInstance(attachment, instance));
        RefreshCosmeticOccupancy();
    }

    // Purpose: Performs the attach customize cosmetic workflow for this component.
    private void AttachCustomizeCosmetic(ShooterCustomizeAttachment attachment)
    {
        if (attachment.visualPrefab == null)
            return;

        Transform socket = ResolveSocket(attachment.socket);

        if (socket == null)
        {
            Debug.LogWarning(
                $"[ShooterVisualRig] Missing socket '{attachment.socket}' for custom visual '{attachment.visualPrefab.name}'.",
                this);
            return;
        }

        GameObject instance = Instantiate(attachment.visualPrefab, socket);
        instance.name = $"Customize_{attachment.visualPrefab.name}";
        attachment.localTransform.ApplyTo(instance.transform);
        RegisterRuntimeVisualWithVision(instance.transform);
        _customizeCosmeticInstances.Add(
            new CustomizeCosmeticInstance(instance, attachment.socket, attachment.occupiedRegions));
        RefreshCosmeticOccupancy();
    }

    // Purpose: Registers runtime visual with vision with its owning runtime service.
    private void RegisterRuntimeVisualWithVision(Transform visualRoot)
    {
        VisionVisibilityTarget visibilityTarget =
            EntityComponentResolver.Resolve<VisionVisibilityTarget>(this);

        visibilityTarget?.RegisterRuntimeVisualRoot(visualRoot);
    }

    // Purpose: Clears the current cosmetic attachments state.
    private void ClearCosmeticAttachments()
    {
        for (int i = _cosmeticInstances.Count - 1; i >= 0; i--)
        {
            GameObject instance = _cosmeticInstances[i].instance;

            if (instance == null)
                continue;

            if (Application.isPlaying)
                Destroy(instance);
            else
                DestroyImmediate(instance);
        }

        _cosmeticInstances.Clear();

        for (int i = _customizeCosmeticInstances.Count - 1; i >= 0; i--)
        {
            GameObject instance = _customizeCosmeticInstances[i].instance;

            if (instance == null)
                continue;

            if (Application.isPlaying)
                Destroy(instance);
            else
                DestroyImmediate(instance);
        }

        _customizeCosmeticInstances.Clear();
        RefreshCosmeticOccupancy();
    }

    // Purpose: Hides fixed cosmetics only when their declared visual regions overlap equipped gear.
    private void RefreshCosmeticOccupancy()
    {
        ShooterVisualOccupancy equippedRegions = ShooterVisualOccupancy.None;

        foreach (KeyValuePair<string, ShooterVisualOccupancy> pair in _equipmentOccupancies)
            equippedRegions |= pair.Value;

        for (int i = 0; i < _cosmeticInstances.Count; i++)
        {
            CosmeticInstance entry = _cosmeticInstances[i];
            if (entry.instance == null || entry.attachment == null)
                continue;

            bool overlaps = (entry.attachment.OccupiedRegions & equippedRegions) != 0;
            entry.instance.SetActive(!overlaps);
        }

        for (int i = 0; i < _customizeCosmeticInstances.Count; i++)
        {
            CustomizeCosmeticInstance entry = _customizeCosmeticInstances[i];
            if (entry.instance == null)
                continue;

            bool overlaps = (entry.occupiedRegions & equippedRegions) != 0;
            entry.instance.SetActive(!overlaps);
        }

        RefreshFaceChildSocketVisibility();
    }

    // Purpose: Caches base pose for later reuse.
    private void CacheBasePose()
    {
        if (_basePoseCached)
            return;

        CachePose(_bodyVisualRoot);
        CachePose(_armorSocket);
        CachePose(_rightHandSocket);
        CachePose(_leftHandSocket);
        CachePose(_hairSocket);
        CachePose(_maskSocket);
        CachePose(_eyesSocket);
        CachePose(_mouthSocket);
        CachePose(_faceSocket);
        CachePose(_bodySocket);
        CachePose(_feetSocket);
        CachePose(_backSocket);
        _basePoseCached = true;
    }

    // Purpose: Performs the reset base pose workflow for this component.
    private void ResetBasePose()
    {
        foreach (KeyValuePair<Transform, ShooterVisualLocalTransform> pair in _basePose)
            pair.Value.ApplyTo(pair.Key);

        CacheBaseVisualRenderers();

        for (int i = 0; i < _baseVisualRenderers.Length; i++)
        {
            Renderer renderer = _baseVisualRenderers[i];

            if (renderer == null)
                continue;

            Material[] materials = renderer.sharedMaterials;

            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                renderer.SetPropertyBlock(null, materialIndex);
        }
    }

    // Purpose: Caches pose for later reuse.
    private void CachePose(Transform target)
    {
        if (target != null)
            _basePose[target] = ShooterVisualLocalTransform.FromTransform(target);
    }

    // Purpose: Caches base visual renderers for later reuse.
    private void CacheBaseVisualRenderers()
    {
        if (_baseVisualRenderers == null)
        {
            _baseVisualRenderers = GetComponentsInChildren<Renderer>(true);
        }
    }

    // Purpose: Finds the requested descendant.
    private static Transform FindDescendant(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        Transform[] descendants = root.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < descendants.Length; i++)
        {
            if (descendants[i] != null && descendants[i].name == childName)
                return descendants[i];
        }

        return null;
    }

    // Purpose: Hides Eye and Mouth child slots only while a Face-slot visual is active.
    private void RefreshFaceChildSocketVisibility()
    {
        bool faceOccupied = _faceEquipmentSources.Count > 0 || HasFaceCosmeticAttachment();
        SetSocketVisible(_eyesSocket, !faceOccupied);
        SetSocketVisible(_mouthSocket, !faceOccupied);
    }

    // Purpose: Determines whether an authored cosmetic is currently using the Face slot.
    private bool HasFaceCosmeticAttachment()
    {
        for (int i = 0; i < _cosmeticInstances.Count; i++)
        {
            ShooterVisualAttachmentSO attachment = _cosmeticInstances[i].attachment;
            if (attachment != null && attachment.Socket == ShooterVisualSocketId.Face)
                return true;
        }

        for (int i = 0; i < _customizeCosmeticInstances.Count; i++)
        {
            if (_customizeCosmeticInstances[i].socket == ShooterVisualSocketId.Face)
                return true;
        }

        return false;
    }

    // Purpose: Applies the Face-slot visibility gate without disabling the Face parent itself.
    private static void SetSocketVisible(Transform socket, bool visible)
    {
        if (socket != null && socket.gameObject.activeSelf != visible)
            socket.gameObject.SetActive(visible);
    }

    private readonly struct CosmeticInstance
    {
        public readonly ShooterVisualAttachmentSO attachment;
        public readonly GameObject instance;

        public CosmeticInstance(
            ShooterVisualAttachmentSO attachment,
            GameObject instance)
        {
            this.attachment = attachment;
            this.instance = instance;
        }
    }

    private readonly struct CustomizeCosmeticInstance
    {
        public readonly GameObject instance;
        public readonly ShooterVisualSocketId socket;
        public readonly ShooterVisualOccupancy occupiedRegions;

        public CustomizeCosmeticInstance(
            GameObject instance,
            ShooterVisualSocketId socket,
            ShooterVisualOccupancy occupiedRegions)
        {
            this.instance = instance;
            this.socket = socket;
            this.occupiedRegions = occupiedRegions;
        }
    }
}
