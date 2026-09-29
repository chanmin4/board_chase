/*
 * Role hierarchy: Presentation > UI > Gameplay > Interaction
 * Primary responsibility: Presents InteractionPromptWidget state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using TMPro;
using UnityEngine;

public class InteractionPromptWidget : MonoBehaviour
{
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_Text _keyText;
    [SerializeField] private TMP_Text _actionText;

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    // Purpose: Binds bind to the active runtime or UI target.
    public void Bind(InteractionPromptSnapshot snapshot)
    {
        if (_keyText != null)
            _keyText.text = snapshot.KeyLabel;

        if (_actionText != null)
            _actionText.text = snapshot.ActionLabel;
    }

    // Purpose: Updates the screen position state.
    public void SetScreenPosition(Vector2 localPoint)
    {
        if (_rectTransform != null)
            _rectTransform.anchoredPosition = localPoint;
    }

    // Purpose: Updates the visible state.
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }
    }
}
