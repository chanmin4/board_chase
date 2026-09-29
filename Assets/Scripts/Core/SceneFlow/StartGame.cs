/*
 * Role hierarchy: Core > SceneFlow
 * Primary responsibility: Implements the focused StartGame responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections;
using UnityEngine;

/// <summary>
/// Open Project StartGame 폼 유지.
/// 단, 위치 기반 이어하기는 제거하고
/// 고정된 게임 씬으로만 진입하도록 단순화한 버전.
/// </summary>
public class StartGame : MonoBehaviour
{
    [Tooltip("게임시작씬")]
    [SerializeField] private GameSceneSO LocationtoLoad;
    [SerializeField] private SaveSystem _saveSystem = default;
    [SerializeField] private bool _showLoadScreen = default;

    [Header("Broadcasting on")]
    [SerializeField] private LoadEventChannelSO _loadGameScene = default;

    [Header("Listening to")]
    [SerializeField] private VoidEventChannelSO _onNewGameButton = default;
    [SerializeField] private VoidEventChannelSO _onContinueButton = default;

    private bool _hasSaveData;

    // Purpose: Completes initialization after scene dependencies are ready.
    private void Start()
    {
        RefreshSaveState();

        if (_onNewGameButton != null)
            _onNewGameButton.OnEventRaised += StartNewGame;

        if (_onContinueButton != null)
            _onContinueButton.OnEventRaised += ContinuePreviousGame;
    }

    // Purpose: Releases owned resources before this component is destroyed.
    private void OnDestroy()
    {
        if (_onNewGameButton != null)
            _onNewGameButton.OnEventRaised -= StartNewGame;

        if (_onContinueButton != null)
            _onContinueButton.OnEventRaised -= ContinuePreviousGame;
    }

    // Purpose: Refreshes the displayed or cached save state.
    private void RefreshSaveState()
    {
        if (_saveSystem == null)
        {
            Debug.LogError("[StartGame] SaveSystem reference is missing.");
            _hasSaveData = false;
            return;
        }

        _hasSaveData = _saveSystem.LoadSaveDataFromDisk();
    }

    // Purpose: Begins the new game workflow.
    private void StartNewGame()
    {
        if (_saveSystem == null)
        {
            Debug.LogError("[StartGame] SaveSystem reference is missing.");
            return;
        }

        if (LocationtoLoad == null)
        {
            Debug.LogError("[StartGame] Gameplay scene is not assigned.");
            return;
        }

        _hasSaveData = false;

        // 오픈프로젝트 느낌 유지:
        // 빈 세이브 작성 -> 새 게임 데이터 세팅 -> 씬 진입
        _saveSystem.WriteEmptySaveFile();
        _saveSystem.SetNewGameData();

        _loadGameScene.RaiseEvent(LocationtoLoad, _showLoadScreen);
    }

    // Purpose: Performs the continue previous game workflow for this component.
    private void ContinuePreviousGame()
    {
        RefreshSaveState();

        if (!_hasSaveData)
        {
            Debug.LogWarning("[StartGame] No save data found. Starting new game instead.");
            StartNewGame();
            return;
        }

        StartCoroutine(LoadSaveGame());
    }

   // Purpose: Loads save game into the active runtime state.
   private IEnumerator LoadSaveGame()
{
    bool loaded = _saveSystem.LoadSaveDataFromDisk();

    if (!loaded)
    {
        Debug.LogWarning("[StartGame] Save data not found. Starting new game instead.");
        StartNewGame();
        yield break; 
    }

    _loadGameScene.RaiseEvent(LocationtoLoad, _showLoadScreen);
    yield break;
}

    // Purpose: Checks whether this component has save data.
    public bool HasSaveData()
    {
        return _hasSaveData;
    }

    // Purpose: Performs the reset save data flag workflow for this component.
    public void ResetSaveDataFlag()
    {
        _hasSaveData = false;
    }
}
