/*
 * Role hierarchy: Authoring > Editor > Pathway
 * Primary responsibility: Provides the PathWayNavMeshUI authoring tool and its editor workflow.
 * Execution boundary: Editor only (Assembly-CSharp-Editor)
 */

using UnityEngine;
using UnityEditorInternal;


public class PathWayNavMeshUI
{
	private PathwayConfigSO _pathway;
	private PathwayNavMesh _pathwayNavMesh;

	public PathWayNavMeshUI(PathwayConfigSO pathway)
	{
		_pathway = pathway;
		_pathwayNavMesh = new PathwayNavMesh(pathway);
		RestorePath();
	}

	// Purpose: Responds to the inspector gui callback.
	public void OnInspectorGUI()
	{
		if (!_pathway.ToggledNavMeshDisplay)
		{
			if (GUILayout.Button("NavMesh Path"))
			{
				_pathway.ToggledNavMeshDisplay = true;
				GeneratePath();
				InternalEditorUtility.RepaintAllViews();
			}
		}
		else
		{
			if (GUILayout.Button("Handles Path"))
			{
				_pathway.ToggledNavMeshDisplay = false;
				InternalEditorUtility.RepaintAllViews();
			}
		}
	}

	// Purpose: Updates the path from the current runtime state.
	public void UpdatePath()
	{

		if (!_pathway.DisplayProbes)
		{
			_pathwayNavMesh.UpdatePath();
		}
	}

	// Purpose: Updates the path at from the current runtime state.
	public void UpdatePathAt(int index)
	{
		if (index >= 0)
		{
			_pathway.DisplayProbes = !_pathwayNavMesh.HasNavMeshAt(index);

			if (!_pathway.DisplayProbes && _pathway.ToggledNavMeshDisplay)
			{
				_pathway.DisplayProbes = !_pathwayNavMesh.UpdateCornersAt(index);
			}
		}
	}

	// Purpose: Performs the real time workflow for this component.
	public void RealTime(int index)
	{
		if (_pathway.RealTimeEnabled)
		{
			UpdatePathAt(index);

			if (_pathway.ToggledNavMeshDisplay)
			{
				UpdatePath();
			}
		}
	}

	// Purpose: Restores the saved path state.
	private void RestorePath()
	{
		bool existsPath = true;

		_pathway.Hits.Clear();
		_pathway.DisplayProbes = false;

		if (_pathway.Waypoints.Count > 1)
		{
			for (int i = 0; i < _pathway.Waypoints.Count; i++)
			{
				existsPath &= _pathwayNavMesh.HasNavMeshAt(i);
				existsPath &= _pathwayNavMesh.UpdateCornersAt(i);
			}

			if (existsPath)
			{
				_pathwayNavMesh.UpdatePath();
			}
		}

		_pathway.DisplayProbes = !existsPath;
	}

	// Purpose: Generates path from the authored data.
	public void GeneratePath()
	{
		if (_pathway.ToggledNavMeshDisplay)
		{
			RestorePath();
			_pathway.ToggledNavMeshDisplay = !_pathway.DisplayProbes;
		}
	}

}
