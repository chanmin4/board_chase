/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Core
 * Primary responsibility: Manages ShooterEquipmentController item storage, equipment, transfer state, and related rules.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class ShooterEquipmentController : MonoBehaviour
{
    [Header("Weapon Runtime")]
    [SerializeField] private WeaponSO _currentWeapon;

    [Header("Shared Hand Socket")]
    [Tooltip("Shared hand mount for weapons and usable items. Existing weapon placement is preserved.")]
    [FormerlySerializedAs("_rightGunBone")]
    [SerializeField] private Transform _weaponRoot;

    [Header("Armor")]
    [SerializeField] private EntityEquipmentRuntime _equipmentRuntime;
    [SerializeField] private Transform _armorVisualRoot;
    [SerializeField] private bool _renderArmorVisual = true;

    [Header("Mask Equipment / Player Gas Mask")]
    [Tooltip("Resolved only on player shooters. Enemies keep using their authored Shooter Customize head cosmetics.")]
    [SerializeField] private PlayerInventoryRuntime _playerInventory;
    [Tooltip("Optional explicit root for an equipped gas-mask visual. Leave empty to use the FaceSocket or MouthSocket selected by the item. Older prefabs retain a HeadSocket fallback.")]
    [FormerlySerializedAs("_headEquipmentVisualRoot")]
    [SerializeField] private Transform _maskEquipmentVisualRoot;
    [SerializeField] private bool _renderGasMaskVisual = true;

    [Header("Runtime Scene Refs")]
    [SerializeField] private Transform _projectilesRoot;

    [Header("Visual Aim")]
    [Tooltip("Visual/model aim root used for rotating equipped visuals and as a forward reference for shooter-facing systems. If empty, this transform is used.")]
    [SerializeField] private Transform _visualAimRoot;
    [SerializeField] private bool _enableVisualAim = true;
    [SerializeField] private float _visualAimTurnSmoothTime = 0.02f;

    [Header("Debug")]
    [SerializeField] private bool _logMissingRefs = true;

    private WeaponView _currentWeaponView;
    private GameObject _heldItemVisual;
    private GameObject _armorVisualInstance;
    private GameObject _gasMaskVisualInstance;
    private float _visualAimTurnVelocity;
    private bool _visible = true;
    private bool _weaponViewSuppressed;
    private bool _gasMaskUsesFaceSlot;
    private bool _loggedWeaponRootFallback;
    private ShooterVisualOccupancy _armorVisualOccupancy;
    private ShooterVisualOccupancy _gasMaskVisualOccupancy;

    public event Action<WeaponSO> OnWeaponChanged;
    public event Action<ArmorItemSO> OnArmorChanged;

    public WeaponSO CurrentWeapon => _equipmentRuntime != null ? _equipmentRuntime.CurrentWeapon : _currentWeapon;
    public int ActiveWeaponSlotIndex => _equipmentRuntime != null ? _equipmentRuntime.ActiveWeaponSlotIndex : 0;
    public int CurrentWeaponAmount => _equipmentRuntime != null ? _equipmentRuntime.CurrentWeaponAmount : (_currentWeapon != null ? 1 : 0);
    public WeaponView CurrentWeaponView => _currentWeaponView;
    public ArmorItemSO CurrentArmor => _equipmentRuntime != null ? _equipmentRuntime.CurrentArmor : null;
    public float CurrentArmorDurability => _equipmentRuntime != null ? _equipmentRuntime.CurrentArmorDurability : 0f;
    public float MaxArmorDurability => _equipmentRuntime != null ? _equipmentRuntime.MaxArmorDurability : 0f;
    public EntityEquipmentRuntime EquipmentRuntime => _equipmentRuntime;
    public float CurrentWeaponDurability => _equipmentRuntime != null ? _equipmentRuntime.CurrentWeaponDurability : 0f;
    public float MaxWeaponDurability => _equipmentRuntime != null ? _equipmentRuntime.MaxWeaponDurability : 0f;
    public float CurrentWeaponCondition01 => _equipmentRuntime != null ? _equipmentRuntime.CurrentWeaponCondition01 : 1f;
    public Transform GameplayFireOrigin => _currentWeaponView != null ? _currentWeaponView.FireOrigin : null;
    public Transform VisualFireOrigin => GameplayFireOrigin;
    public Vector3 FireDirection => _currentWeaponView != null ? _currentWeaponView.FireDirection.normalized : Vector3.forward;
    public Transform ProjectilesRoot => _projectilesRoot;
    public Transform WeaponRoot => _weaponRoot;
    public Transform HandSocket => ResolveWeaponVisualRoot();
    public Transform CurrentMeleeSwingEffectOrigin =>
        _currentWeaponView is MeleeWeaponView meleeView
            ? meleeView.SwingEffectOrigin
            : null;

    // Purpose: Initializes Inspector defaults for this component.
    protected virtual void Reset()
    {
        ResolveRefs();
    }

    // Purpose: Caches the component references required before runtime initialization.
    protected virtual void Awake()
    {
        ResolveRefs();

        if (_equipmentRuntime != null)
        {
            _currentWeapon = _equipmentRuntime.CurrentWeapon;
            RebuildWeaponView(_currentWeapon);
        }
        else
        {
            Equip(_currentWeapon, true);
        }

        SyncArmorVisual();
        SyncGasMaskVisual();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    protected virtual void OnEnable()
    {
        ResolveRefs();

        if (_equipmentRuntime != null)
        {
            _equipmentRuntime.OnWeaponChanged += HandleWeaponChanged;
            _equipmentRuntime.OnArmorChanged += HandleArmorChanged;
        }

        if (_playerInventory != null)
            _playerInventory.GasMaskChanged += HandleGasMaskChanged;

        SyncWeaponView();
        SyncArmorVisual();
        SyncGasMaskVisual();
        RefreshVisionRendererRegistration();
    }

    // Purpose: Completes initialization after scene dependencies are ready.
    protected virtual void Start()
    {
        SyncWeaponView();
        SyncArmorVisual();
        SyncGasMaskVisual();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    protected virtual void OnDisable()
    {
        if (_equipmentRuntime != null)
        {
            _equipmentRuntime.OnWeaponChanged -= HandleWeaponChanged;
            _equipmentRuntime.OnArmorChanged -= HandleArmorChanged;
        }

        if (_playerInventory != null)
            _playerInventory.GasMaskChanged -= HandleGasMaskChanged;
    }

    // Purpose: Releases this controller's visual occupancy claims when its shooter is removed.
    protected virtual void OnDestroy()
    {
        ShooterVisualRig visualRig = GetComponentInChildren<ShooterVisualRig>(true);
        visualRig?.ClearEquipmentOccupancy(GetVisualOccupancySourceId());
    }

    // Purpose: Updates the projectiles root state.
    public void SetProjectilesRoot(Transform root)
    {
        _projectilesRoot = root;
    }

    // Purpose: Updates the visible state.
    public void SetVisible(bool visible)
    {
        _visible = visible;

        RefreshWeaponViewVisibility();
        RefreshGasMaskVisualVisibility();
        if (_heldItemVisual != null)
            _heldItemVisual.SetActive(_visible);
    }

    public void SetHeldItemVisual(GameObject visual)
    {
        _heldItemVisual = visual;
        if (visual != null)
        {
            RegisterRuntimeVisualWithVision(visual.transform);
            visual.SetActive(_visible);
        }
        RefreshVisionRendererRegistration();
    }

    // Purpose: Updates the weapon view suppressed state.
    public void SetWeaponViewSuppressed(bool suppressed)
    {
        _weaponViewSuppressed = suppressed;
        RefreshWeaponViewVisibility();
    }

    // Purpose: Updates the visual aim from the current runtime state.
    public void UpdateVisualAim(Vector3 worldTarget)
    {
        if (!_enableVisualAim)
            return;

        Transform aimRoot = _visualAimRoot != null ? _visualAimRoot : transform;
        Transform fireOrigin = VisualFireOrigin;

        if (fireOrigin == null)
            return;

        Vector3 flatDirection = worldTarget - fireOrigin.position;
        flatDirection.y = 0f;

        if (flatDirection.sqrMagnitude < 0.0001f)
            return;

        float targetYaw = Mathf.Atan2(flatDirection.x, flatDirection.z) * Mathf.Rad2Deg;
        Vector3 euler = aimRoot.eulerAngles;

        euler.y = Mathf.SmoothDampAngle(
            euler.y,
            targetYaw,
            ref _visualAimTurnVelocity,
            Mathf.Max(0.001f, _visualAimTurnSmoothTime));

        aimRoot.eulerAngles = euler;
    }

    // Purpose: Equips equip for the current owner.
    public void Equip(WeaponSO newWeapon)
    {
        ResolveRefs();

        if (_equipmentRuntime != null)
        {
            _equipmentRuntime.EquipWeapon(newWeapon);
            return;
        }

        Equip(newWeapon, false);
    }

    // Purpose: Equips equip for the current owner.
    public void Equip(int weaponSlotIndex, WeaponSO newWeapon, int amount = 1)
    {
        ResolveRefs();

        if (_equipmentRuntime != null)
        {
            _equipmentRuntime.EquipWeapon(weaponSlotIndex, newWeapon, amount);
            return;
        }

        Equip(newWeapon, false);
    }

    // Purpose: Equips enemy spawn weapon for the current owner.
    public void EquipEnemySpawnWeapon(
        int weaponSlotIndex,
        WeaponSO newWeapon,
        int amount,
        EnemyShooterRole enemyRole)
    {
        ResolveRefs();

        if (_equipmentRuntime != null)
        {
            _equipmentRuntime.EquipEnemySpawnWeapon(
                weaponSlotIndex,
                newWeapon,
                amount,
                enemyRole);
            return;
        }

        Equip(newWeapon, false);
    }

    // Purpose: Updates the active weapon slot state.
    public void SetActiveWeaponSlot(int weaponSlotIndex)
    {
        ResolveRefs();
        _equipmentRuntime?.SetActiveWeaponSlot(weaponSlotIndex);
    }

    // Purpose: Returns the current weapon.
    public WeaponSO GetWeapon(int weaponSlotIndex)
    {
        ResolveRefs();
        return _equipmentRuntime != null ? _equipmentRuntime.GetWeapon(weaponSlotIndex) : _currentWeapon;
    }

    // Purpose: Returns the current weapon amount.
    public int GetWeaponAmount(int weaponSlotIndex)
    {
        ResolveRefs();
        return _equipmentRuntime != null ? _equipmentRuntime.GetWeaponAmount(weaponSlotIndex) : (_currentWeapon != null ? 1 : 0);
    }

    // Purpose: Attempts to consume weapon stack and reports whether the request succeeded.
    public bool TryConsumeWeaponStack(
        int weaponSlotIndex,
        int amount,
        out WeaponSO consumedWeapon,
        out int remainingAmount)
    {
        ResolveRefs();

        if (_equipmentRuntime != null)
        {
            return _equipmentRuntime.TryConsumeWeaponStack(
                weaponSlotIndex,
                amount,
                out consumedWeapon,
                out remainingAmount);
        }

        consumedWeapon = _currentWeapon;
        remainingAmount = 0;

        if (_currentWeapon == null || amount > 1)
            return false;

        ClearWeapon();
        return true;
    }

    // Purpose: Clears the current weapon state.
    public void ClearWeapon()
    {
        ResolveRefs();

        if (_equipmentRuntime != null)
        {
            _equipmentRuntime.ClearWeapon();
            return;
        }

        if (_currentWeapon == null && _currentWeaponView == null)
            return;

        _currentWeapon = null;
        ClearCurrentWeaponView();
        OnWeaponChanged?.Invoke(null);
    }

    // Purpose: Clears the current weapon state.
    public void ClearWeapon(int weaponSlotIndex)
    {
        ResolveRefs();

        if (_equipmentRuntime != null)
        {
            _equipmentRuntime.ClearWeapon(weaponSlotIndex);
            return;
        }

        ClearWeapon();
    }

    // Purpose: Equips armor for the current owner.
    public void EquipArmor(ArmorItemSO armor)
    {
        EquipArmor(armor, armor != null ? armor.MaxDurability : 0f);
    }

    // Purpose: Equips armor for the current owner.
    public void EquipArmor(ArmorItemSO armor, float durability)
    {
        ResolveRefs();

        if (_equipmentRuntime != null)
        {
            _equipmentRuntime.EquipArmor(armor, durability);
            return;
        }

        RebuildArmorVisual(armor);
        OnArmorChanged?.Invoke(armor);
    }

    // Purpose: Clears the current armor state.
    public void ClearArmor()
    {
        ResolveRefs();

        if (_equipmentRuntime != null)
        {
            _equipmentRuntime.ClearArmor();
            return;
        }

        ClearArmorVisual();
        _armorVisualOccupancy = ShooterVisualOccupancy.None;
        SyncVisualOccupancy();
        OnArmorChanged?.Invoke(null);
    }

    // Purpose: Synchronizes armor visual with the current source state.
    public void SyncArmorVisual()
    {
        RebuildArmorVisual(CurrentArmor);
    }

    /// <summary>
    /// Synchronizes the player-only gas-mask equipment view. This intentionally
    /// remains separate from ShooterCustomizeSO: equipment can change during a
    /// run, while a Customize SO is the shooter's fixed authored appearance.
    /// </summary>
    public void SyncGasMaskVisual()
    {
        RebuildGasMaskVisual(_playerInventory != null ? _playerInventory.EquippedGasMask : null);
    }

    // Purpose: Synchronizes weapon view with the current source state.
    public void SyncWeaponView()
    {
        WeaponSO weapon = CurrentWeapon;

        if (_currentWeapon == weapon && _currentWeaponView != null)
            return;

        _currentWeapon = weapon;
        RebuildWeaponView(_currentWeapon);
    }

    // Purpose: Plays the requested muzzle particle action or effect.
    public void PlayMuzzleParticle()
    {
        RangedWeaponSO rangedWeapon = _currentWeapon as RangedWeaponSO;

        if (rangedWeapon == null ||
            rangedWeapon.MuzzleParticlePrefab == null ||
            _currentWeaponView == null ||
            !_currentWeaponView.HasFireOrigin)
        {
            return;
        }

        ParticleSystem instance = Instantiate(
            rangedWeapon.MuzzleParticlePrefab,
            _currentWeaponView.FireOrigin.position,
            _currentWeaponView.FireOrigin.rotation,
            _currentWeaponView.FireOrigin);

        RegisterRuntimeVisualWithVision(instance.transform);
        instance.Play();
        Destroy(instance.gameObject, ResolveParticleLifetime(instance));
    }

    // Purpose: Plays the requested melee swing action or effect.
    public void PlayMeleeSwing(
        Vector3 lockedWorldPosition,
        Quaternion lockedWorldRotation,
        Transform particleContainerRoot,
        Transform positionFollowTarget,
        Vector3 lockedFollowOffset,
        float uniformParticleScale,
        bool debugLogs = false)
    {
        MeleeWeaponSO meleeWeapon = CurrentWeapon as MeleeWeaponSO;

        if (meleeWeapon == null)
            return;

        PlayMeleeSwing(
            meleeWeapon.SwingParticlePrefab,
            meleeWeapon.SwingTrailParticlePrefab,
            lockedWorldPosition,
            lockedWorldRotation,
            particleContainerRoot,
            positionFollowTarget,
            lockedFollowOffset,
            uniformParticleScale,
            debugLogs);
    }

    // Purpose: Plays the requested melee swing action or effect.
    public void PlayMeleeSwing(
        ParticleSystem swingParticlePrefab,
        ParticleSystem swingTrailParticlePrefab,
        Vector3 lockedWorldPosition,
        Quaternion lockedWorldRotation,
        Transform particleContainerRoot,
        Transform positionFollowTarget,
        Vector3 lockedFollowOffset,
        float uniformParticleScale,
        bool debugLogs = false)
    {

        SpawnMeleeParticle(
            swingParticlePrefab,
            lockedWorldPosition,
            lockedWorldRotation,
            particleContainerRoot,
            positionFollowTarget,
            lockedFollowOffset,
            uniformParticleScale,
            debugLogs);

        SpawnMeleeParticle(
            swingTrailParticlePrefab,
            lockedWorldPosition,
            lockedWorldRotation,
            particleContainerRoot,
            positionFollowTarget,
            lockedFollowOffset,
            uniformParticleScale,
            debugLogs);
    }

    // Purpose: Spawns melee particle for the current runtime context.
    private void SpawnMeleeParticle(
        ParticleSystem particlePrefab,
        Vector3 worldPosition,
        Quaternion worldRotation,
        Transform particleContainerRoot,
        Transform positionFollowTarget,
        Vector3 lockedFollowOffset,
        float uniformParticleScale,
        bool debugLogs)
    {
        if (particlePrefab == null)
            return;

        if (positionFollowTarget != null)
            worldPosition = positionFollowTarget.position + lockedFollowOffset;

        ParticleSystem instance = UnityEngine.Object.Instantiate(
            particlePrefab,
            worldPosition,
            worldRotation,
            particleContainerRoot);

        RegisterRuntimeVisualWithVision(instance.transform);

        float resolvedScale = Mathf.Max(0.01f, uniformParticleScale);
        instance.transform.localScale *= resolvedScale;

        ParticleSystem[] systems =
            instance.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem.MainModule main = systems[i].main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        }

        Vector3 followOffset = positionFollowTarget != null
            ? lockedFollowOffset
            : Vector3.zero;

        float lifetime = ResolveParticleLifetime(instance);

        if (positionFollowTarget != null)
        {
            StartCoroutine(
                FollowMeleeParticlePosition(
                    instance.transform,
                    positionFollowTarget,
                    followOffset,
                    worldRotation,
                    lifetime));
        }

        if (debugLogs)
        {
            Debug.Log(
                $"[ShooterEquipmentController] Melee particle spawned. " +
                $"prefab={particlePrefab.name}, position={worldPosition}, " +
                $"attackEuler={worldRotation.eulerAngles}, " +
                $"uniformScale={resolvedScale:0.###}, " +
                //$"followTarget={(positionFollowTarget != null ? positionFollowTarget.name : \"None\")}, " +
                $"followOffset={followOffset}, " +
                //$"container={(particleContainerRoot != null ? particleContainerRoot.name : \"None\")}",
                instance);
        }

        instance.Play(true);
        UnityEngine.Object.Destroy(instance.gameObject, lifetime);
    }

    // Purpose: Performs the follow melee particle position workflow for this component.
    private static IEnumerator FollowMeleeParticlePosition(
        Transform particleTransform,
        Transform positionFollowTarget,
        Vector3 worldOffset,
        Quaternion lockedWorldRotation,
        float lifetime)
    {
        float finishTime = Time.time + Mathf.Max(0.1f, lifetime);

        while (particleTransform != null && Time.time < finishTime)
        {
            if (positionFollowTarget == null)
                yield break;

            particleTransform.SetPositionAndRotation(
                positionFollowTarget.position + worldOffset,
                lockedWorldRotation);

            yield return null;
        }
    }

    // Purpose: Resolves the refs needed by this component.
    protected virtual void ResolveRefs()
    {
        if (_equipmentRuntime == null)
            _equipmentRuntime = EntityComponentResolver.Resolve<EntityEquipmentRuntime>(this);

        if (_playerInventory == null)
            _playerInventory = EntityComponentResolver.Resolve<PlayerInventoryRuntime>(this);
    }

    // Purpose: Equips equip for the current owner.
    private void Equip(WeaponSO newWeapon, bool force)
    {
        if (newWeapon == null)
            return;

        if (!force && _currentWeapon == newWeapon)
            return;

        _currentWeapon = newWeapon;
        RebuildWeaponView(newWeapon);
        OnWeaponChanged?.Invoke(_currentWeapon);
    }

    // Purpose: Handles the incoming weapon changed request or event.
    private void HandleWeaponChanged(WeaponSO weapon)
    {
        _currentWeapon = weapon;
        RebuildWeaponView(_currentWeapon);
        OnWeaponChanged?.Invoke(_currentWeapon);
    }

    // Purpose: Repairs or rebuilds weapon view from the current source data.
    private void RebuildWeaponView(WeaponSO weapon)
    {
        ClearCurrentWeaponView();

        if (weapon == null || weapon.WeaponViewPrefab == null)
        {
            if (_logMissingRefs && weapon != null)
                Debug.LogError($"[ShooterEquipmentController] Weapon view prefab is missing on {weapon.name}.", weapon);

            return;
        }

        Transform weaponRoot = ResolveWeaponVisualRoot();
        if (weaponRoot == null)
        {
            if (_logMissingRefs)
                Debug.LogError("[ShooterEquipmentController] Unable to resolve a Weapon Root or visual fallback.", this);

            return;
        }

        _currentWeaponView = Instantiate(weapon.WeaponViewPrefab, weaponRoot);
        weapon.ApplyWeaponViewTransform(_currentWeaponView.transform);
        RegisterRuntimeVisualWithVision(_currentWeaponView.transform);
        RefreshWeaponViewVisibility();

        if (_logMissingRefs &&
            weapon is RangedWeaponSO &&
            !_currentWeaponView.HasFireOrigin)
        {
            Debug.LogError(
                $"[ShooterEquipmentController] WeaponView Fire Origin is missing. weapon={weapon.name}, view={_currentWeaponView.name}",
                _currentWeaponView);
        }

        if (_logMissingRefs &&
            weapon is MeleeWeaponSO &&
            !(_currentWeaponView is MeleeWeaponView))
        {
            Debug.LogError(
                $"[ShooterEquipmentController] Melee weapon view prefab must use MeleeWeaponView. weapon={weapon.name}, view={_currentWeaponView.name}",
                _currentWeaponView);
        }
    }

    // Purpose: Resolves an explicit weapon socket first, then safely falls back to the shooter's visual root.
    private Transform ResolveWeaponVisualRoot()
    {
        if (_weaponRoot != null)
            return _weaponRoot;

        Transform socket = FindVisualTransformByName("RightHand_WeaponSocket")
            ?? FindVisualTransformByName("RightHandWeaponSocket")
            ?? FindVisualTransformByName("WeaponRoot")
            ?? FindVisualTransformByName("WeaponSocket");

        if (socket != null)
            return socket;

        Transform visualRoot = FindVisualTransformByName("Visual")
            ?? FindVisualTransformByName("Visual_Shooter")
            ?? _visualAimRoot
            ?? transform;

        if (_logMissingRefs && !_loggedWeaponRootFallback)
        {
            _loggedWeaponRootFallback = true;
            Debug.LogWarning(
                "[ShooterEquipmentController] Weapon Root is not assigned. " +
                $"Using visual fallback '{visualRoot.name}'. Assign RightHand_WeaponSocket on the authored prefab for exact weapon placement.",
                this);
        }

        return visualRoot;
    }

    // Purpose: Finds a named transform anywhere below this shooter, including inactive visual prefab children.
    private Transform FindVisualTransformByName(string targetName)
    {
        if (string.IsNullOrWhiteSpace(targetName))
            return null;

        Transform[] transforms = GetComponentsInChildren<Transform>(true);
        for (int index = 0; index < transforms.Length; index++)
        {
            Transform candidate = transforms[index];
            if (candidate != null && candidate.name == targetName)
                return candidate;
        }

        return null;
    }

    // Purpose: Repairs or rebuilds armor visual from the current source data.
    private void RebuildArmorVisual(ArmorItemSO armor)
    {
        ClearArmorVisual();
        _armorVisualOccupancy = ShooterVisualOccupancy.None;

        if (!_renderArmorVisual ||
            armor == null ||
            armor.EquippedVisualPrefab == null)
        {
            SyncVisualOccupancy();
            return;
        }

        Transform root = ResolveArmorVisualRoot();

        if (root == null)
        {
            SyncVisualOccupancy();
            return;
        }

        _armorVisualInstance = Instantiate(armor.EquippedVisualPrefab, root);
        armor.ApplyEquippedVisualTransform(_armorVisualInstance.transform);
        RegisterRuntimeVisualWithVision(_armorVisualInstance.transform);
        _armorVisualOccupancy = armor.VisualCoverage;
        SyncVisualOccupancy();
    }

    // Purpose: Resolves the armor visual root needed by this component.
    private Transform ResolveArmorVisualRoot()
    {
        if (_armorVisualRoot != null)
            return _armorVisualRoot;

        if (_equipmentRuntime != null && _equipmentRuntime.ArmorVisualRoot != null)
            return _equipmentRuntime.ArmorVisualRoot;

        return transform;
    }

    // Purpose: Handles the incoming armor changed request or event.
    private void HandleArmorChanged(ArmorItemSO armor)
    {
        SyncArmorVisual();
        OnArmorChanged?.Invoke(armor);
    }

    // Purpose: Rebuilds the player head-equipment visual when the equipped gas mask changes.
    private void HandleGasMaskChanged(GasMaskItemSO gasMask)
    {
        RebuildGasMaskVisual(gasMask);
    }

    // Purpose: Rebuilds the current gas-mask view from item-owned visual data.
    private void RebuildGasMaskVisual(GasMaskItemSO gasMask)
    {
        ClearGasMaskVisual();
        _gasMaskVisualOccupancy = ShooterVisualOccupancy.None;
        _gasMaskUsesFaceSlot = false;

        if (!_renderGasMaskVisual ||
            gasMask == null ||
            gasMask.EquippedVisualPrefab == null)
        {
            SyncVisualOccupancy();
            return;
        }

        Transform root = ResolveGasMaskVisualRoot(gasMask);
        if (root == null)
        {
            if (_logMissingRefs)
            {
                Debug.LogWarning(
                    $"[ShooterEquipmentController] Gas-mask visual skipped because the selected Face/Mouth socket and legacy fallback are missing. mask={gasMask.name}",
                    this);
            }

            SyncVisualOccupancy();
            return;
        }

        _gasMaskVisualInstance = Instantiate(gasMask.EquippedVisualPrefab, root);
        _gasMaskVisualInstance.name = $"Equipped_{gasMask.EquippedVisualPrefab.name}";
        gasMask.ApplyEquippedVisualTransform(_gasMaskVisualInstance.transform);
        RegisterRuntimeVisualWithVision(_gasMaskVisualInstance.transform);
        _gasMaskVisualOccupancy = gasMask.VisualCoverage;
        _gasMaskUsesFaceSlot = gasMask.EquippedVisualSocket == ShooterVisualSocketId.Face;
        SyncVisualOccupancy();
        RefreshGasMaskVisualVisibility();
    }

    // Purpose: Resolves the authored face-region socket for dynamic gas-mask equipment visuals.
    private Transform ResolveGasMaskVisualRoot(GasMaskItemSO gasMask)
    {
        if (_maskEquipmentVisualRoot != null)
            return _maskEquipmentVisualRoot;

        ShooterVisualRig visualRig = GetComponentInChildren<ShooterVisualRig>(true);
        ShooterVisualSocketId desiredSocket = gasMask != null
            ? gasMask.EquippedVisualSocket
            : ShooterVisualSocketId.Mask;
        Transform primarySocket = visualRig != null
            ? visualRig.ResolveSocket(desiredSocket)
            : null;

        Transform legacyHairSocket = visualRig != null
            ? visualRig.ResolveSocket(ShooterVisualSocketId.Hair)
            : null;

        return primarySocket ??
               (visualRig != null ? visualRig.ResolveSocket(ShooterVisualSocketId.Face) : null) ??
               FindVisualTransformByName("MaskSocket") ??
               legacyHairSocket ??
               FindVisualTransformByName("HeadSocket");
    }

    // Purpose: Synchronizes dynamic armor and head-equipment visual regions with the shooter visual rig.
    private void SyncVisualOccupancy()
    {
        ShooterVisualRig visualRig = GetComponentInChildren<ShooterVisualRig>(true);
        if (visualRig == null)
            return;

        string sourceId = GetVisualOccupancySourceId();
        visualRig.SetEquipmentOccupancy(
            sourceId,
            _armorVisualOccupancy | _gasMaskVisualOccupancy);
        visualRig.SetEquipmentFaceSlotOccupied(sourceId, _gasMaskUsesFaceSlot);
    }

    // Purpose: Builds one stable per-controller key for visual occupancy registration.
    private string GetVisualOccupancySourceId()
    {
        return $"ShooterEquipmentController_{GetInstanceID()}";
    }

    // Purpose: Resolves the particle lifetime needed by this component.
    private static float ResolveParticleLifetime(ParticleSystem particle)
    {
        if (particle == null)
            return 1f;

        ParticleSystem.MainModule main = particle.main;
        float lifetime = main.startLifetime.constantMax;

        return Mathf.Max(0.1f, main.duration + lifetime);
    }

    // Purpose: Clears the current current weapon view state.
    private void ClearCurrentWeaponView()
    {
        if (_currentWeaponView != null)
            Destroy(_currentWeaponView.gameObject);

        _currentWeaponView = null;
        RefreshVisionRendererRegistration();
    }

    // Purpose: Refreshes the displayed or cached weapon view visibility.
    private void RefreshWeaponViewVisibility()
    {
        if (_currentWeaponView != null)
            _currentWeaponView.gameObject.SetActive(_visible && !_weaponViewSuppressed);
    }

    // Purpose: Clears the current armor visual state.
    private void ClearArmorVisual()
    {
        if (_armorVisualInstance != null)
            Destroy(_armorVisualInstance);

        _armorVisualInstance = null;
        RefreshVisionRendererRegistration();
    }

    // Purpose: Clears the previous dynamic gas-mask visual instance.
    private void ClearGasMaskVisual()
    {
        if (_gasMaskVisualInstance != null)
            Destroy(_gasMaskVisualInstance);

        _gasMaskVisualInstance = null;
        RefreshVisionRendererRegistration();
    }

    // Purpose: Keeps dynamic head equipment consistent with the shooter visibility system.
    private void RefreshGasMaskVisualVisibility()
    {
        if (_gasMaskVisualInstance != null)
            _gasMaskVisualInstance.SetActive(_visible);
    }

    // Purpose: Registers runtime visual with vision with its owning runtime service.
    private void RegisterRuntimeVisualWithVision(Transform visualRoot)
    {
        VisionVisibilityTarget visibilityTarget =
            EntityComponentResolver.Resolve<VisionVisibilityTarget>(this);

        visibilityTarget?.RegisterRuntimeVisualRoot(visualRoot);
    }

    // Purpose: Refreshes the displayed or cached vision renderer registration.
    private void RefreshVisionRendererRegistration()
    {
        VisionVisibilityTarget visibilityTarget =
            EntityComponentResolver.Resolve<VisionVisibilityTarget>(this);

        visibilityTarget?.RefreshRenderers();
    }
}
