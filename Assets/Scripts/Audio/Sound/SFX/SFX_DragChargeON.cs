using UnityEngine;

public class Sfx_DragChargeOn : MonoBehaviour
{
    public PlayerDisk playerdisk;
    public string key = "sfx.dragchargeon";

    void Reset()    => playerdisk ??= GetComponent<PlayerDisk>();
    void OnEnable()
    {
        playerdisk ??= GetComponent<PlayerDisk>();
        if (!playerdisk) { enabled = false; return; }
        //playerdisk.DragChargeOn+= Charge;
    }
    void OnDisable()
    {
        //if (playerdisk) drag.DragChargeOn -= Charge;
    }
    void Charge() => AudioMaster.I?.PlayKey(key);
}
