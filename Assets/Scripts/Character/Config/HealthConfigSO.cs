using UnityEngine;

/// <summary>
/// this script used for gamescene entity health
/// </summary>
[CreateAssetMenu(fileName = "HealthConfig", menuName = "EntityConfig/Health Config")]
public class HealthConfigSO : ScriptableObject
{
	[SerializeField] private float _initialHealth;

	public float InitialHealth => _initialHealth;

}
