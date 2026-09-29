/*
 * Role hierarchy: Core > Persistence
 * Primary responsibility: Coordinates the FileManager subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.IO;
using UnityEngine;

public static class FileManager
{
	// Purpose: Performs the write to file workflow for this component.
	public static bool WriteToFile(string fileName, string fileContents)
	{
		var fullPath = Path.Combine(Application.persistentDataPath, fileName);

		try
		{
			File.WriteAllText(fullPath, fileContents);
			return true;
		}
		catch (Exception e)
		{
			Debug.LogError($"Failed to write to {fullPath} with exception {e}");
			return false;
		}
	}

	// Purpose: Loads from file into the active runtime state.
	public static bool LoadFromFile(string fileName, out string result)
	{
		var fullPath = Path.Combine(Application.persistentDataPath, fileName);

		if (!File.Exists(fullPath))
		{
			result = "";
			return false;
		}

		try
		{
			result = File.ReadAllText(fullPath);
			return !string.IsNullOrWhiteSpace(result);
		}
		catch (Exception e)
		{
			Debug.LogError($"Failed to read from {fullPath} with exception {e}");
			result = "";
			return false;
		}
	}

	// Purpose: Moves file toward its requested destination.
	public static bool MoveFile(string fileName, string newFileName)
	{
		var fullPath = Path.Combine(Application.persistentDataPath, fileName);
		var newFullPath = Path.Combine(Application.persistentDataPath, newFileName);

		try
		{
			if (File.Exists(newFullPath))
			{
				File.Delete(newFullPath);
			}

			if (!File.Exists(fullPath))
			{
				return false;
			}
			
			File.Move(fullPath, newFullPath);
		}
		catch (Exception e)
		{
			Debug.LogError($"Failed to move file from {fullPath} to {newFullPath} with exception {e}");
			return false;
		}

		return true;
	}
}
