using System.Collections.Generic;
using UnityEngine;

public class CardSystem : MonoBehaviour
{
    public bool isWhite;
    public Board board;

    public List<Card> deck = new List<Card>();
    public List<Card> hand = new List<Card>();

    public int maxHandSize = 5;
    public Card selectedCard;
    public EnergyBar energyBar;

    public GameObject cardPrefab;
    public float startedX, startedY;
    public float cardSpacing = 1.4f;

    public CardInfoPanel infoPanel;


    void Awake()
    {
        hand.Clear();
        selectedCard = null;
    }

    public void OnTurnStart()
    {
        if (isWhite) board.blackCardSystem.DeselectCard();
        else board.whiteCardSystem.DeselectCard();

        energyBar.AddEnergy(1);

        DrawCard();
    }

    public void DrawCard()
    {
        Debug.Log($"[DrawCard] hand.Count={hand.Count}, maxHandSize={maxHandSize}");

        if (hand.Count >= maxHandSize)
        {
            Debug.Log("Hand full");
            return;
        }

        GameObject go = Instantiate(cardPrefab, Vector3.zero, Quaternion.identity);
        Card card = go.GetComponent<Card>();
        card.CardInit();
        card.owner = this;

        hand.Add(card);
        RepositionHand();
    }


    void RepositionHand()
    {
        hand.RemoveAll(card => card == null);

        for (int i = 0; i < hand.Count; i++)
        {
            Vector3 pos = new Vector3(
                startedX + i * cardSpacing,
                startedY,
                0
            );
            hand[i].transform.position = pos;
        }
    }

    public void DeselectCard()
    {
        if (selectedCard != null)
        {
            selectedCard.SetSelected(false);
            selectedCard = null;
            infoPanel.Hide();
        }
    }


    public void SelectCard(Card card)
    {
        // ignore if not this player's turn
        if (board.isWhiteTurn != isWhite) return;

        // click same card to use it
        if (selectedCard == card)
        {
            UseSelectedCard();
            return;
        }

        // cancel previous selection
        if (selectedCard != null) DeselectCard();

        // select new card
        selectedCard = card;
        selectedCard.SetSelected(true);
        infoPanel.Show(card);   
    }

    void UseSelectedCard()
    {
        if (selectedCard == null) return;

        if (energyBar.energy < selectedCard.cost)
        {
            Debug.Log("Not enough energy to use this card");
            return;
        }

        energyBar.MinusEnergy(selectedCard.cost);
        Debug.Log($"Use card: {selectedCard.cardName}");

        // TODO: add card effect here

        hand.Remove(selectedCard);
        Destroy(selectedCard.gameObject);

        selectedCard = null;
        infoPanel.Hide();

        RepositionHand();
    }



    public void ResetCards()
    {
        selectedCard = null;
        foreach (var card in hand)
        {
            if (card != null)
            {
                Destroy(card.gameObject);
            }
        }
        hand.Clear();
    }

}
