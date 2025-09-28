using UnityEngine;

public class Sfx_DragChargeOn : MonoBehaviour
{
    public DiskLauncher drag;
    public string key = "sfx.dragchargeon";

    void Reset()    => drag ??= GetComponent<DiskLauncher>();
    void OnEnable()
    {
        drag ??= GetComponent<DiskLauncher>();
        if (!drag) { enabled = false; return; }
        drag.DragChargeOn+= Charge;
    }
    void OnDisable()
    {
        if (drag) drag.DragChargeOn -= Charge;
    }
    void Charge() => AudioMaster.I?.PlayKey(key);
}
