/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player
 * Primary responsibility: Plays local-player consumable and room-condition audio feedback.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

/// <summary>
/// Presentation-only audio bridge for player conditions that are not weapon or
/// movement actions. Gameplay remains owned by the consumable and sector-trait
/// systems; this component only observes their resolved state.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerConditionAudio : MonoBehaviour
{
    [Header("Shared Audio Output")]
    [SerializeField] private AudioCueEventChannelSO _sfxEventChannel;
    [SerializeField] private AudioConfigurationSO _playerSfxConfiguration;

    [Header("Condition Cues")]
    [SerializeField] private AudioCueSO _consumableUseCue;
    [SerializeField] private AudioCueSO _consumableCompleteCue;
    [SerializeField] private AudioCueSO _infectionCureUseCue;
    [SerializeField] private AudioCueSO _infectionCureCompleteCue;
    [Tooltip("Played once when the player enters a room with an active biochemical-gas leak.")]
    [SerializeField] private AudioCueSO _biochemicalGasLoopCue;

    private PlayerConsumableUseRuntime _consumableUseRuntime;
    private bool _wasGasActive;

    // Purpose: Resolves sibling and parent runtime references when the player prefab starts.
    private void Awake()
    {
        ResolveRefs();
    }

    // Purpose: Subscribes to consumable feedback after the player becomes active.
    private void OnEnable()
    {
        ResolveRefs();

        if (_consumableUseRuntime != null)
        {
            _consumableUseRuntime.UseStarted += HandleConsumableUseStarted;
            _consumableUseRuntime.UseCompleted += HandleConsumableUseCompleted;
        }
    }

    // Purpose: Releases consumable event subscriptions and clears transient room-condition state.
    private void OnDisable()
    {
        if (_consumableUseRuntime != null)
        {
            _consumableUseRuntime.UseStarted -= HandleConsumableUseStarted;
            _consumableUseRuntime.UseCompleted -= HandleConsumableUseCompleted;
        }

        _wasGasActive = false;
    }

    // Purpose: Detects player entry into an active room-gas leak without mutating the trait state.
    private void Update()
    {
        float rawGasRate = SectorRoomTraitRuntime.CurrentPlayerBiochemicalGasInfectionPerSecond;
        bool gasActive = rawGasRate > 0.001f;

        if (gasActive && !_wasGasActive)
            PlayOneShot(_biochemicalGasLoopCue);

        _wasGasActive = gasActive;
    }

    // Purpose: Resolves the requested player consumable use request or event.
    private void HandleConsumableUseStarted(ConsumableItemSO item)
    {
        PlayOneShot(item is InfectionCureItemSO ? _infectionCureUseCue : _consumableUseCue);
    }

    // Purpose: Resolves the requested player consumable use completion request or event.
    private void HandleConsumableUseCompleted(ConsumableItemSO item)
    {
        PlayOneShot(item is InfectionCureItemSO
            ? _infectionCureCompleteCue
            : _consumableCompleteCue);
    }

    // Purpose: Plays a valid local-player condition one-shot.
    private void PlayOneShot(AudioCueSO cue)
    {
        if (_sfxEventChannel == null || cue == null)
            return;

        _sfxEventChannel.RaisePlayEvent(cue, _playerSfxConfiguration, transform.position);
    }

    // Purpose: Resolves parent player components that may be reconstructed during scene flow.
    private void ResolveRefs()
    {
        if (_consumableUseRuntime == null)
            _consumableUseRuntime = GetComponentInParent<PlayerConsumableUseRuntime>();
    }
}
