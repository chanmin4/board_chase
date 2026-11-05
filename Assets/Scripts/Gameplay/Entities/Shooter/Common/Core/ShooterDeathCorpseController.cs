/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Core
 * Primary responsibility: Coordinates the ShooterDeathCorpseController subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public class ShooterDeathCorpseController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Damageable _damageable;

    [Header("Visuals")]
    [SerializeField] private GameObject _aliveVisualRoot;
    [SerializeField] private GameObject _corpseVisualRoot;
    [SerializeField] private bool _hideCorpseVisualOnAwake = true;

    [Header("Death Toggle")]
    [SerializeField] private GameObject[] _disableOnDeathRoots;
    [SerializeField] private GameObject[] _enableOnDeathRoots;
    [SerializeField] private Collider[] _disableOnDeathColliders;
    [SerializeField] private Collider[] _enableOnDeathColliders;

    [Header("Corpse Interaction")]
    [Tooltip("Interaction colliders used by corpse looting. Disabled while alive, enabled after death.")]
    [SerializeField] private Collider[] _corpseInteractionColliders;

    [Header("Debug")]
    [SerializeField] private bool _debugLogs;

    private bool _isCorpse;

    protected Damageable Damageable => _damageable;
    protected bool DebugLogsEnabled => _debugLogs;

    // Purpose: Initializes Inspector defaults for this component.
    protected virtual void Reset()
    {
        ResolveRefs();
    }

    // Purpose: Caches the component references required before runtime initialization.
    protected virtual void Awake()
    {
        ResolveRefs();

        if (_hideCorpseVisualOnAwake && _corpseVisualRoot != null)
            _corpseVisualRoot.SetActive(false);

        SetObjectsActive(_enableOnDeathRoots, false);
        SetCollidersEnabled(_enableOnDeathColliders, false);
        SetCollidersEnabled(_corpseInteractionColliders, false);

        LogCorpseState("Awake initialization completed");
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    protected virtual void OnEnable()
    {
        ResolveRefs();

        if (_damageable != null)
            _damageable.OnDie += ConvertToCorpse;

        if (_damageable != null && _damageable.IsDead)
            ConvertToCorpse();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    protected virtual void OnDisable()
    {
        if (_damageable != null)
            _damageable.OnDie -= ConvertToCorpse;
    }

    // Purpose: Converts the current data to to corpse.
    public void ConvertToCorpse()
    {
        if (_isCorpse)
            return;

        _isCorpse = true;

        LogCorpseState("ConvertToCorpse begin");

        BeforeConvertToCorpse();

        if (CanToggleRoot(_corpseVisualRoot))
            _corpseVisualRoot.SetActive(true);

        bool hasCorpseVisual =
            _corpseVisualRoot != null &&
            _corpseVisualRoot.GetComponentInChildren<Renderer>(true) != null;

        if (hasCorpseVisual && CanToggleRoot(_aliveVisualRoot))
            _aliveVisualRoot.SetActive(false);
        else if (!hasCorpseVisual)
            Debug.LogWarning(
                $"[ShooterDeathCorpseController] Corpse visual is missing. Alive visual is preserved. owner={name}",
                this);

        SetObjectsActive(_disableOnDeathRoots, false);
        SetObjectsActive(_enableOnDeathRoots, true);
        SetCollidersEnabled(_disableOnDeathColliders, false);
        SetCollidersEnabled(_enableOnDeathColliders, true);
        SetCollidersEnabled(_corpseInteractionColliders, true);
        if (Application.isPlaying)
        {
            if (_corpseInteractionColliders != null)
                foreach (var collider in _corpseInteractionColliders)
                    if (collider != null) collider.isTrigger = true;
            ShooterBodyCollisionMotor.MakeCorpsePassThrough(this);
        }

        VisionVisibilityTarget visibilityTarget =
            EntityComponentResolver.Resolve<VisionVisibilityTarget>(this);
        visibilityTarget?.RefreshRenderers();

        AfterConvertToCorpse();
        LogCorpseState("ConvertToCorpse completed");
    }

    // Purpose: Performs the before convert to corpse workflow for this component.
    protected virtual void BeforeConvertToCorpse()
    {
    }

    // Purpose: Performs the after convert to corpse workflow for this component.
    protected virtual void AfterConvertToCorpse()
    {
    }

    // Purpose: Resolves the refs needed by this component.
    protected virtual void ResolveRefs()
    {
        if (_damageable == null)
            _damageable = EntityComponentResolver.Resolve<Damageable>(this);
    }

    // Purpose: Updates the objects active state.
    private void SetObjectsActive(GameObject[] targets, bool active)
    {
        if (targets == null)
            return;

        for (int i = 0; i < targets.Length; i++)
        {
            if (CanToggleRoot(targets[i]))
                targets[i].SetActive(active);
        }
    }

    // Purpose: Checks whether toggle root is currently permitted.
    private bool CanToggleRoot(GameObject target)
    {
        if (target == null)
            return false;

        Transform ownerRoot = transform.root;
        if (target.transform == ownerRoot)
        {
            if (_debugLogs)
            {
                Debug.LogWarning(
                    $"[ShooterDeathCorpseController] Refusing to toggle owner root. target={target.name}, active={target.activeSelf}",
                    this);
            }

            return false;
        }

        return true;
    }

    // Purpose: Updates the colliders enabled state.
    private static void SetCollidersEnabled(Collider[] targets, bool enabled)
    {
        if (targets == null)
            return;

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
                targets[i].enabled = enabled;
        }
    }

    // Purpose: Performs the log corpse state workflow for this component.
    private void LogCorpseState(string phase)
    {
        if (!_debugLogs)
            return;

        Debug.Log(
            $"[ShooterDeathCorpseController] {phase}. owner={name}, " +
            $"damageableNull={_damageable == null}, " +
            $"isDead={(_damageable != null && _damageable.IsDead)}, " +
            $"aliveVisual={DescribeObject(_aliveVisualRoot)}, " +
            $"corpseVisual={DescribeObject(_corpseVisualRoot)}, " +
            $"corpseInteractionColliders={DescribeColliders(_corpseInteractionColliders)}",
            this);
    }

    // Purpose: Performs the describe object workflow for this component.
    private static string DescribeObject(GameObject target)
    {
        if (target == null)
            return "null";

        return $"{target.name}(activeSelf={target.activeSelf}, activeInHierarchy={target.activeInHierarchy})";
    }

    // Purpose: Performs the describe colliders workflow for this component.
    private static string DescribeColliders(Collider[] colliders)
    {
        if (colliders == null || colliders.Length == 0)
            return "[]";

        string result = "[";

        for (int i = 0; i < colliders.Length; i++)
        {
            if (i > 0)
                result += ", ";

            Collider collider = colliders[i];

            if (collider == null)
            {
                result += "null";
                continue;
            }

            result +=
                $"{collider.name}(enabled={collider.enabled}, " +
                $"active={collider.gameObject.activeInHierarchy}, " +
                $"trigger={collider.isTrigger}, " +
                $"layer={LayerMask.LayerToName(collider.gameObject.layer)})";
        }

        return result + "]";
    }
}
