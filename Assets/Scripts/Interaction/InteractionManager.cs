using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public enum InteractionType
{
    None = 0,
    PickUp,
    Talk,
    Portal
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

    private LinkedList<Interaction> _potentialInteractions = new LinkedList<Interaction>();

    private Transform InteractionActor => _interactionActor != null ? _interactionActor : transform;

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

        if (_onObjectPickUp != null)
        {
            ItemSO currentItem = itemObject.GetComponent<CollectableItem>().GetItem();
            _onObjectPickUp.RaiseEvent(currentItem);
        }

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

   	public void OnTriggerChangeDetected(bool entered, GameObject obj)
	{
		Debug.Log($"[InteractionManager] Trigger {(entered ? "Enter" : "Exit")} obj={obj.name}, tag={obj.tag}, portal={obj.GetComponentInParent<SectorPortal>()}");

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

		Debug.Log($"[InteractionManager] TryCreateInteraction obj={obj.name}, portal={portal}, portalCanInteract={(portal != null ? portal.CanInteract.ToString() : "null")}");

		if (portal != null && portal.CanInteract)
		{
			interaction = new Interaction(InteractionType.Portal, portal.gameObject);
			Debug.Log($"[InteractionManager] Added portal interaction. portal={portal.name}");
			return true;
		}
		Debug.Log($"[InteractionManager] No valid interaction type. obj={obj.name}");
		return false;
	}

    private GameObject ResolveInteractableObject(GameObject obj)
    {
        if (obj == null)
            return null;

        SectorPortal portal = obj.GetComponentInParent<SectorPortal>();
        if (portal != null)
            return portal.gameObject;

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
        if (_toggleInteractionUI == null)
            return;

        if (visible && _potentialInteractions.Count > 0)
            _toggleInteractionUI.RaiseEvent(true, _potentialInteractions.First.Value.type);
        else
            _toggleInteractionUI.RaiseEvent(false, InteractionType.None);
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
}
