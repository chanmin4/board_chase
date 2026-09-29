/*
 * Role hierarchy: Presentation > Audio
 * Primary responsibility: Handles UIButtonAudioEventEmitter audio playback and sound-stimulus delivery.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIButtonAudioEventEmitter : MonoBehaviour, IPointerClickHandler,
    IPointerEnterHandler, ISelectHandler, ISubmitHandler, ICancelHandler, IDragHandler, IMoveHandler
{
    [Header("Events")]
    [SerializeField] private UIAudioEventChannelSO _uiAudioEventChannel;

    [Header("Button")]
    [SerializeField] private Selectable _selectable;
    [SerializeField] private UIAudioCueType _clickCue = UIAudioCueType.ButtonClick;
    [SerializeField] private UIAudioCueType _deniedCue = UIAudioCueType.Denied;
    [SerializeField] private bool _playDeniedWhenNotInteractable = true;
    private Button _button;

    public void Bind(Selectable selectable, UIAudioEventChannelSO channel)
    {
        _selectable = selectable;
        if (_uiAudioEventChannel == null)
            _uiAudioEventChannel = channel;
    }

    private void OnEnable()
    {
        if (_selectable == null) _selectable = GetComponent<Selectable>();
        _button = _selectable as Button;
        if (_button != null) _button.onClick.AddListener(HandleButtonClick);
    }

    private void OnDisable()
    {
        if (_button != null) _button.onClick.RemoveListener(HandleButtonClick);
    }

    private void Emit(UIAudioCueType cue)
    {
        if (_uiAudioEventChannel != null) _uiAudioEventChannel.RaiseEvent(cue);
        else UIAudio.Request(cue);
    }

    private bool Interactable => _selectable != null && _selectable.IsActive() && _selectable.IsInteractable();
    private void HandleButtonClick() => Emit(_clickCue);
    private void Navigate() { if (Interactable) Emit(UIAudioCueType.Navigate); }
    public void OnPointerEnter(PointerEventData eventData) => Navigate();
    public void OnSelect(BaseEventData eventData) => Navigate();
    public void OnMove(AxisEventData eventData) => Navigate();
    public void OnDrag(PointerEventData eventData)
    {
        if (_selectable is Slider) Navigate();
    }
    public void OnCancel(BaseEventData eventData) => Emit(UIAudioCueType.Cancel);
    public void OnSubmit(BaseEventData eventData)
    {
        if (_button == null && Interactable) Emit(_clickCue);
    }

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        _selectable = GetComponent<Selectable>();
    }

    // Purpose: Responds to the pointer click callback.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        bool interactable = Interactable;

        if (interactable)
        {
            // Button.onClick covers both pointer and keyboard/controller submit.
            if (_button == null) Emit(_clickCue);
            return;
        }

        if (_playDeniedWhenNotInteractable)
            Emit(_deniedCue);
    }
}
