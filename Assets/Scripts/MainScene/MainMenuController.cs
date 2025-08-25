using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    public string gameSceneName = "GameScene";
    public TMP_Text bestText;

    void Start()
    {
        if (!ProgressManager.Instance)
        {
            var go = new GameObject("ProgressManager");
            go.AddComponent<ProgressManager>();
        }
        Refresh();
        ProgressManager.Instance.OnBestScoreChanged += _ => Refresh();
    }

    void OnDestroy()
    {
        if (ProgressManager.Instance != null)
            ProgressManager.Instance.OnBestScoreChanged -= _ => Refresh();
    }

    void Refresh()
    {
        if (bestText)
        {
            int ms = ProgressManager.Instance.Data.bestTimeMs;
            bestText.text = $"BEST: {TimeUtils.FormatMsClock(ms, 1)}"; // "03:27.5" 형태
        }

    }

    public void OnClickStart()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
