/*
 * Role hierarchy: Presentation > Audio > Sound > UI
 * Primary responsibility: Implements the focused ButtonClick responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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
