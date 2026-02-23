using UnityEngine;

[CreateAssetMenu(menuName="Cards/CardData", fileName="NewCardData")]
public class CardData : ScriptableObject
{
    [Header("Info")]
    public string cardName = "Cleaner";
    public Sprite icon;

    [Header("Charge")]
    public int maxCharge = 5;
    public int gainPerZoneBounce = 1;
    public int gainPerZoneCritBounce = 2;

    [Header("Activation")]
    public float duration = 6f;
    public float radiusTiles = 1.0f;
    public float cooldown = 0f;

    [Header("Ability Binding")]
    public string abilityType = "CleanTrail";
}
