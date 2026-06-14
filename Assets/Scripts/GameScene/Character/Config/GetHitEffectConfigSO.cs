using UnityEngine;

[CreateAssetMenu(fileName = "DamageFlash_GetHitConfig", menuName = "EntityConfig/DamageFlash GetHitConfig")]
public class DamageFlash_GetHitConfigSO : ScriptableObject
{
	[SerializeField] private Color _getHitFlashingColor = default;
	[SerializeField] private float _getHitFlashingDuration = 0.5f;
	[SerializeField] private float _getHitFlashingSpeed = 3.0f;

	public Color GetHitFlashingColor => _getHitFlashingColor;
	public float GetHitFlashingDuration => _getHitFlashingDuration;
	public float GetHitFlashingSpeed => _getHitFlashingSpeed;
}
