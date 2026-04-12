using UnityEngine;

[CreateAssetMenu(fileName = "HealthConfig", menuName = "EntityConfig/Health Config")]
public class HealthConfigSO : ScriptableObject
{
	[SerializeField] private float _initialHealth;

	public float InitialHealth => _initialHealth;

}
