using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public enum InteractionType
{
    None = 0,
    PickUp,
    Talk,
    Portal,
    QTE
}

public class InteractionManager : MonoBehaviour
{
    [SerializeField] private InputReader _inputReader = default;

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
				break;

			case InteractionType.Talk:
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

        SectorPortal portal = interaction.interactableObject.GetComponent<SectorPortal>();
        if (portal == null)
            portal = interaction.interactableObject.GetComponentInParent<SectorPortal>();

        if (portal == null)
            return;

        bool moved = portal.TryInteract(InteractionActor);

        if (moved)
        {
            currentInteractionType = InteractionType.None;
            _potentialInteractions.Clear();
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
            ResolveActionLabel(interaction.type)));
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

    private string ResolveActionLabel(InteractionType type)
    {
        return type switch
        {
            InteractionType.Portal => "",
            InteractionType.QTE => "",
            InteractionType.PickUp => "",
            InteractionType.Talk => "",
            _ => ""
        };
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

}
