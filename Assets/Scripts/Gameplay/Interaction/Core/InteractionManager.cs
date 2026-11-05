/*
 * Role hierarchy: Gameplay > Interaction > Core
 * Primary responsibility: Handles InteractionType interaction, pickup, trade, loot, or storage flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public enum InteractionType
{
    None = 0,
    PickUp = 1,
    Talk = 2,
    Portal = 3,
    QTE = 4,
    Shop = 5,
    Loot = 6,
    Treasure = 7,
    Consumable = 8,
    Reload = 9,
    StageSelect = 10,
    SectorSkip = 11,
    RaidStation = 12,
    SectorLockPanel = 13,
    HomeInfectionPurifier = 14,
    PortalActivation = 15
}

public class InteractionManager : MonoBehaviour
{
    [SerializeField] private InputReader _inputReader = default;
    [SerializeField] private PlayerInteractConfigSO _interactionConfig;

    [Header("Actor")]
    [SerializeField] private Transform _interactionActor;
    [SerializeField] private PlayerStatsRuntime _playerStatsRuntime;

    [Header("Casting")]
    [SerializeField] private PlayerInteractConfigSO _playerInteractConfig;
    [SerializeField] private InteractionCastBarEventChannelSO _interactionCastBarChannel;
    [SerializeField] private bool _hidePromptWhileCasting = true;
    [SerializeField] private bool _debugCastLogs;

    [Tooltip("Logs trigger detection, interaction resolution, loot eligibility, and prompt publication. Enable while diagnosing missing corpse prompts.")]
    [SerializeField] private bool _debugInteractionLogs;

    [Header("Broadcasting on")]
    [SerializeField] private InteractionUIEventChannelSO _toggleInteractionUI = default;

    [Header("Listening to")]
    [SerializeField] private VoidEventChannelSO _onInteractionEnded = default;
    [SerializeField] private PlayableDirectorChannelSO _onCutsceneStart = default;

    [ReadOnly] public InteractionType currentInteractionType;
    [Header("Interaction Prompt")]
    [SerializeField] private InteractionPromptEventChannelSO _interactionPromptChannel;
    [SerializeField] private string _interactionKeyLabel = "E";

    private LinkedList<Interaction> _potentialInteractions = new LinkedList<Interaction>();
    private readonly Dictionary<GameObject, HashSet<GameObject>> _interactionSources = new();
    private ZoneTriggerController _interactionZone;
    private float _nextRangeValidationTime;
    private float _nextPortalInteractAllowedTime;
    private Coroutine _interactionCastRoutine;
    private Interaction _castingInteraction;
    private InteractionPromptAnchor _activePromptAnchor;
    private RaidStation _activePromptStation;
    private Damageable _castDamageable;
    private PlayerAimController _castAim;
    private void CancelForMove(Vector2 input) { if(input.sqrMagnitude>.001f)CancelForAction(); }
    private void CancelForDamage(Damageable target,float damage)
    {
        if(target!=null&&target.IsPeriodicStatusDamage)return;
        CancelForAction();
    }
    private void CancelForAction()
    {
        if(_castingInteraction==null)return;
        CancelInteractionCast();
        RequestUpdateUI(_potentialInteractions.Count>0);
    }
    public void NotifyPortalTravelCompleted() => _nextPortalInteractAllowedTime = 0f;

    private Transform InteractionActor
    {
        get
        {
            if (_interactionActor == null)
                return transform;

            CharacterController controller = _interactionActor.GetComponent<CharacterController>();
            if (controller != null)
                return controller.transform;

            controller = _interactionActor.GetComponentInParent<CharacterController>();
            if (controller != null)
                return controller.transform;

            return _interactionActor;
        }
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if(_inputReader!=null) {
            _inputReader.MoveEvent+=CancelForMove; _inputReader.ShootEvent+=CancelForAction;
            _inputReader.MeleeAttackEvent+=CancelForAction; _inputReader.DashEvent+=CancelForAction;
        }
        _castDamageable=EntityComponentResolver.Resolve<Damageable>(InteractionActor);
        if(_castDamageable!=null)_castDamageable.OnDamaged+=CancelForDamage;
        _potentialInteractions.Clear();
        _interactionSources.Clear();
        ResolveInteractionZone();
        SetActivePromptStation(null);
        SetActivePromptAnchor(null);

        if (_inputReader != null)
            _inputReader.InteractEvent += OnInteractionButtonPress;

        if (_onInteractionEnded != null)
            _onInteractionEnded.OnEventRaised += OnInteractionEnd;

        if (_onCutsceneStart != null)
            _onCutsceneStart.OnEventRaised += ResetPotentialInteractions;
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if(_inputReader!=null) {
            _inputReader.MoveEvent-=CancelForMove; _inputReader.ShootEvent-=CancelForAction;
            _inputReader.MeleeAttackEvent-=CancelForAction; _inputReader.DashEvent-=CancelForAction;
        }
        if(_castDamageable!=null)_castDamageable.OnDamaged-=CancelForDamage;
        SetActivePromptStation(null);
        CancelInteractionCast();
        SetActivePromptAnchor(null);
        _potentialInteractions.Clear();
        _interactionSources.Clear();
        PublishInteractionPrompt(false);

        if (_inputReader != null)
            _inputReader.InteractEvent -= OnInteractionButtonPress;

        if (_onInteractionEnded != null)
            _onInteractionEnded.OnEventRaised -= OnInteractionEnd;

        if (_onCutsceneStart != null)
            _onCutsceneStart.OnEventRaised -= ResetPotentialInteractions;
    }

    private void ResolveInteractionZone()
    {
        if (_interactionZone != null) return;
        _interactionZone = _interactionActor != null
            ? _interactionActor.GetComponent<ZoneTriggerController>() : null;
        if (_interactionZone == null) _interactionZone = GetComponentInChildren<ZoneTriggerController>(true);
    }

    private bool IsTargetInInteractionZone(GameObject owner)
    {
        if (owner == null || !owner.activeInHierarchy) return false;
        ResolveInteractionZone();
        if (_interactionZone == null || !_interactionSources.TryGetValue(owner, out var sources)) return false;
        foreach (GameObject source in sources)
            if (_interactionZone.IsObjectInZone(source)) return true;
        return false;
    }

    private void Update()
    {
        if (_potentialInteractions.Count == 0 || _interactionCastRoutine != null ||
            Time.unscaledTime < _nextRangeValidationTime) return;
        _nextRangeValidationTime = Time.unscaledTime + .15f;
        int previousCount = _potentialInteractions.Count;
        RefreshPotentialInteractions();
        if (previousCount != _potentialInteractions.Count) RequestUpdateUI(_potentialInteractions.Count > 0);
    }

    // Purpose: Collects collect for this workflow.
    private void Collect()
    {
        if (_potentialInteractions.Count == 0)
            return;

        Interaction interaction = _potentialInteractions.First.Value;

        if (interaction.type != InteractionType.PickUp)
            return;

        GameObject itemObject = interaction.interactableObject;
        _interactionSources.Remove(itemObject);
        _potentialInteractions.RemoveFirst();
        Destroy(itemObject);
        RequestUpdateUI(false);
    }

	// Purpose: Responds to the interaction button press callback.
	private void OnInteractionButtonPress()
	{
		if (_playerStatsRuntime != null &&
            _playerStatsRuntime.StatusEffects != null &&
            _playerStatsRuntime.StatusEffects.BlocksActions)
        {
            return;
        }

		Debug.Log($"[InteractionManager] E pressed. potentialCount={_potentialInteractions.Count}");

        if (_interactionCastRoutine != null)
            return;

		if (_potentialInteractions.Count == 0)
			return;

        RefreshPotentialInteractions();

        if (_potentialInteractions.Count == 0)
        {
            RequestUpdateUI(false);
            return;
        }

		Interaction interaction = _potentialInteractions.First.Value;

		Debug.Log($"[InteractionManager] Current interaction type={interaction.type}, obj={interaction.interactableObject}");

        float castSeconds = ResolveInteractionCastSeconds(interaction);

        if (castSeconds > 0f)
        {
            _interactionCastRoutine = StartCoroutine(CastAndExecuteInteraction(interaction, castSeconds));
            return;
        }

        ExecuteInteraction(interaction);
	}

    // Purpose: Performs the execute interaction workflow for this component.
    private void ExecuteInteraction(Interaction interaction)
    {
        if (!IsInteractionStillValid(interaction))
        {
            RefreshPotentialInteractions();
            RequestUpdateUI(_potentialInteractions.Count > 0);
            return;
        }

        currentInteractionType = interaction.type;

        if (interaction.type == InteractionType.PortalActivation)
        {
            var device = interaction.interactableObject.GetComponent<SectorPortalActivator>();
            bool succeeded = device != null && device.TryInteract(InteractionActor);
            currentInteractionType = InteractionType.None;
            RefreshPotentialInteractions();
            RequestUpdateUI(!succeeded && _potentialInteractions.Count > 0);
            return;
        }

        if (interaction.type == InteractionType.HomeInfectionPurifier)
        {
            var purifier = interaction.interactableObject.GetComponent<HomeInfectionPurifier>();
            bool succeeded = purifier != null && purifier.TryInteract(InteractionActor);
            currentInteractionType = InteractionType.None;
            RequestUpdateUI(!succeeded && _potentialInteractions.Count > 0);
            return;
        }

        if (interaction.type == InteractionType.SectorLockPanel)
        {
            var panel = interaction.interactableObject.GetComponent<SectorLockPanel>();
            bool succeeded = panel != null && panel.TryInteract(InteractionActor);
            currentInteractionType = InteractionType.None;
            RequestUpdateUI(!succeeded && _potentialInteractions.Count > 0);
            return;
        }

        if (interaction.type == InteractionType.RaidStation)
        {
            RaidStation station = interaction.interactableObject != null ? interaction.interactableObject.GetComponent<RaidStation>() : null;
            bool succeeded = station != null && station.TryInteract(InteractionActor);
            currentInteractionType = InteractionType.None;
            RequestUpdateUI(!succeeded && _potentialInteractions.Count > 0);
            return;
        }

		switch (interaction.type)
		{
			case InteractionType.Portal:
				TryUsePortal(interaction);
				break;

			case InteractionType.PickUp:
                TryUsePickup(interaction);
				break;

            case InteractionType.Talk:
                TryUseQuestNpc(interaction);
                break;

            case InteractionType.StageSelect:
                TryUseHideoutStageSelectStation(interaction);
                break;

            case InteractionType.SectorSkip:
                TryUseSectorSkipStation(interaction);
                break;

            case InteractionType.Shop:
                TryUseShop(interaction);
                break;

            case InteractionType.Loot:
                TryUseLoot(interaction);
                break;

            case InteractionType.Treasure:
                TryUseTreasure(interaction);
                break;

            case InteractionType.QTE:
                TryUseQTEStation(interaction);
                break;
		}
	}

    // Purpose: Performs the cast and execute interaction workflow for this component.
    private IEnumerator CastAndExecuteInteraction(Interaction interaction, float castSeconds)
    {
        _castingInteraction = interaction;

        if (_hidePromptWhileCasting)
            PublishInteractionPrompt(false);

        Transform actor = InteractionActor;
        var damageable=EntityComponentResolver.Resolve<Damageable>(actor);
        if(damageable!=_castDamageable)
        {
            if(_castDamageable!=null)_castDamageable.OnDamaged-=CancelForDamage;
            _castDamageable=damageable;
            if(_castDamageable!=null)_castDamageable.OnDamaged+=CancelForDamage;
        }
        _castAim=EntityComponentResolver.Resolve<PlayerAimController>(actor);
        if(_castAim!=null)_castAim.OnShotConsumed+=CancelForAction; // Also catches held automatic fire, not only the initial input press.
        Transform anchor = ResolveCastBarAnchor(actor, interaction.interactableObject);
        Vector3 castPosition=actor!=null?actor.position:Vector3.zero;
        float startTime = Time.time;
        float duration = Mathf.Max(0f, castSeconds);

        if (_debugCastLogs)
        {
            Debug.Log(
                $"[InteractionManager] Cast started. type={interaction.type}, seconds={duration:0.###}, obj={interaction.interactableObject}",
                this);
        }

        while (Time.time < startTime + duration)
        {
            if (!IsInteractionStillValid(interaction) || (actor!=null && (actor.position-castPosition).sqrMagnitude>.0025f))
            {
                if (_debugCastLogs)
                {
                    Debug.Log(
                        $"[InteractionManager] Cast cancelled. type={interaction.type}, obj={interaction.interactableObject}",
                        this);
                }

                ClearInteractionCastState();
                RequestUpdateUI(_potentialInteractions.Count > 0);
                yield break;
            }

            float progress = duration > 0f
                ? Mathf.Clamp01((Time.time - startTime) / duration)
                : 1f;

            PublishInteractionCast(interaction, actor, anchor, progress, duration);
            yield return null;
        }

        // Revalidate after the final yield too (death, movement or availability can change on this frame).
        if (!IsInteractionStillValid(interaction) || (damageable != null && damageable.IsDead) ||
            (actor != null && (actor.position - castPosition).sqrMagnitude > .0025f))
        {
            ClearInteractionCastState();
            RequestUpdateUI(_potentialInteractions.Count > 0);
            yield break;
        }
        PublishInteractionCast(interaction, actor, anchor, 1f, duration);

        if (_debugCastLogs)
        {
            Debug.Log(
                $"[InteractionManager] Cast completed. type={interaction.type}, obj={interaction.interactableObject}",
                this);
        }

        ClearInteractionCastState();
        ExecuteInteraction(interaction);
    }

    // Purpose: Attempts to use portal and reports whether the request succeeded.
    private void TryUsePortal(Interaction interaction)
    {
        if (interaction.interactableObject == null)
            return;

        if (Time.time < _nextPortalInteractAllowedTime)
            return;

        SectorPortal portal = interaction.interactableObject.GetComponent<SectorPortal>();
        if (portal == null)
            portal = interaction.interactableObject.GetComponentInParent<SectorPortal>();

        if (portal == null)
            return;

        bool moved = portal.TryInteract(InteractionActor);

        if (moved && !portal.HasPendingKeyConfirmation)
        {
            // Movement completion, not accepted loading/key UI, starts the shared cooldown.
            currentInteractionType = InteractionType.None;
            _potentialInteractions.Clear();
            _interactionSources.Clear();
            RequestUpdateUI(false);
        }
    }

    // Purpose: Attempts to use quest npc and reports whether the request succeeded.
    private void TryUseQuestNpc(Interaction interaction)
    {
        if (interaction.interactableObject == null)
            return;

        HomeQuestNpc questNpc =
            interaction.interactableObject.GetComponent<HomeQuestNpc>() ??
            interaction.interactableObject.GetComponentInParent<HomeQuestNpc>();

        if (questNpc == null || !questNpc.CanInteract)
            return;

        questNpc.TryInteract(InteractionActor);
    }

    // Purpose: Attempts to use hideout stage select station and reports whether the request succeeded.
    private void TryUseHideoutStageSelectStation(Interaction interaction)
    {
        if (interaction.interactableObject == null)
            return;

        HideoutStageSelectStation stageSelectStation =
            interaction.interactableObject.GetComponent<HideoutStageSelectStation>() ??
            interaction.interactableObject.GetComponentInParent<HideoutStageSelectStation>();

        if (stageSelectStation == null || !stageSelectStation.CanInteract)
            return;

        if (stageSelectStation.TryInteract(InteractionActor))
        {
            currentInteractionType = InteractionType.None;
            RequestUpdateUI(false);
        }
    }

    // Purpose: Attempts to use sector skip station and reports whether the request succeeded.
    private void TryUseSectorSkipStation(Interaction interaction)
    {
        if (interaction.interactableObject == null)
            return;

        SectorSkipStation sectorSkipStation =
            interaction.interactableObject.GetComponent<SectorSkipStation>() ??
            interaction.interactableObject.GetComponentInParent<SectorSkipStation>();

        if (sectorSkipStation == null || !sectorSkipStation.CanInteract)
            return;

        if (sectorSkipStation.TryInteract(this))
        {
            currentInteractionType = InteractionType.None;
            RequestUpdateUI(false);
        }
    }

    // Purpose: Attempts to use pickup and reports whether the request succeeded.
    private void TryUsePickup(Interaction interaction)
    {
        if (interaction.interactableObject == null)
            return;

        GameObject pickupObject = interaction.interactableObject;

        WorldItemPickup itemPickup =
            pickupObject.GetComponent<WorldItemPickup>() ??
            pickupObject.GetComponentInParent<WorldItemPickup>();

        if (itemPickup == null || !itemPickup.CanInteract)
            return;

        bool picked = itemPickup.TryPickup(InteractionActor);

        if (!picked)
            return;

        currentInteractionType = InteractionType.None;
        RemovePotentialInteraction(pickupObject, removeAllSources: true);
    }

    // Purpose: Attempts to use shop and reports whether the request succeeded.
    private void TryUseShop(Interaction interaction)
    {
        if (interaction.interactableObject == null)
            return;

        SectorShop shop =
            interaction.interactableObject.GetComponent<SectorShop>() ??
            interaction.interactableObject.GetComponentInParent<SectorShop>();

        if (shop == null || !shop.CanInteract)
            return;

        if (shop.TryInteract(InteractionActor))
        {
            currentInteractionType = InteractionType.None;
            RequestUpdateUI(false);
        }
    }

    // Purpose: Attempts to use loot and reports whether the request succeeded.
    private void TryUseLoot(Interaction interaction)
    {
        if (interaction.interactableObject == null)
            return;

        LootableObject lootableObject =
            interaction.interactableObject.GetComponent<LootableObject>() ??
            interaction.interactableObject.GetComponentInParent<LootableObject>();

        if (lootableObject != null)
        {
            if (lootableObject.TryInteract(InteractionActor))
            {
                currentInteractionType = InteractionType.None;
                RequestUpdateUI(false);
            }

            return;
        }

        EnemyLootInventoryRuntime loot =
            EntityComponentResolver.Resolve<EnemyLootInventoryRuntime>(
                interaction.interactableObject.transform);

        if (loot == null)
        {
            if (_debugInteractionLogs)
            {
                Debug.LogError(
                    $"[InteractionManager] Loot execution failed because runtime could not be resolved. " +
                    $"obj={BuildHierarchyPath(interaction.interactableObject.transform)}",
                    interaction.interactableObject);
            }

            return;
        }

        if (!loot.CanInteract)
        {
            if (_debugInteractionLogs)
            {
                Debug.LogWarning(
                    $"[InteractionManager] Loot execution blocked. {loot.BuildInteractionDebugInfo()}",
                    loot);
            }

            return;
        }

        if (loot.TryInteract(InteractionActor))
        {
            currentInteractionType = InteractionType.None;
            RequestUpdateUI(false);
        }
    }

    // Purpose: Attempts to use treasure and reports whether the request succeeded.
    private void TryUseTreasure(Interaction interaction)
    {
        if (interaction.interactableObject == null)
            return;

        SectorTreasure treasure =
            interaction.interactableObject.GetComponent<SectorTreasure>() ??
            interaction.interactableObject.GetComponentInParent<SectorTreasure>() ??
            interaction.interactableObject.GetComponentInChildren<SectorTreasure>(true);

        if (treasure == null || !treasure.CanInteract)
            return;

        if (!treasure.TryInteract(InteractionActor))
            return;

        currentInteractionType = InteractionType.None;
        // Structure loot remains inspectable after opening, including when its
        // single slot is empty. Keep the trigger registered for later re-open.
        RequestUpdateUI(false);
    }

    // Purpose: Attempts to use qte station and reports whether the request succeeded.
    private void TryUseQTEStation(Interaction interaction)
    {
        if (interaction.interactableObject == null)
            return;

        MutarusQTEStation station =
            interaction.interactableObject.GetComponent<MutarusQTEStation>() ??
            interaction.interactableObject.GetComponentInParent<MutarusQTEStation>();

        if (station == null)
            return;
        Debug.Log($"[InteractionManager] Trying to use QTE station. station={station.name}");
        bool started = station.TryInteract();

        if (started)
        {
            Debug.Log($"[InteractionManager] Started QTE station. station={station.name}");
            currentInteractionType = InteractionType.None;
            //_potentialInteractions.Clear();
            RequestUpdateUI(false);
        }
    }
	// Purpose: Responds to the trigger change detected callback.
	public void OnTriggerChangeDetected(bool entered, GameObject obj)
	{
		if (_debugInteractionLogs)
        {
            Collider collider = obj != null ? obj.GetComponent<Collider>() : null;
            Debug.Log(
                $"[InteractionManager] Trigger {(entered ? "ENTER" : "EXIT")}. " +
                $"obj={BuildHierarchyPath(obj != null ? obj.transform : null)}, " +
                $"layer={(obj != null ? LayerMask.LayerToName(obj.layer) : "null")}, " +
                $"active={(obj != null && obj.activeInHierarchy)}, " +
                $"collider={(collider != null ? collider.GetType().Name : "none")}, " +
                $"colliderEnabled={(collider != null && collider.enabled)}, " +
                $"isTrigger={(collider != null && collider.isTrigger)}, " +
                $"potentialCount={_potentialInteractions.Count}",
                this);
        }

		if (entered)
			AddPotentialInteraction(obj);
		else
			RemovePotentialInteraction(obj);
	}

    // Purpose: Adds the requested potential interaction to this component.
    private void AddPotentialInteraction(GameObject obj)
    {
        if (!TryCreateInteraction(
                obj,
                out Interaction newPotentialInteraction,
                _debugInteractionLogs))
        {
            return;
        }

        if (ContainsInteractionObject(newPotentialInteraction.interactableObject))
        {
            RegisterInteractionSource(newPotentialInteraction.interactableObject, obj);
            if (_debugInteractionLogs)
            {
                Debug.Log(
                    $"[InteractionManager] Interaction already registered. " +
                    $"type={newPotentialInteraction.type}, " +
                    $"obj={BuildHierarchyPath(newPotentialInteraction.interactableObject.transform)}",
                    this);
            }

            return;
        }

        RegisterInteractionSource(newPotentialInteraction.interactableObject, obj);
        _potentialInteractions.AddFirst(newPotentialInteraction);

        if (_debugInteractionLogs)
        {
            Debug.Log(
                $"[InteractionManager] Interaction added. " +
                $"type={newPotentialInteraction.type}, " +
                $"obj={BuildHierarchyPath(newPotentialInteraction.interactableObject.transform)}, " +
                $"potentialCount={_potentialInteractions.Count}",
                this);
        }

        RequestUpdateUI(true);
    }

    // Purpose: Removes the requested potential interaction from this component.
    private void RegisterInteractionSource(GameObject owner, GameObject source)
    {
        if (!_interactionSources.TryGetValue(owner, out var sources))
        {
            sources = new HashSet<GameObject>();
            _interactionSources.Add(owner, sources);
        }
        sources.Add(source);
    }

    private void RemovePotentialInteraction(GameObject obj, bool removeAllSources = false)
    {
        GameObject normalizedObject = ResolveInteractableObject(obj);
        // Exit uses the exact owner remembered at Enter, even if components were
        // disabled/reparented meanwhile. One child Exit must not clear its siblings.
        if (!removeAllSources)
        {
            foreach (var pair in _interactionSources)
                if (pair.Value.Contains(obj)) { normalizedObject = pair.Key; break; }
        }
        if (normalizedObject == null) return;
        if (_interactionSources.TryGetValue(normalizedObject, out var sources))
        {
            if (!removeAllSources)
            {
                ResolveInteractionZone();
                // Multiple colliders can share one GameObject; an Exit from one
                // collider does not invalidate another still inside the detector.
                if (_interactionZone == null || !_interactionZone.IsObjectInZone(obj)) sources.Remove(obj);
                if (IsTargetInInteractionZone(normalizedObject)) return;
            }
            _interactionSources.Remove(normalizedObject);
        }

        if (_castingInteraction != null &&
            _castingInteraction.interactableObject == normalizedObject)
        {
            CancelInteractionCast();
        }

        LinkedListNode<Interaction> currentNode = _potentialInteractions.First;
        while (currentNode != null)
        {
            LinkedListNode<Interaction> nextNode = currentNode.Next;

            if (currentNode.Value.interactableObject == normalizedObject)
            {
                _potentialInteractions.Remove(currentNode);
                break;
            }

            currentNode = nextNode;
        }

        RequestUpdateUI(_potentialInteractions.Count > 0);

        if (_debugInteractionLogs)
        {
            Debug.Log(
                $"[InteractionManager] Interaction removed. " +
                $"trigger={BuildHierarchyPath(obj != null ? obj.transform : null)}, " +
                $"normalized={BuildHierarchyPath(normalizedObject != null ? normalizedObject.transform : null)}, " +
                $"potentialCount={_potentialInteractions.Count}",
                this);
        }
    }

    // Presentation preview only: never registers a potential interaction or
    // changes the E-key trigger range/selected target.
    public bool TryResolveNearbyPrompt(GameObject obj, out Transform anchor)
    {
        anchor = null;
        if (!TryCreateInteraction(obj, out Interaction interaction)) return false;
        if (interaction.interactableObject == null || !interaction.interactableObject.activeInHierarchy) return false;
        anchor = ResolvePromptAnchor(interaction.interactableObject);
        return anchor != null;
    }

    public bool CanInteractWithTarget(GameObject owner)
    {
        return isActiveAndEnabled && IsTargetInInteractionZone(owner) &&
            TryCreateInteraction(owner, out Interaction interaction) && interaction.interactableObject == owner;
    }

	// Purpose: Attempts to create interaction and reports whether the request succeeded.
	private bool TryCreateInteraction(
        GameObject obj,
        out Interaction interaction,
        bool logDecision = false)
	{
		interaction = null;
        // Registration, refresh and removal must resolve the same canonical owner.
        obj = ResolveInteractableObject(obj);

		if (obj == null)
		{
			if (logDecision)
                Debug.LogWarning("[InteractionManager] Interaction rejected because trigger object is null.", this);

			return false;
		}

		SectorPortal portal = obj.GetComponentInParent<SectorPortal>();
        var activator = obj.GetComponentInParent<SectorPortalActivator>();
        if (activator != null && activator.CanInteract)
        {
            interaction = new Interaction(InteractionType.PortalActivation, activator.gameObject);
            LogResolvedInteraction(logDecision, interaction, obj);
            return true;
        }

		//Debug.Log($"[InteractionManager] TryCreateInteraction obj={obj.name}, portal={portal}, portalCanInteract={(portal != null ? portal.CanInteract.ToString() : "null")}");

		if (portal != null && portal.CanInteract)
		{
			interaction = new Interaction(InteractionType.Portal, portal.gameObject);
			LogResolvedInteraction(logDecision, interaction, obj);
			return true;
		}
        MutarusQTEStation qteStation = obj.GetComponentInParent<MutarusQTEStation>();

        if (qteStation != null && qteStation.CanInteract)
        {
            interaction = new Interaction(InteractionType.QTE, qteStation.gameObject);
            LogResolvedInteraction(logDecision, interaction, obj);
            return true;
        }

        WorldItemPickup itemPickup = obj.GetComponentInParent<WorldItemPickup>();

        if (itemPickup != null && itemPickup.CanInteract)
        {
            interaction = new Interaction(InteractionType.PickUp, itemPickup.gameObject);
            LogResolvedInteraction(logDecision, interaction, obj);
            return true;
        }

        SectorTreasure treasure = obj.GetComponentInParent<SectorTreasure>();

        if (treasure != null && treasure.CanInteract)
        {
            interaction = new Interaction(InteractionType.Treasure, treasure.gameObject);
            LogResolvedInteraction(logDecision, interaction, obj);
            return true;
        }

        var lockPanel = obj.GetComponentInParent<SectorLockPanel>();
        if (lockPanel != null && lockPanel.CanInteract)
        {
            interaction = new Interaction(InteractionType.SectorLockPanel, lockPanel.gameObject);
            return true;
        }

        var purifier = obj.GetComponentInParent<HomeInfectionPurifier>();
        if (purifier != null && purifier.CanInteract)
        {
            interaction = new Interaction(InteractionType.HomeInfectionPurifier, purifier.gameObject);
            return true;
        }

        RaidStation raidStation = obj.GetComponentInParent<RaidStation>();
        if (raidStation != null && raidStation.CanInteract)
        {
            interaction = new Interaction(InteractionType.RaidStation, raidStation.gameObject);
            return true;
        }

        SectorSkipStation sectorSkipStation = obj.GetComponentInParent<SectorSkipStation>();

        if (sectorSkipStation != null && sectorSkipStation.CanInteract)
        {
            interaction = new Interaction(InteractionType.SectorSkip, sectorSkipStation.gameObject);
            LogResolvedInteraction(logDecision, interaction, obj);
            return true;
        }

        HomeQuestNpc questNpc = obj.GetComponentInParent<HomeQuestNpc>();

        if (questNpc != null && questNpc.CanInteract)
        {
            interaction = new Interaction(InteractionType.Talk, questNpc.gameObject);
            LogResolvedInteraction(logDecision, interaction, obj);
            return true;
        }

        // Authored world containers use LootableObject rather than the corpse
        // inventory hierarchy. Resolve them before the generic enemy-loot path
        // so placed chests participate in the same E-key prompt and LootHub.
        LootableObject lootableObject = obj.GetComponentInParent<LootableObject>();

        if (lootableObject != null && lootableObject.CanInteract)
        {
            interaction = new Interaction(InteractionType.Loot, lootableObject.gameObject);
            LogResolvedInteraction(logDecision, interaction, obj);
            return true;
        }

        // Corpse interaction colliders and loot runtime are sibling children under
        // the shooter root, so parent-only lookup cannot resolve corpse loot.
        EnemyLootInventoryRuntime loot =
            EntityComponentResolver.Resolve<EnemyLootInventoryRuntime>(obj.transform);

        if (loot != null && loot.CanInteract)
        {
            interaction = new Interaction(InteractionType.Loot, loot.gameObject);
            LogResolvedInteraction(logDecision, interaction, obj);
            return true;
        }

        if (logDecision && loot != null)
        {
            Debug.LogWarning(
                $"[InteractionManager] Loot runtime found but interaction is blocked. " +
                $"trigger={BuildHierarchyPath(obj.transform)}, {loot.BuildInteractionDebugInfo()}",
                loot);
        }

        HideoutStageSelectStation stageSelectStation =
            obj.GetComponentInParent<HideoutStageSelectStation>();

        if (stageSelectStation != null && stageSelectStation.CanInteract)
        {
            interaction = new Interaction(
                InteractionType.StageSelect,
                stageSelectStation.gameObject);
            LogResolvedInteraction(logDecision, interaction, obj);
            return true;
        }

        SectorShop shop = obj.GetComponentInParent<SectorShop>();

        if (shop != null && shop.CanInteract)
        {
            interaction = new Interaction(InteractionType.Shop, shop.gameObject);
            LogResolvedInteraction(logDecision, interaction, obj);
            return true;
        }

		if (logDecision)
        {
            Debug.LogWarning(
                $"[InteractionManager] No valid interaction resolved. " +
                $"trigger={BuildHierarchyPath(obj.transform)}, " +
                $"portal={(portal != null)}, qte={(qteStation != null)}, " +
                $"pickup={(itemPickup != null)}, treasure={(treasure != null)}, " +
                $"loot={(loot != null)}, stageSelect={(stageSelectStation != null)}, " +
                $"shop={(shop != null)}",
                obj);
        }

		return false;
	}

    // Purpose: Resolves the interactable object needed by this component.
    private GameObject ResolveInteractableObject(GameObject obj)
    {
        if (obj == null)
            return null;

        var activator = obj.GetComponentInParent<SectorPortalActivator>();
        if (activator != null) return activator.gameObject;

        SectorPortal portal = obj.GetComponentInParent<SectorPortal>();
        if (portal != null)
            return portal.gameObject;

        MutarusQTEStation qteStation = obj.GetComponentInParent<MutarusQTEStation>();
        if (qteStation != null)
            return qteStation.gameObject;

        WorldItemPickup itemPickup = obj.GetComponentInParent<WorldItemPickup>();
        if (itemPickup != null)
            return itemPickup.gameObject;

        SectorTreasure treasure = obj.GetComponentInParent<SectorTreasure>();
        if (treasure != null)
            return treasure.gameObject;

        SectorLockPanel lockPanel = obj.GetComponentInParent<SectorLockPanel>();
        if (lockPanel != null) return lockPanel.gameObject;

        var purifier = obj.GetComponentInParent<HomeInfectionPurifier>();
        if (purifier != null) return purifier.gameObject;

        RaidStation raidStation = obj.GetComponentInParent<RaidStation>();
        if (raidStation != null) return raidStation.gameObject;

        SectorSkipStation sectorSkipStation = obj.GetComponentInParent<SectorSkipStation>();
        if (sectorSkipStation != null)
            return sectorSkipStation.gameObject;

        HomeQuestNpc questNpc = obj.GetComponentInParent<HomeQuestNpc>();
        if (questNpc != null) return questNpc.gameObject;

        LootableObject lootableObject = obj.GetComponentInParent<LootableObject>();
        if (lootableObject != null) return lootableObject.gameObject;

        EnemyLootInventoryRuntime loot =
            EntityComponentResolver.Resolve<EnemyLootInventoryRuntime>(obj.transform);
        if (loot != null)
            return loot.gameObject;

        HideoutStageSelectStation stageSelectStation =
            obj.GetComponentInParent<HideoutStageSelectStation>();
        if (stageSelectStation != null)
            return stageSelectStation.gameObject;

        SectorShop shop = obj.GetComponentInParent<SectorShop>();
        if (shop != null)
            return shop.gameObject;

        return obj;
    }

    // Purpose: Performs the contains interaction object workflow for this component.
    private bool ContainsInteractionObject(GameObject obj)
    {
        LinkedListNode<Interaction> currentNode = _potentialInteractions.First;
        while (currentNode != null)
        {
            if (currentNode.Value.interactableObject == obj)
                return true;

            currentNode = currentNode.Next;
        }

        return false;
    }

    // Purpose: Requests update ui from the owning service.
    private void RequestUpdateUI(bool visible)
    {
        if (visible)
            RefreshPotentialInteractions();

        bool shouldShow = visible && _potentialInteractions.Count > 0;

        if (_toggleInteractionUI != null)
        {
            if (shouldShow)
                _toggleInteractionUI.RaiseEvent(true, _potentialInteractions.First.Value.type);
            else
                _toggleInteractionUI.RaiseEvent(false, InteractionType.None);
        }

        PublishInteractionPrompt(shouldShow);
    }
    // Purpose: Responds to the interaction end callback.
    private void OnInteractionEnd()
    {
        switch (currentInteractionType)
        {
            case InteractionType.Talk:
                RequestUpdateUI(true);
                break;
        }

        if (_inputReader != null)
            _inputReader.EnableGameplayInput();
    }

    // Purpose: Performs the reset potential interactions workflow for this component.
    private void ResetPotentialInteractions(PlayableDirector playableDirector)
    {
        CancelInteractionCast();
        _potentialInteractions.Clear();
        _interactionSources.Clear();
        RequestUpdateUI(false);
    }

    // Purpose: Publishes the interaction prompt notification.
    private void PublishInteractionPrompt(bool visible)
    {
        Interaction selectedInteraction = _castingInteraction;
        if (selectedInteraction == null && visible && _potentialInteractions.Count > 0)
            selectedInteraction = _potentialInteractions.First.Value;

        InteractionPromptAnchor selectedAnchor = selectedInteraction != null
            ? ResolvePromptAnchorComponent(selectedInteraction.interactableObject)
            : null;
        SetActivePromptAnchor(selectedAnchor);
        SetActivePromptStation(selectedInteraction?.type == InteractionType.RaidStation &&
            selectedInteraction.interactableObject != null ? selectedInteraction.interactableObject.GetComponent<RaidStation>() : null);

        if (_interactionPromptChannel == null)
            return;

        if (!visible || _potentialInteractions.Count <= 0)
        {
            _interactionPromptChannel.Clear();
            return;
        }

        Interaction interaction = _potentialInteractions.First.Value;
        Transform anchor = ResolvePromptAnchor(interaction.interactableObject);

        if (anchor == null)
        {
            if (_debugInteractionLogs)
            {
                Debug.LogWarning(
                    $"[InteractionManager] Prompt hidden because no anchor was resolved. " +
                    $"type={interaction.type}, " +
                    $"obj={BuildHierarchyPath(interaction.interactableObject.transform)}",
                    interaction.interactableObject);
            }

            _interactionPromptChannel.Clear();
            return;
        }

        if (_debugInteractionLogs)
        {
            Debug.Log(
                $"[InteractionManager] Prompt published. type={interaction.type}, " +
                $"obj={BuildHierarchyPath(interaction.interactableObject.transform)}, " +
                $"anchor={BuildHierarchyPath(anchor)}",
                interaction.interactableObject);
        }

        _interactionPromptChannel.RaiseEvent(new InteractionPromptSnapshot(
            true,
            interaction.type,
            anchor,
            ResolveInteractionKeyLabel(),
            ResolveActionLabel(interaction)));
    }

    private void SetActivePromptStation(RaidStation station)
    {
        if (_activePromptStation == station) return;
        if (!ReferenceEquals(_activePromptStation, null)) _activePromptStation.PromptChanged -= HandleStationPromptChanged;
        _activePromptStation = station;
        if (_activePromptStation != null) _activePromptStation.PromptChanged += HandleStationPromptChanged;
    }

    private void HandleStationPromptChanged()
    {
        if (_interactionCastRoutine != null && _hidePromptWhileCasting) return;
        RequestUpdateUI(_potentialInteractions.Count > 0);
    }

    // Purpose: Resolves the prompt anchor needed by this component.
    private Transform ResolvePromptAnchor(GameObject obj)
    {
        if (obj == null)
            return null;

        InteractionPromptAnchor anchor = ResolvePromptAnchorComponent(obj);

        if (anchor != null)
            return anchor.Anchor;

        return obj.transform;
    }

    // Purpose: Resolves the prompt anchor component needed by this component.
    private static InteractionPromptAnchor ResolvePromptAnchorComponent(GameObject obj)
    {
        return obj != null
            ? EntityComponentResolver.Resolve<InteractionPromptAnchor>(obj.transform)
            : null;
    }

    // Purpose: Updates the active prompt anchor state.
    private void SetActivePromptAnchor(InteractionPromptAnchor nextAnchor)
    {
        if (_activePromptAnchor == nextAnchor)
        {
            if (_activePromptAnchor != null)
                _activePromptAnchor.SetInteractDotVisible(true);

            return;
        }

        if (_activePromptAnchor != null)
            _activePromptAnchor.SetInteractDotVisible(false);

        _activePromptAnchor = nextAnchor;

        if (_activePromptAnchor != null)
            _activePromptAnchor.SetInteractDotVisible(true);
    }

    // Purpose: Resolves the cast bar anchor needed by this component.
    private Transform ResolveCastBarAnchor(Transform actor, GameObject interactableObject)
    {
        if (actor != null)
        {
            InteractionPromptAnchor actorAnchor =
                actor.GetComponentInChildren<InteractionPromptAnchor>(true) ??
                actor.GetComponentInParent<InteractionPromptAnchor>();

            if (actorAnchor != null)
                return actorAnchor.Anchor;

            return actor;
        }

        return ResolvePromptAnchor(interactableObject);
    }

    // Purpose: Resolves the interaction cast seconds needed by this component.
    private float ResolveInteractionCastSeconds(Interaction interaction)
    {
        if (interaction == null || interaction.interactableObject == null)
            return 0f;

        if (interaction.type == InteractionType.Portal) return Mathf.Max(0f, SectorTravelRulesSO.Active?.travel.portalCast ?? 5f);
        if (interaction.type == InteractionType.PortalActivation) return Mathf.Max(0f, SectorTravelRulesSO.Active?.travel.portalActivationCast ?? 5f);

        PlayerInteractConfigSO config = ResolvePlayerInteractConfig();
        return config != null
            ? config.ResolveCastSeconds(interaction.type)
            : 0.2f;
    }

    // Purpose: Resolves the player interact config needed by this component.
    private PlayerInteractConfigSO ResolvePlayerInteractConfig()
    {
        if (_playerInteractConfig != null)
            return _playerInteractConfig;

        if (_playerStatsRuntime == null)
            _playerStatsRuntime = EntityComponentResolver.Resolve<PlayerStatsRuntime>(this);

        if (_playerStatsRuntime != null &&
            _playerStatsRuntime.StatConfig is PlayerStatsConfigSO playerConfig)
        {
            return playerConfig.InteractConfig;
        }

        return null;
    }

    // Purpose: Checks whether interaction still valid is currently true.
    private bool IsInteractionStillValid(Interaction interaction)
    {
        if (interaction == null || !IsTargetInInteractionZone(interaction.interactableObject))
            return false;

        if (!TryCreateInteraction(interaction.interactableObject, out Interaction refreshed))
            return false;

        return refreshed.type == interaction.type &&
               refreshed.interactableObject == interaction.interactableObject;
    }

    // Purpose: Publishes the interaction cast notification.
    private void PublishInteractionCast(
        Interaction interaction,
        Transform actor,
        Transform anchor,
        float progress01,
        float durationSeconds)
    {
        if (_interactionCastBarChannel == null || interaction == null)
            return;

        _interactionCastBarChannel.RaiseEvent(new InteractionCastBarSnapshot(
            true,
            interaction.type,
            actor,
            anchor,
            progress01,
            durationSeconds));
    }

    // Purpose: Checks whether cancel interaction cast is currently permitted.
    private void CancelInteractionCast()
    {
        if (_interactionCastRoutine != null)
        {
            StopCoroutine(_interactionCastRoutine);
            _interactionCastRoutine = null;
        }

        ClearInteractionCastState();
    }

    // Purpose: Clears the current interaction cast state state.
    private void ClearInteractionCastState()
    {
        if(_castAim!=null)_castAim.OnShotConsumed-=CancelForAction;
        _castAim=null;
        _interactionCastRoutine = null;
        _castingInteraction = null;
        SetActivePromptAnchor(null);
        currentInteractionType = InteractionType.None;
        _interactionCastBarChannel?.Clear();
    }

    // Purpose: Resolves the interaction key label needed by this component.
    private string ResolveInteractionKeyLabel()
    {
        return string.IsNullOrWhiteSpace(_interactionKeyLabel)
            ? "E"
            : _interactionKeyLabel;
    }

    // Purpose: Resolves the action label needed by this component.
    private string ResolveActionLabel(Interaction interaction)
    {
        if (interaction?.type == InteractionType.PortalActivation) return "포탈 개방";
        if (interaction?.type == InteractionType.SectorLockPanel && interaction.interactableObject != null)
            return interaction.interactableObject.GetComponent<SectorLockPanel>()?.PromptLabel ?? string.Empty;
        if (interaction?.type == InteractionType.HomeInfectionPurifier && interaction.interactableObject != null)
            return interaction.interactableObject.GetComponent<HomeInfectionPurifier>()?.PromptLabel ?? string.Empty;
        if (interaction?.type == InteractionType.RaidStation && interaction.interactableObject != null)
            return interaction.interactableObject.GetComponent<RaidStation>()?.PromptLabel ?? string.Empty;
        if (interaction != null &&
            interaction.type == InteractionType.StageSelect &&
            interaction.interactableObject != null)
        {
            HideoutStageSelectStation stageSelectStation =
                interaction.interactableObject.GetComponent<HideoutStageSelectStation>() ??
                interaction.interactableObject.GetComponentInParent<HideoutStageSelectStation>();

            if (stageSelectStation != null)
                return stageSelectStation.PromptLabel;
        }

        if (interaction != null &&
            interaction.type == InteractionType.PickUp &&
            interaction.interactableObject != null)
        {
            WorldItemPickup itemPickup =
                interaction.interactableObject.GetComponent<WorldItemPickup>() ??
                interaction.interactableObject.GetComponentInParent<WorldItemPickup>();

            if (itemPickup != null)
                return itemPickup.DisplayLabel;
        }

        if (interaction != null &&
            interaction.type == InteractionType.SectorSkip &&
            interaction.interactableObject != null)
        {
            SectorSkipStation sectorSkipStation =
                interaction.interactableObject.GetComponent<SectorSkipStation>() ??
                interaction.interactableObject.GetComponentInParent<SectorSkipStation>();

            if (sectorSkipStation != null)
                return sectorSkipStation.PromptLabel;
        }

        if (interaction?.type == InteractionType.Portal && interaction.interactableObject != null)
        {
            var portal = interaction.interactableObject.GetComponentInParent<SectorPortal>();
            if (portal != null && portal.IsExtraction)
                return UIRuntimeLocalization.Get("UI_Map", "Map_Extract", "탈출");
        }

        return interaction != null ? interaction.type switch
        {
            InteractionType.Portal => "섹터 이동",
            InteractionType.QTE => "",
            InteractionType.PickUp => "",
            InteractionType.Shop => "",
            InteractionType.Loot => "",
            InteractionType.Treasure => "",
            InteractionType.Talk => "",
            InteractionType.StageSelect => "",
            InteractionType.SectorSkip => "Skip Sector",
            _ => ""
        } : "";
    }

    // Purpose: Refreshes the displayed or cached potential interactions.
    private void RefreshPotentialInteractions()
    {
        LinkedListNode<Interaction> node = _potentialInteractions.First;

        while (node != null)
        {
            LinkedListNode<Interaction> next = node.Next;
            GameObject obj = node.Value.interactableObject;

            if (!IsTargetInInteractionZone(obj) || !TryCreateInteraction(obj, out Interaction refreshedInteraction))
            {
                _potentialInteractions.Remove(node);
                if (!ReferenceEquals(obj, null)) _interactionSources.Remove(obj);
            }
            else
                node.Value = refreshedInteraction;

            node = next;
        }
    }

    // Purpose: Resolves the portal interact cooldown needed by this component.
    private float ResolvePortalInteractCooldown()
    {
        if (_interactionConfig == null)
            return 1f;

        return _interactionConfig._defaultCastSeconds;
    }

    // Purpose: Performs the log resolved interaction workflow for this component.
    private void LogResolvedInteraction(
        bool enabled,
        Interaction interaction,
        GameObject triggerObject)
    {
        if (!enabled || interaction == null || interaction.interactableObject == null)
            return;

        Debug.Log(
            $"[InteractionManager] Interaction resolved. type={interaction.type}, " +
            $"trigger={BuildHierarchyPath(triggerObject != null ? triggerObject.transform : null)}, " +
            $"owner={BuildHierarchyPath(interaction.interactableObject.transform)}",
            interaction.interactableObject);
    }

    // Purpose: Builds the hierarchy path from the current authored data.
    private static string BuildHierarchyPath(Transform target)
    {
        if (target == null)
            return "null";

        string path = target.name;
        Transform current = target.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

}
