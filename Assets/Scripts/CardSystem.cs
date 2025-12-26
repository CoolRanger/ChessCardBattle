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

    [Header("瞄準系統")]
    public bool isTargeting = false;
    private Card pendingCard;

    [Header("SkyCastle 狀態")]
    public bool waitForSkyCastleMove = false;
    public ChessPieces skyCastleRook = null;

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
        if (board.currentTeam == -1)
        {
            int randomIndex = Random.Range(0, deckData.Count);
            DrawSpecificCard(randomIndex);
        }
        else if (board.currentTeam == (isWhite ? 0 : 1))
        {
            int randomIndex = Random.Range(0, deckData.Count);

            NetDrawCard dc = new NetDrawCard();
            dc.deckIndex = randomIndex;
            Client.Instance.SendToServer(dc);
        }
    }

    public void DrawSpecificCard(int index)
    {
        if (hand.Count >= maxHandSize) return;
        if (deckData.Count == 0) return;

        GameObject go = Instantiate(cardPrefab, Vector3.zero, Quaternion.identity);
        Card card = go.GetComponent<Card>();

        CardData data = deckData[index];
        card.Init(data);
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

        if (waitForSkyCastleMove)
        {
            waitForSkyCastleMove = false;
            skyCastleRook = null;
            pendingCard = null;
            if (board != null) board.clearAllTile();
            Debug.Log("取消天空之城移動");
        }

        if (isTargeting)
        {
            isTargeting = false;
            pendingCard = null;
            if (board != null) board.clearAllTile();
        }

        if (selectedCard != null)
        {
            selectedCard.SetSelected(false);
            selectedCard = null;

            if (CardDescriptionUI.Instance != null)
                CardDescriptionUI.Instance.Hide();
        }
    }

    public void SelectCard(Card card)
    {
        if (board.currentTeam != -1)
        {
            if (board.currentTeam == 0 && !isWhite) return;
            if (board.currentTeam == 1 && isWhite) return;
        }

        if (selectedCard == card)
        {
            if (board.isWhiteTurn == isWhite) UseSelectedCard();
            else Debug.Log("現在不是你的回合，只能看不能用");
            return;
        }

        if (selectedCard != null) DeselectCard();

        selectedCard = card;
        selectedCard.SetSelected(true);

        if (CardDescriptionUI.Instance != null) CardDescriptionUI.Instance.Show(card.data);
    }

    void UseSelectedCard()
    {
        if (selectedCard == null) return;

        if (energyBar.energy < selectedCard.data.cost)
        {
            Debug.Log("能量不足！");
            return;
        }

        if (selectedCard.data.cardName == "DeathJudgement")
        {
            if (board.stepsThisTurn > 0)
            {
                Debug.Log("請在回合開始時使用！");
                return;
            }

            isTargeting = true;
            pendingCard = selectedCard;
            string myTeamString = isWhite ? "white" : "black";
            board.HighlightDeathJudgementTargets(myTeamString);
            Debug.Log("【死神裁決】目標已標示，請點擊...");
            return;
        }

        if (selectedCard.data.cardName == "SkyCastle")
        {
            isTargeting = true;
            pendingCard = selectedCard;
            Debug.Log("【天空之城】請選擇我方的一個 Rook...");
            return;
        }

        int index = hand.IndexOf(selectedCard);
        if (index == -1) return;

        if (board.currentTeam == -1) UseSpecificCard(index);
        else
        {
            NetUseCard uc = new NetUseCard();
            uc.handIndex = index;
            Client.Instance.SendToServer(uc);
            DeselectCard();
        }
    }

    public void UseSpecificCard(int index)
    {
        if (index < 0 || index >= hand.Count) return;

        Card targetCard = hand[index];
        if (targetCard == null) return;

        if (CardDescriptionUI.Instance != null)
        {
            CardDescriptionUI.Instance.ShowSkillLog(isWhite, targetCard.data.displayName);
        }

        energyBar.MinusEnergy(targetCard.data.cost);
        Debug.Log($"[同步] 使用卡牌: {targetCard.data.cardName}");

        hand.RemoveAt(index);
        Destroy(targetCard.gameObject);

        if (selectedCard == targetCard)
        {
            selectedCard = null;
            if (CardDescriptionUI.Instance != null)
                CardDescriptionUI.Instance.Hide();
        }

        RepositionHand();
    }

    public void ResetCards()
    {
        selectedCard = null;

        if (CardDescriptionUI.Instance != null)
        {
            CardDescriptionUI.Instance.Hide();
            CardDescriptionUI.Instance.HidePieceInfo();
        }

        foreach (var card in hand)
        {
            if (card != null) Destroy(card.gameObject);
        }
        hand.Clear();
    }

    public void OnTargetSelected(int x, int y)
    {
        if (!isTargeting || pendingCard == null) return;

        ChessPieces targetPiece = board.pieces[x, y];
        string myTeamString = isWhite ? "white" : "black";

        if (targetPiece == null)
        {
            CancelTargeting();
            return;
        }

        if (pendingCard.data.cardName == "DeathJudgement")
        {
            if (targetPiece.team == myTeamString)
            {
                Debug.Log("無效：不能對自己人使用");
                CancelTargeting();
                return;
            }

            if (targetPiece.type.ToLower() == "king")
            {
                Debug.Log("無效：死神無法帶走國王");
                return;
            }

            if (!board.IsSquareUnderAttack(x, y, myTeamString))
            {
                Debug.Log("無效：目標不在我方任何棋子的攻擊範圍內！");
                return;
            }

            ExecuteDeathJudgement(targetPiece);
        }
        else if (pendingCard.data.cardName == "SkyCastle")
        {
            if (targetPiece.team != myTeamString)
            {
                Debug.Log("無效：天空之城只能作用於我方棋子");
                CancelTargeting();
                return;
            }

            if (targetPiece.type.ToLower() != "rook")
            {
                Debug.Log("無效：只能選擇 Rook (車)");
                return;
            }

            skyCastleRook = targetPiece;

            board.HighlightSkyCastleMoves(skyCastleRook);
            board.selectedPiece = skyCastleRook;
            board.tiles[x, y].setSelected(); 
            isTargeting = false;
            waitForSkyCastleMove = true;
        }
    }

    public void FinishSkyCastleMove()
    {
        if (!waitForSkyCastleMove) return;

        Debug.Log("天空之城發動成功！已移動");

        if (CardDescriptionUI.Instance != null)
            CardDescriptionUI.Instance.ShowSkillLog(isWhite, pendingCard.data.displayName);

        int index = hand.IndexOf(pendingCard);

        if (board.currentTeam == -1) // Local
        {
            energyBar.MinusEnergy(pendingCard.data.cost);
            hand.RemoveAt(index);
            Destroy(pendingCard.gameObject);
            RepositionHand();
        }
        else // Online
        {
            NetUseCard uc = new NetUseCard();
            uc.handIndex = index;
            Client.Instance.SendToServer(uc);
        }

        waitForSkyCastleMove = false;
        skyCastleRook = null;
        pendingCard = null;

        DeselectCard();
    }

    private void ExecuteDeathJudgement(ChessPieces targetPiece)
    {

        int index = hand.IndexOf(pendingCard);

        if (board.currentTeam == -1) 
        {
            energyBar.MinusEnergy(pendingCard.data.cost);
            board.stepsThisTurn += 2;

            board.pieces[targetPiece.X, targetPiece.Y] = null;
            board.tiles[targetPiece.X, targetPiece.Y].clearTile();
            Destroy(targetPiece.gameObject);

            hand.RemoveAt(index);
            Destroy(pendingCard.gameObject);
            RepositionHand();
        }
        else 
        {
            NetUseCard uc = new NetUseCard();
            uc.handIndex = index;
            Client.Instance.SendToServer(uc);

            energyBar.MinusEnergy(pendingCard.data.cost);
            board.stepsThisTurn += 2;

            board.pieces[targetPiece.X, targetPiece.Y] = null;
            board.tiles[targetPiece.X, targetPiece.Y].clearTile();
            Destroy(targetPiece.gameObject);

            hand.RemoveAt(index);
            Destroy(pendingCard.gameObject);
            RepositionHand();
        }

        CancelTargeting();
        board.CheckTurnEnd();
    }

    public void CancelTargeting()
    {
        DeselectCard();
        Debug.Log("取消瞄準");
    }
}