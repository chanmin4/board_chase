using UnityEngine;

public abstract class CardAbility : MonoBehaviour
{
    public abstract void Activate(Transform player, SurvivalDirector director, CardData data);
    public abstract void StopNow();
    public virtual bool IsRunning { get; protected set; }
}
