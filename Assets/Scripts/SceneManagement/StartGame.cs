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

    private void Start()
    {
        RefreshSaveState();

        if (_onNewGameButton != null)
            _onNewGameButton.OnEventRaised += StartNewGame;

        if (_onContinueButton != null)
            _onContinueButton.OnEventRaised += ContinuePreviousGame;
    }

    private void OnDestroy()
    {
        if (_onNewGameButton != null)
            _onNewGameButton.OnEventRaised -= StartNewGame;

        if (_onContinueButton != null)
            _onContinueButton.OnEventRaised -= ContinuePreviousGame;
    }

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

    public bool HasSaveData()
    {
        return _hasSaveData;
    }

    public void ResetSaveDataFlag()
    {
        _hasSaveData = false;
    }
}