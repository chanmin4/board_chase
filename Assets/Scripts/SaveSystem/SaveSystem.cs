using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
[CreateAssetMenu(fileName = "SaveSystem", menuName = "Save/Save System")]
public class SaveSystem : ScriptableObject
{
    [SerializeField] private VoidEventChannelSO _saveSettingsEvent = default;
	[SerializeField] private SettingsSO _currentSettings = default;
    public string saveFilename = "save.vaccine";
	public string backupSaveFilename = "save.vaccine.bak";
    public Save saveData = new Save();

    private void OnEnable()
    {
        if (_saveSettingsEvent != null)
            _saveSettingsEvent.OnEventRaised += SaveSettings;
    }

    private void OnDisable()
    {
        if (_saveSettingsEvent != null)
            _saveSettingsEvent.OnEventRaised -= SaveSettings;
    }

    public bool LoadSaveDataFromDisk()
	{
		if (FileManager.LoadFromFile(saveFilename, out var json))
		{
			saveData.LoadFromJson(json);
			return true;
		}

        saveData.EnsureRuntimeDefaults();
		return false;
	}

    	public void SaveDataToDisk()
	{
        saveData.EnsureRuntimeDefaults();

		FileManager.MoveFile(saveFilename, backupSaveFilename);
		FileManager.WriteToFile(saveFilename, saveData.ToJson());
	}

	public void WriteEmptySaveFile()
	{
		FileManager.WriteToFile(saveFilename, "");

	}
	public void SetNewGameData()
	{
		FileManager.WriteToFile(saveFilename, "");

		SaveDataToDisk();

	}
	void SaveSettings()
	{
		saveData.SaveSettings(_currentSettings);

	}
}
