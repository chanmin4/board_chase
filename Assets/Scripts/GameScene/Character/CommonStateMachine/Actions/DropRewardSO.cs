using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "DropReward", menuName = "State Machines/Actions/Drop Reward")]
public class DropRewardSO : StateActionSO
{
	protected override StateAction CreateAction() => new DropReward();
}

public class DropReward : StateAction
{
	private DroppableRewardConfigSO _dropRewardConfig;
	private EnemyLootInventoryRuntime _lootInventory;
	private Transform _currentTransform;


	public override void Awake(StateMachine stateMachine)
	{
		if (stateMachine == null)
			return;

		if (!stateMachine.TryGetComponent(out Damageable damageable))
			damageable = stateMachine.GetComponentInChildren<Damageable>(true);

		_dropRewardConfig = damageable != null ? damageable.DroppableRewardConfig : null;

		if (!stateMachine.TryGetComponent(out _lootInventory))
			_lootInventory = stateMachine.GetComponentInChildren<EnemyLootInventoryRuntime>(true);

		_currentTransform = stateMachine.transform;
	}

	public override void OnUpdate()
	{

	}

	public override void OnStateEnter()
	{
		if (_lootInventory != null && _lootInventory.SuppressRandomDropReward)
			return;

		if (_dropRewardConfig == null)
			return;

		DropAllRewards(_currentTransform.position);
	}

	private void DropAllRewards(Vector3 position)
	{
		DropGroup specialDropItem = _dropRewardConfig.DropSpecialItem(); 
		if (specialDropItem != null) // drops a special item if any 
			DropOneReward(specialDropItem, position);
		// Drop items
		if (_dropRewardConfig.DropGroups == null)
			return;

		foreach (DropGroup dropGroup in _dropRewardConfig.DropGroups)
		{
			if (dropGroup == null)
				continue;

			float randValue = Random.value;
			if (dropGroup.DropRate >= randValue)
			{
				DropOneReward(dropGroup, position);
			}
		}
	}

	private void DropOneReward(DropGroup dropGroup, Vector3 position)
	{
		if (dropGroup == null || dropGroup.Drops == null)
			return;

		float totalWeight = 0f;

		foreach (DropItem dropItem in dropGroup.Drops)
		{
			if (dropItem == null)
				continue;

			totalWeight += Mathf.Max(0f, dropItem.ItemDropRate);
		}

		if (totalWeight <= 0f)
			return;

		float roll = Random.Range(0f, totalWeight);
		DropItem selectedDrop = null;

		foreach (DropItem dropItem in dropGroup.Drops)
		{
			if (dropItem == null)
				continue;

			float weight = Mathf.Max(0f, dropItem.ItemDropRate);
			if (weight <= 0f)
				continue;

			roll -= weight;
			if (roll > 0f)
				continue;

			selectedDrop = dropItem;
			break;
		}

		if (selectedDrop == null || selectedDrop.Item == null)
			return;

		GameObject itemPrefab = selectedDrop.Item.WorldItemPrefab;
		if (itemPrefab == null)
			return;

		float randAngle = Random.value * Mathf.PI * 2;
		GameObject collectibleItem = GameObject.Instantiate(itemPrefab,
			position + itemPrefab.transform.localPosition +
			_dropRewardConfig.ScatteringDistance * (Mathf.Cos(randAngle) * Vector3.forward + Mathf.Sin(randAngle) * Vector3.right),
			Quaternion.identity);

		if (collectibleItem.GetComponentInParent<TreasureRoomRewardPickup>() == null)
		{
			ItemWorldPickup pickup =
				collectibleItem.GetComponent<ItemWorldPickup>() ??
				collectibleItem.GetComponentInChildren<ItemWorldPickup>(true);

			if (pickup == null)
				pickup = collectibleItem.AddComponent<ItemWorldPickup>();

			pickup.Initialize(selectedDrop.Item);
		}
	}
}
