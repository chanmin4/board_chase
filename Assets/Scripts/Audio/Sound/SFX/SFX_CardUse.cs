using UnityEngine;

public class SFX_CardUse : MonoBehaviour
{
    public CardManager card;
    public string key = "sfx.carduse";

    void Reset()    => card ??= GetComponent<CardManager>();
    void OnEnable()
    {
        card ??= GetComponent<CardManager>();
        if (!card) { enabled = false; return; }
        card.CardUse+= Use;
    }
    void OnDisable()
    {
        if (card) card.CardUse -= Use;
    }
    void Use() => AudioMaster.I?.PlayKey(key);
}