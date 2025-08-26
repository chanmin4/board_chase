using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonClick : MonoBehaviour
{
    public string key = "sfx.buttonclick";
    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            if (AudioMaster.I) AudioMaster.I.PlayKey(key);
        });
    }
}
