using UnityEngine;

/// <summary>
/// An instance of the health of a character, be it the player or an NPC.
/// The initial values are usually contained in another SO of type HealthConfigSO.
/// </summary>
[CreateAssetMenu(fileName = "PlayersHealth", menuName = "EntityConfig/Player's Health")]
public class HealthSO : ScriptableObject
{
	[Tooltip("The initial health")]
	[SerializeField][ReadOnly] private float _maxHealth;
	[SerializeField][ReadOnly] private float _currentHealth;

	public float MaxHealth => _maxHealth;
	public float CurrentHealth => _currentHealth;

	public void SetMaxHealth(float newValue)
	{
		_maxHealth = newValue;
	}

	public void SetCurrentHealth(float newValue)
	{
		_currentHealth = newValue;
	}
	
	public void InflictDamage(float DamageValue)
	{
		_currentHealth -= DamageValue;
	}

	public void RestoreHealth(float HealthValue)
	{
		_currentHealth += HealthValue;
		if(_currentHealth > _maxHealth)
			_currentHealth = _maxHealth;
	}
}
