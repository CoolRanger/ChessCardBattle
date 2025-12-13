using System.Collections.Generic;
using UnityEngine;

public class CardSystem : MonoBehaviour
{
    public List<Card> deck = new List<Card>();
    public List<Card> hand = new List<Card>();

    public int maxHandSize = 5;
    public EnergyBar energyBar;


    // 回合開始呼叫
    public void OnTurnStart()
    {
        // 回能
        energyBar.AddEnergy(1);

        // 抽牌
        //DrawCard();
    }

    public void DrawCard()
    {

        if (hand.Count >= maxHandSize)
        {
            Debug.Log("Hand full");
            return;
        }

        Card card = deck[Random.Range(0, deck.Count)];
        hand.Add(card);

        Debug.Log($"Draw card: {card.cardName}");
    }

}
