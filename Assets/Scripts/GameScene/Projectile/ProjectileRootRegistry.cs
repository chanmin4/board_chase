using UnityEngine;

public class ProjectileRootRegistry : MonoBehaviour
{
    public static Transform Root { get; private set; }

    private void OnEnable()
    {
        Root = transform;
    }

    private void OnDisable()
    {
        if (Root == transform)
            Root = null;
    }
}
