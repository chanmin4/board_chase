/*
 * Role hierarchy: Presentation > Audio
 * Primary responsibility: Handles UIAudio audio playback and sound-stimulus delivery.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIAudio : MonoBehaviour
{
    private static UIAudio _active;
    private Selectable[] _selectables = new Selectable[128];
    private int _lastBindFrame = -1;
    private UIAudioCueType _pendingCue;
    private bool _hasPendingCue;
    private AudioCueSO _pendingOverrideCue;
    private float _nextNavigationTime;

    [Header("Automatic UI Wiring")]
    [Tooltip("Connect active authored and runtime-created Selectables to the existing UI channel. No AudioSources are added to buttons.")]
    [SerializeField] private bool _autoBindSelectables = true;

    [Header("Events")]
    [SerializeField] private UIAudioEventChannelSO _uiAudioEventChannel;
    [SerializeField] private AudioCueEventChannelSO _sfxEventChannel;

    [Header("Audio Output")]
    [SerializeField] private AudioConfigurationSO _audioConfig;
    [SerializeField] private GameStateSO _gameState;

    [Header("Panel")]
    [SerializeField] private AudioCueSO _panelOpenCue;
    [SerializeField] private AudioCueSO _panelCloseCue;

    [Header("Generic UI")]
    [SerializeField] private AudioCueSO _buttonClickCue;
    [SerializeField] private AudioCueSO _deniedCue;
    [SerializeField] private AudioCueSO _navigateCue;
    [SerializeField] private AudioCueSO _cancelCue;

    [Header("Quest Notifications")]
    [SerializeField] private AudioCueSO _questAcceptedCue;
    [SerializeField] private AudioCueSO _questCompletedCue;

    [Header("Inventory")]
    [SerializeField] private AudioCueSO _inventoryMoveCue;
    [SerializeField] private AudioCueSO _equipCue;
    [SerializeField] private AudioCueSO _unequipCue;
    [SerializeField] private AudioCueSO _splitCue;

    [Header("Shop")]
    [SerializeField] private AudioCueSO _buyCue;
    [SerializeField] private AudioCueSO _sellCue;
    [SerializeField] private AudioCueSO _repairCue;

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_active != null && _active != this)
        {
            enabled = false;
            return;
        }
        _active = this;
        Canvas.willRenderCanvases += BindActiveSelectables;
        if (_uiAudioEventChannel != null)
            _uiAudioEventChannel.OnEventRaised += Play;
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        Canvas.willRenderCanvases -= BindActiveSelectables;
        _hasPendingCue = false;
        _pendingOverrideCue = null;
        if (_active == this)
            _active = null;
        if (_uiAudioEventChannel != null)
            _uiAudioEventChannel.OnEventRaised -= Play;
    }

    public static void Request(UIAudioCueType cueType)
    {
        if (_active == null || !_active.isActiveAndEnabled)
            return;
        if (_active._uiAudioEventChannel != null)
            _active._uiAudioEventChannel.RaiseEvent(cueType);
        else
            _active.Play(cueType);
    }

    // Called only at UI command boundaries, never from inventory restore/refresh.
    public static bool ReportResult(bool success, UIAudioCueType successCue)
    {
        Request(success ? successCue : UIAudioCueType.Denied);
        return success;
    }

    public static void PlayQuestNotification(AudioCueSO overrideCue, bool completed)
    {
        if (_active == null)
            return;
        AudioCueSO cue = overrideCue != null && overrideCue.HasPlayableClip && !overrideCue.looping
            ? overrideCue
            : _active.ResolveCue(completed ? UIAudioCueType.QuestCompleted : UIAudioCueType.QuestAccepted);
        if (_active.CanPlay(cue))
        {
            _active._pendingCue = completed ? UIAudioCueType.QuestCompleted : UIAudioCueType.QuestAccepted;
            _active._pendingOverrideCue = cue;
            _active._hasPendingCue = true;
        }
    }

    private void BindActiveSelectables()
    {
        if (!Application.isPlaying || !_autoBindSelectables || _lastBindFrame == Time.frameCount)
            return;
        _lastBindFrame = Time.frameCount;
        if (_selectables.Length < Selectable.allSelectableCount)
            _selectables = new Selectable[Selectable.allSelectableCount + 32];
        int count = Selectable.AllSelectablesNoAlloc(_selectables);
        for (int i = 0; i < count; i++)
        {
            Selectable selectable = _selectables[i];
            if (selectable == null || !selectable.gameObject.scene.IsValid() ||
                selectable is InputField || selectable is TMPro.TMP_InputField)
                continue;
            if (!selectable.TryGetComponent(out UIButtonAudioEventEmitter emitter))
                emitter = selectable.gameObject.AddComponent<UIButtonAudioEventEmitter>();
            emitter.Bind(selectable, _uiAudioEventChannel);
        }
        System.Array.Clear(_selectables, 0, count);
    }

    private void LateUpdate()
    {
        if (!_hasPendingCue)
            return;
        AudioCueSO cue = _pendingOverrideCue != null ? _pendingOverrideCue : ResolveCue(_pendingCue);
        _hasPendingCue = false;
        _pendingOverrideCue = null;
        if (CanPlay(cue))
            _sfxEventChannel.RaisePlayEvent(cue, _audioConfig, Vector3.zero);
    }

    private static int Priority(UIAudioCueType type)
    {
        if (type == UIAudioCueType.QuestAccepted || type == UIAudioCueType.QuestCompleted) return 6;
        if (type == UIAudioCueType.Navigate) return 0;
        if (type == UIAudioCueType.ButtonClick || type == UIAudioCueType.Cancel) return 1;
        if (type == UIAudioCueType.PanelOpen || type == UIAudioCueType.PanelClose) return 2;
        if (type == UIAudioCueType.Denied) return 5;
        if (type == UIAudioCueType.InventoryMove) return 3;
        return 4;
    }

    // Purpose: Plays the requested panel open action or effect.
    public void PlayPanelOpen() => Play(UIAudioCueType.PanelOpen);
    // Purpose: Plays the requested panel close action or effect.
    public void PlayPanelClose() => Play(UIAudioCueType.PanelClose);
    // Purpose: Plays the requested button click action or effect.
    public void PlayButtonClick() => Play(UIAudioCueType.ButtonClick);
    // Purpose: Plays the requested denied action or effect.
    public void PlayDenied() => Play(UIAudioCueType.Denied);
    // Purpose: Plays the requested inventory move action or effect.
    public void PlayInventoryMove() => Play(UIAudioCueType.InventoryMove);
    // Purpose: Plays the requested equip action or effect.
    public void PlayEquip() => Play(UIAudioCueType.Equip);
    // Purpose: Plays the requested unequip action or effect.
    public void PlayUnequip() => Play(UIAudioCueType.Unequip);
    // Purpose: Plays the requested split action or effect.
    public void PlaySplit() => Play(UIAudioCueType.Split);
    // Purpose: Plays the requested buy action or effect.
    public void PlayBuy() => Play(UIAudioCueType.Buy);
    // Purpose: Plays the requested sell action or effect.
    public void PlaySell() => Play(UIAudioCueType.Sell);
    // Purpose: Plays the requested repair action or effect.
    public void PlayRepair() => Play(UIAudioCueType.Repair);

    // Purpose: Plays the requested play action or effect.
    public void Play(UIAudioCueType cueType)
    {
        if (!CanPlay(ResolveCue(cueType)))
            return;
        if (cueType == UIAudioCueType.Navigate)
        {
            if (Time.unscaledTime < _nextNavigationTime) return;
            _nextNavigationTime = Time.unscaledTime + 0.08f;
        }
        // Result feedback wins over its button click in the same frame.
        if (!_hasPendingCue || Priority(cueType) >= Priority(_pendingCue))
        {
            _pendingCue = cueType;
            _pendingOverrideCue = null;
            _hasPendingCue = true;
        }
    }

    // Purpose: Resolves the cue needed by this component.
    private AudioCueSO ResolveCue(UIAudioCueType cueType)
    {
        return cueType switch
        {
            UIAudioCueType.PanelOpen => _panelOpenCue,
            UIAudioCueType.PanelClose => _panelCloseCue,
            UIAudioCueType.ButtonClick => _buttonClickCue,
            UIAudioCueType.Denied => _deniedCue,
            UIAudioCueType.InventoryMove => _inventoryMoveCue,
            UIAudioCueType.Equip => _equipCue,
            UIAudioCueType.Unequip => _unequipCue,
            UIAudioCueType.Split => _splitCue,
            UIAudioCueType.Buy => _buyCue,
            UIAudioCueType.Sell => _sellCue,
            UIAudioCueType.Repair => _repairCue,
            UIAudioCueType.Navigate => _navigateCue,
            UIAudioCueType.Cancel => _cancelCue,
            UIAudioCueType.QuestAccepted => _questAcceptedCue,
            UIAudioCueType.QuestCompleted => _questCompletedCue,
            _ => null
        };
    }

    // Purpose: Checks whether play is currently permitted.
    private bool CanPlay(AudioCueSO cue)
    {
        if (!isActiveAndEnabled)
            return false;

        if (cue == null || !cue.HasPlayableClip || cue.looping || _audioConfig == null || _sfxEventChannel == null)
            return false;

        if (_gameState != null && _gameState.CurrentGameState == GameState.Cutscene)
            return false;

        return true;
    }
}
