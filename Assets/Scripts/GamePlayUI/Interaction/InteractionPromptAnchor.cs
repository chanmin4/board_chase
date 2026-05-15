using UnityEngine;

[DisallowMultipleComponent]
public class InteractionPromptAnchor : MonoBehaviour
{
    [SerializeField] private Transform _anchor;

    public Transform Anchor => _anchor != null ? _anchor : transform;
}
