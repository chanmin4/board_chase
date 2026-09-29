/*
 * Role hierarchy: Core > Anchors
 * Primary responsibility: Implements the focused RuntimeAnchorBase responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using  UnityEngine.Events;

public class RuntimeAnchorBase<T> : DescriptionBaseSO where T : UnityEngine.Object
{
	public UnityAction OnAnchorProvided;

	[Header("Debug")]
	[ReadOnly] public bool isSet = false; // Any script can check if the transform is null before using it, by just checking this bool

	[ReadOnly] [SerializeField] private T _value;
	public T Value
	{
		get { return _value; }
	}

	// Purpose: Performs the provide workflow for this component.
	public void Provide(T value)
	{
		if(value == null)
		{
			Debug.LogError("A null value was provided to the " + this.name + " runtime anchor.");
			return;
		}

		_value = value;
		isSet = true;
		
		if(OnAnchorProvided != null)
			OnAnchorProvided.Invoke();
	}

	// Purpose: Performs the unset workflow for this component.
	public void Unset()
	{
		_value = null;
		isSet = false;
	}

	// Purpose: Releases active subscriptions and transient state while disabled.
	private void OnDisable()
	{
		Unset();
	}
}
