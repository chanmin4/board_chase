/*
 * Role hierarchy: Presentation > UI > Common
 * Primary responsibility: Implements the focused UIGenericButton responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

public class UIGenericButton : MonoBehaviour
{
	[SerializeField] private LocalizeStringEvent _buttonText = default;
	[SerializeField] private MultiInputButton _button = default;

	public UnityAction Clicked = default;

	private bool _isDefaultSelection = false;

	// Purpose: Releases active subscriptions and transient state while disabled.
	private void OnDisable()
	{
		_button.IsSelected = false;
		_isDefaultSelection = false;
	}

	// Purpose: Updates the button state.
	public void SetButton(bool isSelect)
	{
		_isDefaultSelection = isSelect;
		if (isSelect)
			_button.UpdateSelected();
	}

	// Purpose: Updates the button state.
	public void SetButton(LocalizedString localizedString, bool isSelected)
	{
		if (_buttonText != null)
		{
			_buttonText.StringReference = localizedString;
			_buttonText.RefreshString();
		}

		if (isSelected)
			SelectButton();
	}

	// Purpose: Updates the button state.
	public void SetButton(string tableEntryReference, bool isSelected)
	{
		_buttonText.StringReference.TableEntryReference = tableEntryReference;

		if (isSelected)
			SelectButton();
	}

	void SelectButton()
	{
		_button.Select();
	}

	// Purpose: Performs the click workflow for this component.
	public void Click()
	{
		Clicked.Invoke();
	}
}
