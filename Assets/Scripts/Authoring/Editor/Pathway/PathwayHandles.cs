/*
 * Role hierarchy: Authoring > Editor > Pathway
 * Primary responsibility: Provides the PathwayHandles authoring tool and its editor workflow.
 * Execution boundary: Editor only (Assembly-CSharp-Editor)
 */

using UnityEngine;
using UnityEditor;


public class PathwayHandles
{
	private PathwayConfigSO _pathway;
	private Vector3 _tmp;

	public PathwayHandles(PathwayConfigSO pathway)
	{
		_pathway = pathway;
	}

	// Purpose: Performs the display handles workflow for this component.
	public int DisplayHandles()
	{
		for (int i = 0; i < _pathway.Waypoints.Count; i++)
		{
			EditorGUI.BeginChangeCheck();

			_tmp = Handles.PositionHandle(_pathway.Waypoints[i].waypoint, Quaternion.identity);

			if (EditorGUI.EndChangeCheck())
			{
				_pathway.Waypoints[i].waypoint = _tmp;
				return i;
			}
		}
		return -1;
	}
}
