using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour
{
    public string cardName;
    public string description;
    public int cost;          
    public Sprite artwork;

    public CardSystem owner; //white or black
    public Board board;

    SpriteRenderer sr;
    Color normalColor = Color.white;
    Color selectedColor = new Color(0.7f, 0.9f, 1f);

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.color = normalColor;
        board = Object.FindFirstObjectByType<Board>();
    }

    public void CardInit()
    {
        cardName = "test";
        description = "this is a test card";
        cost = 1;
}

    public void SetSelected(bool selected)
    {
        sr.color = selected ? selectedColor : normalColor;
    }

    void OnMouseDown()
    {
        if (!board.isGameActive) return;
        owner.SelectCard(this);
    }
}
