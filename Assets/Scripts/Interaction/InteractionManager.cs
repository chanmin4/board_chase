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
    Loot = 6
}

public class InteractionManager : MonoBehaviour
{
    [SerializeField] private InputReader _inputReader = default;
    [SerializeField] private InteractionConfigSO _interactionConfig;

    [Header("Actor")]
    [SerializeField] private Transform _interactionActor;

    [Header("Broadcasting on")]
    [SerializeField] private ItemEventChannelSO _onObjectPickUp = default;
    [SerializeField] private InteractionUIEventChannelSO _toggleInteractionUI = default;

    [Header("Listening to")]
    [SerializeField] private VoidEventChannelSO _onInteractionEnded = default;
    [SerializeField] private PlayableDirectorChannelSO _onCutsceneStart = default;

    [ReadOnly] public InteractionType currentInteractionType;
    [Header("Interaction Prompt")]
    [SerializeField] private InteractionPromptEventChannelSO _interactionPromptChannel;
    [SerializeField] private string _interactionKeyLabel = "E";
    private LinkedList<Interaction> _potentialInteractions = new LinkedList<Interaction>();
    private float _nextPortalInteractAllowedTime;

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

    private void OnEnable()
    {
        if (_inputReader != null)
            _inputReader.InteractEvent += OnInteractionButtonPress;

        if (_onInteractionEnded != null)
            _onInteractionEnded.OnEventRaised += OnInteractionEnd;

        if (_onCutsceneStart != null)
            _onCutsceneStart.OnEventRaised += ResetPotentialInteractions;
    }

    private void OnDisable()
    {
        if (_inputReader != null)
            _inputReader.InteractEvent -= OnInteractionButtonPress;

        if (_onInteractionEnded != null)
            _onInteractionEnded.OnEventRaised -= OnInteractionEnd;

        if (_onCutsceneStart != null)
            _onCutsceneStart.OnEventRaised -= ResetPotentialInteractions;
    }

    private void Collect()
    {
        if (_potentialInteractions.Count == 0)
            return;

        Interaction interaction = _potentialInteractions.First.Value;

        if (interaction.type != InteractionType.PickUp)
            return;

        GameObject itemObject = interaction.interactableObject;
        _potentialInteractions.RemoveFirst();
        Destroy(itemObject);
        RequestUpdateUI(false);
    }

	private void OnInteractionButtonPress()
	{
		Debug.Log($"[InteractionManager] E pressed. potentialCount={_potentialInteractions.Count}");

		if (_potentialInteractions.Count == 0)
			return;

		Interaction interaction = _potentialInteractions.First.Value;
		currentInteractionType = interaction.type;

		Debug.Log($"[InteractionManager] Current interaction type={interaction.type}, obj={interaction.interactableObject}");

		switch (interaction.type)
		{
			case InteractionType.Portal:
				TryUsePortal(interaction);
				break;

			case InteractionType.PickUp:
                TryUsePickup(interaction);
				break;

			case InteractionType.Talk:
				break;

            case InteractionType.Shop:
                TryUseShop(interaction);
                break;

            case InteractionType.Loot:
                TryUseLoot(interaction);
                break;

            case InteractionType.QTE:
                TryUseQTEStation(interaction);
                break;
		}
	}

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

        if (moved)
        {
            _nextPortalInteractAllowedTime =
                Time.time + ResolvePortalInteractCooldown();
            currentInteractionType = InteractionType.None;
            _potentialInteractions.Clear();
            RequestUpdateUI(false);
        }
    }

    private void TryUsePickup(Interaction interaction)
    {
        if (interaction.interactableObject == null)
            return;

        GameObject pickupObject = interaction.interactableObject;

        TreasureRoomRewardPickup pickup =
            pickupObject.GetComponent<TreasureRoomRewardPickup>() ??
            pickupObject.GetComponentInParent<TreasureRoomRewardPickup>();

        bool picked = false;

        if (pickup != null && pickup.CanInteract)
        {
            picked = pickup.TryPickup(InteractionActor);
        }
        else
        {
            ItemWorldPickup itemPickup =
                pickupObject.GetComponent<ItemWorldPickup>() ??
                pickupObject.GetComponentInParent<ItemWorldPickup>();

            if (itemPickup == null || !itemPickup.CanInteract)
                return;

            picked = itemPickup.TryPickup(InteractionActor);
        }

        if (!picked)
            return;

        currentInteractionType = InteractionType.None;
        RemovePotentialInteraction(pickupObject);
    }

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

    private void TryUseLoot(Interaction interaction)
    {
        if (interaction.interactableObject == null)
            return;

        EnemyLootInventoryRuntime loot =
            interaction.interactableObject.GetComponent<EnemyLootInventoryRuntime>() ??
            interaction.interactableObject.GetComponentInParent<EnemyLootInventoryRuntime>();

        if (loot == null || !loot.CanInteract)
            return;

        if (loot.TryInteract(InteractionActor))
        {
            currentInteractionType = InteractionType.None;
            RequestUpdateUI(false);
        }
    }

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
   	public void OnTriggerChangeDetected(bool entered, GameObject obj)
	{
		//Debug.Log($"[InteractionManager] Trigger {(entered ? "Enter" : "Exit")} obj={obj.name}, tag={obj.tag}, portal={obj.GetComponentInParent<SectorPortal>()}");

		if (entered)
			AddPotentialInteraction(obj);
		else
			RemovePotentialInteraction(obj);
	}

    private void AddPotentialInteraction(GameObject obj)
    {
        if (!TryCreateInteraction(obj, out Interaction newPotentialInteraction))
            return;

        if (ContainsInteractionObject(newPotentialInteraction.interactableObject))
            return;

        _potentialInteractions.AddFirst(newPotentialInteraction);
        RequestUpdateUI(true);
    }

    private void RemovePotentialInteraction(GameObject obj)
    {
        GameObject normalizedObject = ResolveInteractableObject(obj);

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
    }

	private bool TryCreateInteraction(GameObject obj, out Interaction interaction)
	{
		interaction = new Interaction(InteractionType.None, null);

		if (obj == null)
			return false;

		SectorPortal portal = obj.GetComponentInParent<SectorPortal>();

		//Debug.Log($"[InteractionManager] TryCreateInteraction obj={obj.name}, portal={portal}, portalCanInteract={(portal != null ? portal.CanInteract.ToString() : "null")}");

		if (portal != null && portal.CanInteract)
		{
			interaction = new Interaction(InteractionType.Portal, portal.gameObject);
			//Debug.Log($"[InteractionManager] Added portal interaction. portal={portal.name}");
			return true;
		}
        MutarusQTEStation qteStation = obj.GetComponentInParent<MutarusQTEStation>();

        if (qteStation != null && qteStation.CanInteract)
        {
            interaction = new Interaction(InteractionType.QTE, qteStation.gameObject);
            return true;
        }

        TreasureRoomRewardPickup pickup =
            obj.GetComponentInParent<TreasureRoomRewardPickup>();

        if (pickup != null && pickup.CanInteract)
        {
            interaction = new Interaction(InteractionType.PickUp, pickup.gameObject);
            return true;
        }

        ItemWorldPickup itemPickup = obj.GetComponentInParent<ItemWorldPickup>();

        if (itemPickup != null && itemPickup.CanInteract)
        {
            interaction = new Interaction(InteractionType.PickUp, itemPickup.gameObject);
            return true;
        }

        EnemyLootInventoryRuntime loot = obj.GetComponentInParent<EnemyLootInventoryRuntime>();

        if (loot != null && loot.CanInteract)
        {
            interaction = new Interaction(InteractionType.Loot, loot.gameObject);
            return true;
        }

        SectorShop shop = obj.GetComponentInParent<SectorShop>();

        if (shop != null && shop.CanInteract)
        {
            interaction = new Interaction(InteractionType.Shop, shop.gameObject);
            return true;
        }
		//Debug.Log($"[InteractionManager] No valid interaction type. obj={obj.name}");
		return false;
	}

    private GameObject ResolveInteractableObject(GameObject obj)
    {
        if (obj == null)
            return null;

        SectorPortal portal = obj.GetComponentInParent<SectorPortal>();
        if (portal != null)
            return portal.gameObject;

        MutarusQTEStation qteStation = obj.GetComponentInParent<MutarusQTEStation>();
        if (qteStation != null)
            return qteStation.gameObject;

        TreasureRoomRewardPickup pickup =
            obj.GetComponentInParent<TreasureRoomRewardPickup>();
        if (pickup != null)
            return pickup.gameObject;

        ItemWorldPickup itemPickup = obj.GetComponentInParent<ItemWorldPickup>();
        if (itemPickup != null)
            return itemPickup.gameObject;

        EnemyLootInventoryRuntime loot = obj.GetComponentInParent<EnemyLootInventoryRuntime>();
        if (loot != null)
            return loot.gameObject;

        SectorShop shop = obj.GetComponentInParent<SectorShop>();
        if (shop != null)
            return shop.gameObject;

        return obj;
    }

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

    private void ResetPotentialInteractions(PlayableDirector playableDirector)
    {
        _potentialInteractions.Clear();
        RequestUpdateUI(false);
    }

    private void PublishInteractionPrompt(bool visible)
    {
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
            _interactionPromptChannel.Clear();
            return;
        }

        _interactionPromptChannel.RaiseEvent(new InteractionPromptSnapshot(
            true,
            interaction.type,
            anchor,
            ResolveInteractionKeyLabel(),
            ResolveActionLabel(interaction)));
    }

    private Transform ResolvePromptAnchor(GameObject obj)
    {
        if (obj == null)
            return null;

        InteractionPromptAnchor anchor =
            obj.GetComponentInChildren<InteractionPromptAnchor>(true) ??
            obj.GetComponentInParent<InteractionPromptAnchor>();

        if (anchor != null)
            return anchor.Anchor;

        return obj.transform;
    }

    private string ResolveInteractionKeyLabel()
    {
        return string.IsNullOrWhiteSpace(_interactionKeyLabel)
            ? "E"
            : _interactionKeyLabel;
    }

    private string ResolveActionLabel(Interaction interaction)
    {
        if (interaction != null &&
            interaction.type == InteractionType.PickUp &&
            interaction.interactableObject != null)
        {
            ItemWorldPickup itemPickup =
                interaction.interactableObject.GetComponent<ItemWorldPickup>() ??
                interaction.interactableObject.GetComponentInParent<ItemWorldPickup>();

            if (itemPickup != null)
                return itemPickup.DisplayLabel;
        }

        return interaction != null ? interaction.type switch
        {
            InteractionType.Portal => "",
            InteractionType.QTE => "",
            InteractionType.PickUp => "",
            InteractionType.Shop => "",
            InteractionType.Loot => "",
            InteractionType.Talk => "",
            _ => ""
        } : "";
    }

    private void RefreshPotentialInteractions()
    {
        LinkedListNode<Interaction> node = _potentialInteractions.First;

        while (node != null)
        {
            LinkedListNode<Interaction> next = node.Next;
            GameObject obj = node.Value.interactableObject;

            if (obj == null || !TryCreateInteraction(obj, out Interaction refreshedInteraction))
                _potentialInteractions.Remove(node);
            else
                node.Value = refreshedInteraction;

            node = next;
        }
    }

    private float ResolvePortalInteractCooldown()
    {
        if (_interactionConfig == null)
            return 1f;

        return _interactionConfig.PortalInteractCooldownSeconds;
    }

}
