using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemyHealthBarSettings",
    menuName = "Game/UI/Enemy Health Bar Settings")]
public class EnemyHealthBarSettingsSO : ScriptableObject
{
    [Header("Behavior")]
    [SerializeField] private bool _hideWhenFull = false;
    [SerializeField] private bool _hideWhenDead = true;
    [SerializeField] private float _emphasisDuration = 1.2f;

    public bool HideWhenFull => _hideWhenFull;
    public bool HideWhenDead => _hideWhenDead;
    public float EmphasisDuration => _emphasisDuration;
}
