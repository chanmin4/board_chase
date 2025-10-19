using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using TMPro; // TMP 쓸 때

public class LevelTextBinder : MonoBehaviour
{
    public DiskInkLeveler leveler;
    public TextMeshProUGUI text;  // TextMeshProUGUI
    public string format = "Lv. {0}";
    void Awake()
    {
        if (!text) text = GetComponentInChildren<TextMeshProUGUI>(true);
    }

    void OnEnable()
    {
        if (leveler != null)
            leveler.OnLevelChanged.AddListener(OnLevelChanged);
    }
    void OnDisable()
    {
        if (leveler != null)
            leveler.OnLevelChanged.RemoveListener(OnLevelChanged);
    }

    void OnLevelChanged(int lv)
    {
        if (text) text.text = string.Format(format, lv);
    }
}
