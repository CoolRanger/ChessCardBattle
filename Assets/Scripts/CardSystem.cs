using System.Collections.Generic;
using UnityEngine;

public class CardSystem : MonoBehaviour
{
    public bool isWhite;
    public Board board;

    public List<CardData> deckData = new List<CardData>();

    public List<Card> hand = new List<Card>();

    public int maxHandSize = 5;
    public Card selectedCard;
    public EnergyBar energyBar;

    public GameObject cardPrefab;
    public float startedX, startedY;
    public float cardSpacing = 1.4f;

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
        if (hand.Count >= maxHandSize) return;
        if (deckData.Count == 0) return; 

        GameObject go = Instantiate(cardPrefab, Vector3.zero, Quaternion.identity);
        Card card = go.GetComponent<Card>();

        CardData randomData = deckData[Random.Range(0, deckData.Count)];
        card.Init(randomData);
        card.owner = this;

        if (board.currentTeam == 1) card.transform.rotation = Quaternion.Euler(0, 0, 180);

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
            CardDescriptionUI.Instance.Hide();
        }
    }

    public void SelectCard(Card card)
    {

        if (board.isWhiteTurn != isWhite) return;


        if (board.currentTeam != -1)
        {
            if (board.currentTeam == 0 && !isWhite) return;
            if (board.currentTeam == 1 && isWhite) return;
        }


        if (selectedCard == card)
        {
            UseSelectedCard();
            return;
        }

        if (selectedCard != null) DeselectCard();

        selectedCard = card;
        selectedCard.SetSelected(true);

        if (CardDescriptionUI.Instance != null)
            CardDescriptionUI.Instance.Show(card.data);
    }

    void UseSelectedCard()
    {
        if (selectedCard == null) return;

        if (energyBar.energy < selectedCard.data.cost)
        {
            Debug.Log("能量不足！");
            return;
        }

        energyBar.MinusEnergy(selectedCard.data.cost);
        Debug.Log($"使用了卡牌: {selectedCard.data.cardName}");

        hand.Remove(selectedCard);
        Destroy(selectedCard.gameObject);
        selectedCard = null;
        CardDescriptionUI.Instance.Hide();
        RepositionHand();
    }

    public void ResetCards()
    {
        selectedCard = null;

        if (CardDescriptionUI.Instance != null) CardDescriptionUI.Instance.Hide();

        foreach (var card in hand)
        {
            if (card != null) Destroy(card.gameObject);
        }
        hand.Clear();
    }
}