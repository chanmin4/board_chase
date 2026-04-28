using UnityEngine;
//contact damage적용할곳에  달아주면됨 적의contact damage를 주는 여러부위들에 부착
public class EnemyContactAttack : MonoBehaviour
{
    private EnemyContactDamageManager _owner;

    public void Initialize(EnemyContactDamageManager owner)
    {
        _owner = owner;
    }

    private void OnTriggerStay(Collider other)
    {
        if (_owner == null)
            return;

        _owner.TryDamage(other);
    }
}
