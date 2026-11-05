/*
 * Role hierarchy: Gameplay > Progression > Quests > Home
 * Primary responsibility: Makes an authored HomeSector resident open the quest mission panel and reset on home entry.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class HomeQuestNpc : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("This only identifies which authored quest branch this Home resident offers. All accepted quests still share one QuestRuntime and one Mission page.")]
    [SerializeField] private QuestBranch _role = QuestBranch.Main;
    [SerializeField] private string _displayName = "Main NPC";

    [Header("Conversation Audio")]
    [SerializeField] private NpcAudioProfileSO _conversationAudio;
    private AudioCueKey _voiceKey = AudioCueKey.Invalid;
    private bool _conversationOpen;
    [Tooltip("Only this resident's offered quests are shown at the top of the Mission page. Accepted quests from every resident remain visible below them. Quest prerequisites decide which offer can be accepted.")]
    [SerializeField] private QuestDefinitionSO[] _offeredQuests = Array.Empty<QuestDefinitionSO>();

    [Header("Home Reset")]
    [SerializeField] private Damageable _damageable;
    [Tooltip("Quest residents never expose combat loot. Keep their authored inventory and equipment empty as well.")]
    [SerializeField] private bool _disableLootRuntime = true;

    [Header("Quest-Only Runtime")]
    [Tooltip("Optional combat behaviours to keep disabled for this resident. Assign the EnemyShooter combat controller, state-machine driver, or any other combat-only behaviour from the authored Quest NPC variant. This component never destroys them, so the NPC can be reset every Home visit.")]
    [SerializeField] private Behaviour[] _disabledRuntimeBehaviours = Array.Empty<Behaviour>();

    public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
    public QuestBranch Role => _role;
    public QuestDefinitionSO[] OfferedQuests => _offeredQuests ?? Array.Empty<QuestDefinitionSO>();
    public bool CanInteract => isActiveAndEnabled;
    public bool IsConversationOpen => _conversationOpen;

    // Purpose: Prevents an authored quest from being offered by the wrong Home NPC branch.
    public bool CanOffer(QuestDefinitionSO definition)
    {
        return definition != null && definition.Branch == _role;
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        if (_damageable == null)
            _damageable = EntityComponentResolver.Resolve<Damageable>(transform);

        DisableQuestOnlyRuntime();
        DisableLootRuntime();
    }

    // Purpose: Attempts to interact and reports whether the request succeeded.
    public bool TryInteract(Component actor)
    {
        if (!CanInteract)
            return false;

        QuestRuntime.EnsureRuntime().SelectQuestGiver(this);

        PlayerPanelHub playerPanelHub = FindFirstObjectByType<PlayerPanelHub>(FindObjectsInactive.Include);
        if (playerPanelHub == null)
        {
            Debug.LogError("[HomeQuestNpc] PlayerPanelHub is missing; cannot open the Mission page.", this);
            return false;
        }

        playerPanelHub.OpenMissions(this);
        BeginConversation();
        return true;
    }

    public void BeginConversation()
    {
        if (_conversationOpen) return;
        _conversationOpen = true;
        PlayDialogueCue(_conversationAudio != null ? _conversationAudio.greeting : null);
    }

    public void EndConversation()
    {
        if (!_conversationOpen) return;
        _conversationOpen = false;
        PlayDialogueCue(_conversationAudio != null ? _conversationAudio.farewell : null);
    }

    public void PlayChoiceResponse(bool accepted)
    {
        if (!_conversationOpen || _conversationAudio == null) return;
        PlayDialogueCue(accepted ? _conversationAudio.accept : _conversationAudio.reject);
    }

    // A future dialogue line can call this too. Replacing/skipping a line stops the previous voice.
    public void PlayDialogueCue(AudioCueSO cue)
    {
        StopVoice();
        if (_conversationAudio == null || _conversationAudio.channel == null ||
            cue == null || !cue.HasPlayableClip || cue.looping) return;
        _voiceKey = _conversationAudio.channel.RaisePlayEvent(cue, _conversationAudio.configuration, transform.position);
    }

    private void StopVoice()
    {
        if (_voiceKey.IsValid && _conversationAudio != null)
            _conversationAudio.channel?.RaiseStopEvent(_voiceKey);
        _voiceKey = AudioCueKey.Invalid;
    }

    private void OnDisable()
    {
        _conversationOpen = false;
        StopVoice();
    }

    // Purpose: Performs the reset for home visit workflow for this component.
    public void ResetForHomeVisit()
    {
        gameObject.SetActive(true);
        _damageable?.ResetHealthFromConfig();
        DisableQuestOnlyRuntime();
        DisableLootRuntime();
    }

    // Purpose: Disables quest only runtime for this component.
    private void DisableQuestOnlyRuntime()
    {
        if (_disabledRuntimeBehaviours == null)
            return;

        for (int i = 0; i < _disabledRuntimeBehaviours.Length; i++)
        {
            Behaviour behaviour = _disabledRuntimeBehaviours[i];
            if (behaviour != null)
                behaviour.enabled = false;
        }
    }

    // Purpose: Disables loot runtime for this component.
    private void DisableLootRuntime()
    {
        if (!_disableLootRuntime)
            return;

        EnemyLootInventoryRuntime[] lootRuntimes = GetComponentsInChildren<EnemyLootInventoryRuntime>(true);
        for (int i = 0; i < lootRuntimes.Length; i++)
        {
            if (lootRuntimes[i] != null)
                lootRuntimes[i].enabled = false;
        }
    }
}
