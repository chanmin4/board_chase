using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonClick : MonoBehaviour
{
    public string key = "ui.buttonclick";
    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            if (AudioMaster.I) AudioMaster.I.PlayKey(key);
        });
    }
}
