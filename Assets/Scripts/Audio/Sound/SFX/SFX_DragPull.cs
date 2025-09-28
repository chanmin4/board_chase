using UnityEngine;

public class Sfx_DragPull : MonoBehaviour
{
    public DragAimController drag;
    public string key = "sfx.dragpull";

    void Reset()    => drag ??= GetComponent<DragAimController>();
    void OnEnable()
    {
        drag ??= GetComponent<DragAimController>();
        if (!drag){ enabled = false; return; }
        drag.DragPull += Fire;
    }
    void OnDisable()
    {
        if (drag) drag.DragPull -= Fire;
    }
    void Fire()  => AudioMaster.I?.PlayKey(key);   
}
