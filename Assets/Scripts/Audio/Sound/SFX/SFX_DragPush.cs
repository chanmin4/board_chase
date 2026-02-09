/*
using UnityEngine;

public class Sfx_DragPush : MonoBehaviour
{
    public DragAimController drag;
    public string key = "sfx.dragpush";

    void Reset()    => drag ??= GetComponent<DragAimController>();
    void OnEnable()
    {
        drag ??= GetComponent<DragAimController>();
        if (!drag) { enabled = false; return; }
        drag.DragPush += Fire;
    }
    void OnDisable()
    {
        if (drag) drag.DragPush -= Fire;
    }
    void Fire() => AudioMaster.I?.PlayKey(key);
}
*/