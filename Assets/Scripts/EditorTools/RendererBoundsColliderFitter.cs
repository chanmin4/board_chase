using UnityEngine;

public class RendererBoundsColliderFitter : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("Root used to search MeshRenderer or SkinnedMeshRenderer. If empty, this transform is used.")]
    [SerializeField] private Transform _rendererSearchRoot;

    [Tooltip("BoxCollider to create or update. If empty, one will be added to this GameObject.")]
    [SerializeField] private BoxCollider _targetCollider;

    [Header("Options")]
    [Tooltip("Include inactive child renderers when calculating bounds.")]
    [SerializeField] private bool _includeInactive = true;

    [Tooltip("Usually true for hurt boxes.")]
    [SerializeField] private bool _isTrigger = true;

    [ContextMenu("Fit Collider From Renderer Bounds")]
    public void FitCollider()
    {
        if (_rendererSearchRoot == null)
            _rendererSearchRoot = transform;

        if (_targetCollider == null)
            _targetCollider = GetComponent<BoxCollider>();

        if (_targetCollider == null)
            _targetCollider = gameObject.AddComponent<BoxCollider>();

        if (!TryGetWorldBounds(out Bounds worldBounds))
        {
            Debug.LogWarning(
                $"[{nameof(RendererBoundsColliderFitter)}] No Renderer found under {_rendererSearchRoot.name}.",
                this);
            return;
        }

        Vector3 localCenter = transform.InverseTransformPoint(worldBounds.center);
        Vector3 localSize = transform.InverseTransformVector(worldBounds.size);

        localSize.x = Mathf.Abs(localSize.x);
        localSize.y = Mathf.Abs(localSize.y);
        localSize.z = Mathf.Abs(localSize.z);

        _targetCollider.center = localCenter;
        _targetCollider.size = localSize;
        _targetCollider.isTrigger = _isTrigger;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(_targetCollider);
        UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
    }

    private bool TryGetWorldBounds(out Bounds combinedBounds)
    {
        combinedBounds = default;

        Renderer[] renderers =
            _rendererSearchRoot.GetComponentsInChildren<Renderer>(_includeInactive);

        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null)
                continue;

            if (!hasBounds)
            {
                combinedBounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }
}
