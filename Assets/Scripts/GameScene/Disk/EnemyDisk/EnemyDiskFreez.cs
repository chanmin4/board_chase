/*
// EnemyDiskDebugFreeze.cs
using System.Collections;
using UnityEngine;

public class EnemyDiskDebugFreeze : MonoBehaviour
{
    public KeyCode toggleKey = KeyCode.F1;

    public EnemyDiskLauncher launcher;  // 적 런처 컴포넌트
    public Rigidbody rb;

    bool frozen;
    Vector3 savedVel, savedAng;
    RigidbodyConstraints savedConstraints;

    void Reset() {
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!launcher) launcher = GetComponent<EnemyDiskLauncher>();
    }

    void Update() {
        if (Input.GetKeyDown(toggleKey)) SetFrozen(!frozen);
    }

    [ContextMenu("Freeze 3s")]
    public void Freeze3s() { StartCoroutine(FreezeForSeconds(3f)); }

    public void SetFrozen(bool on) {
        if (!rb) return;
        if (on == frozen) return;
        frozen = on;

        if (on) {
            if (launcher) launcher.enabled = false;
            savedVel = rb.linearVelocity;
            savedAng = rb.angularVelocity;
            savedConstraints = rb.constraints;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        } else {
            rb.constraints = savedConstraints;
            if (launcher) launcher.enabled = true;
            rb.linearVelocity = savedVel;
            rb.angularVelocity = savedAng;
        }
    }

    IEnumerator FreezeForSeconds(float sec) {
        SetFrozen(true);
        yield return new WaitForSeconds(sec);
        SetFrozen(false);
    }
}

*/