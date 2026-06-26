using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "BossRewardCatalog",
    menuName = "Boss Reward/Reward Catalog")]
public class BossRewardCatalogSO : ScriptableObject
{
    [SerializeField] private BossRewardOptionSO[] _options;
    [SerializeField, Min(1)] private int _choiceCount = 3;

    public int ChoiceCount => _choiceCount;

    public BossRewardOptionSO[] PickChoices()
    {
        List<BossRewardOptionSO> pool = new();

        if (_options != null)
        {
            for (int i = 0; i < _options.Length; i++)
            {
                if (_options[i] != null)
                    pool.Add(_options[i]);
            }
        }

        List<BossRewardOptionSO> result = new();

        int count = Mathf.Min(_choiceCount, pool.Count);
        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return result.ToArray();
    }
}
