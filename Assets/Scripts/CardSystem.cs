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
            int r = Random.Range(0, deckData.Count);
            int cardId = deckData[r].id;

            NetDrawCard dc = new NetDrawCard();
            dc.team = isWhite ? 0 : 1;
            dc.cardId = cardId;
            Client.Instance.SendToServer(dc);
        }
    }

    public string GetPendingCardName()
    {
        if (pendingCard != null) return pendingCard.data.cardName;
        return "";
    }

    //local draw
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

    //network draw by cardId
    public void DrawById(int cardId)
    {
        if (hand.Count >= maxHandSize) return;
        if (deckData.Count == 0) return;

        CardData data = deckData.Find(c => c != null && c.id == cardId);
        if (data == null)
        {
            Debug.LogError($"[DrawById] 找不到 cardId={cardId} 的 CardData");
            return;
        }

        GameObject go = Instantiate(cardPrefab, Vector3.zero, Quaternion.identity);
        Card card = go.GetComponent<Card>();
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
            else Debug.Log("現在不是你的回合");
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

        string cName = selectedCard.data.cardName;

        if (cName == "DeathJudgement")
        {
            if (board.stepsThisTurn > 0) { Debug.Log("請在回合開始時使用！"); return; }
            isTargeting = true; pendingCard = selectedCard;
            board.HighlightDeathJudgementTargets(isWhite ? "white" : "black");
            return;
        }
        else if (cName == "SkyCastle" || cName == "FireBall" || cName == "Blood" || cName == "KingChange")
        {
            isTargeting = true;
            pendingCard = selectedCard;
            return;
        }


        int index = hand.IndexOf(selectedCard);
        int cardId = selectedCard.data.id;

        if (index == -1) return;

        if (board.currentTeam != -1) 
        {
            NetUseCard uc = new NetUseCard();
            uc.handIndex = index;
            uc.cardId = cardId;
            Client.Instance.SendToServer(uc);
        }

        UseSpecificCard(index, cardId);
        DeselectCard();
    }

    public void UseSpecificCard(int index, int expectedCardId, int targetX = -1, int targetY = -1)
    {
        if (index < 0 || index >= hand.Count) return;

        Card targetCard = hand[index];
        int actualId = targetCard.data.id;

        if (actualId != expectedCardId)
        {
            Debug.LogWarning($"[同步校正] Index {index} 的卡 ID 是 {actualId}，但預期是 {expectedCardId}。正在搜尋...");

            bool found = false;
            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i].data.id == expectedCardId)
                {
                    Debug.Log($"[同步校正] 在 Index {i} 找到正確卡牌！");
                    targetCard = hand[i];
                    index = i;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                Debug.LogError($"嚴重錯誤：手牌裡找不到 ID={expectedCardId} 的卡片！可能雙方 DeckData 設定不一致。");
                return;
            }
        }

        if (targetCard == null) return;

        if (CardDescriptionUI.Instance != null)
        {
            CardDescriptionUI.Instance.ShowSkillLog(isWhite, targetCard.data.displayName);
        }

        string cName = targetCard.data.cardName;
        if (cName == "FireBall")
        {
            if (targetX != -1 && targetY != -1) ApplyFireBallDamage(targetX, targetY);
        }
        else if (cName == "Blood")
        {
            if (targetX != -1 && targetY != -1)
            {
                ChessPieces target = board.pieces[targetX, targetY];
                if (target != null) target.atk *= 2;
            }
        }
        else if (cName == "KingChange")
        {
            if (targetX != -1 && targetY != -1) ApplyKingChangeEffect(targetX, targetY);
        }

        else if (cName == "Crystal") energyBar.AddEnergy(1);

        energyBar.MinusEnergy(targetCard.data.cost);
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

    public void OnTargetSelected(int x, int y)
    {
        if (!isTargeting || pendingCard == null) return;

        ChessPieces targetPiece = board.pieces[x, y];
        string myTeamString = isWhite ? "white" : "black";
        string cName = pendingCard.data.cardName;

        if (cName == "DeathJudgement")
        {
            if (targetPiece == null || targetPiece.team == myTeamString || targetPiece.type.ToLower() == "king") return;
            if (!board.IsSquareUnderAttack(x, y, myTeamString)) return;
            ExecuteDeathJudgement(targetPiece);
        }
        else if (cName == "SkyCastle")
        {
            if (targetPiece == null || targetPiece.team != myTeamString || targetPiece.type.ToLower() != "rook") return;
            skyCastleRook = targetPiece;
            board.HighlightSkyCastleMoves(skyCastleRook);
            board.selectedPiece = skyCastleRook;
            board.tiles[x, y].setSelected();
            isTargeting = false;
            waitForSkyCastleMove = true;
        }
        else if (cName == "FireBall")
        {
            ExecuteFireBall(x, y);
        }
        else if (cName == "Blood")
        {
            if (targetPiece == null || targetPiece.team != myTeamString) return;
            ExecuteBlood(targetPiece);
        }
        else if (cName == "KingChange")
        {
            if (targetPiece == null || targetPiece.team != myTeamString || targetPiece.type.ToLower() == "king") return;
            ExecuteKingChange(targetPiece);
        }
    }


    public void FinishSkyCastleMove()
    {
        if (!waitForSkyCastleMove) return;

        Debug.Log("天空之城發動成功！已移動");

        if (CardDescriptionUI.Instance != null)
            CardDescriptionUI.Instance.ShowSkillLog(isWhite, pendingCard.data.displayName);

        int index = hand.IndexOf(pendingCard);
        int cardId = pendingCard.data.id;

        //local
        energyBar.MinusEnergy(pendingCard.data.cost);
        hand.RemoveAt(index);
        Destroy(pendingCard.gameObject);
        RepositionHand();


        if (board.currentTeam != -1)
        {
            NetUseCard uc = new NetUseCard();
            uc.handIndex = index;
            uc.cardId = cardId; 
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
        int cardId = pendingCard.data.id;

        board.pieces[targetPiece.X, targetPiece.Y] = null;
        board.tiles[targetPiece.X, targetPiece.Y].clearTile();
        Destroy(targetPiece.gameObject);

        energyBar.MinusEnergy(pendingCard.data.cost);
        board.stepsThisTurn += 2;
        hand.RemoveAt(index);
        Destroy(pendingCard.gameObject);
        RepositionHand();

        if (board.currentTeam != -1)
        {
            NetUseCard uc = new NetUseCard();
            uc.handIndex = index;
            uc.cardId = cardId; 
            Client.Instance.SendToServer(uc);
        }

        CancelTargeting();
        board.CheckTurnEnd();
    }

    private void ExecuteFireBall(int refX, int refY)
    {
        if (CardDescriptionUI.Instance != null)
            CardDescriptionUI.Instance.ShowSkillLog(isWhite, pendingCard.data.displayName);

        int index = hand.IndexOf(pendingCard);
        int cardId = pendingCard.data.id;

        ApplyFireBallDamage(refX, refY);

        energyBar.MinusEnergy(pendingCard.data.cost);
        hand.RemoveAt(index);
        Destroy(pendingCard.gameObject);
        RepositionHand();

        if (board.currentTeam != -1)
        {
            NetUseCard uc = new NetUseCard();
            uc.handIndex = index;
            uc.cardId = cardId;
            uc.targetX = refX;
            uc.targetY = refY;
            Client.Instance.SendToServer(uc);
        }
        CancelTargeting();
    }

    private void ExecuteBlood(ChessPieces targetPiece)
    {
        if (CardDescriptionUI.Instance != null)
            CardDescriptionUI.Instance.ShowSkillLog(isWhite, pendingCard.data.displayName);

        int index = hand.IndexOf(pendingCard);
        int cardId = pendingCard.data.id;

        targetPiece.atk *= 2;

        energyBar.MinusEnergy(pendingCard.data.cost);
        hand.RemoveAt(index);
        Destroy(pendingCard.gameObject);
        RepositionHand();

        if (board.currentTeam != -1)
        {
            NetUseCard uc = new NetUseCard();
            uc.handIndex = index;
            uc.cardId = cardId; 
            uc.targetX = targetPiece.X;
            uc.targetY = targetPiece.Y;
            Client.Instance.SendToServer(uc);
        }
        CancelTargeting();
    }

    private void ExecuteKingChange(ChessPieces targetPiece)
    {
        if (CardDescriptionUI.Instance != null)
            CardDescriptionUI.Instance.ShowSkillLog(isWhite, pendingCard.data.displayName);

        int index = hand.IndexOf(pendingCard);
        int cardId = pendingCard.data.id;

        int savedTargetX = targetPiece.X;
        int savedTargetY = targetPiece.Y;

        ApplyKingChangeEffect(savedTargetX, savedTargetY);

        energyBar.MinusEnergy(pendingCard.data.cost);
        hand.RemoveAt(index);
        Destroy(pendingCard.gameObject);
        RepositionHand();

        if (board.currentTeam != -1)
        {
            NetUseCard uc = new NetUseCard();
            uc.handIndex = index;
            uc.cardId = cardId;
            uc.targetX = savedTargetX;
            uc.targetY = savedTargetY;
            Client.Instance.SendToServer(uc);
        }
        CancelTargeting();
    }

    private void ApplyFireBallDamage(int refX, int refY)
    {

        string enemyTeam = isWhite ? "black" : "white";

        Debug.Log($"[Fireball] {(isWhite ? "White" : "Black")} attacks center ({refX}, {refY})");


        if (!isWhite)
        {
            for (int x = refX; x > refX - 2; x--)
            {
                for (int y = refY; y > refY - 2; y--)
                {
                    if (board.isOnBoard(x, y)) Hit(x, y, enemyTeam);
                }
            }
        }
        else 
        {
            for (int x = refX; x < refX + 2; x++)
            {
                for (int y = refY; y < refY + 2; y++)
                {
                    if (board.isOnBoard(x, y)) Hit(x, y, enemyTeam);
                }
            }
        }
    }

    void Hit(int x, int y, string enemyTeam)
    {
        ChessPieces target = board.pieces[x, y];
        if (target != null && target.team == enemyTeam)
        {
            target.hp -= 1;
            if (target.hp <= 0)
            {
                board.pieces[x, y] = null;
                board.tiles[x, y].clearTile();
                if (target.type == "king") GameUI.Instance.OnGameWon(isWhite ? 0 : 1);
                Destroy(target.gameObject);
            }
        }
    }

    private void ApplyKingChangeEffect(int targetX, int targetY)
    {
        ChessPieces targetPiece = board.pieces[targetX, targetY];
        string myTeamString = isWhite ? "white" : "black";
        ChessPieces myKing = null;
        foreach (var piece in board.pieces)
        {
            if (piece != null && piece.type.ToLower() == "king" && piece.team == myTeamString)
            {
                myKing = piece;
                break;
            }
        }

        if (myKing != null && targetPiece != null)
        {
            int kingX = myKing.X; int kingY = myKing.Y;
            int targetPX = targetPiece.X; int targetPY = targetPiece.Y;

            board.pieces[kingX, kingY] = targetPiece;
            board.pieces[targetPX, targetPY] = myKing;

            myKing.X = targetPX; myKing.Y = targetPY;
            targetPiece.X = kingX; targetPiece.Y = kingY;

            if (board.tiles[myKing.X, myKing.Y] != null)
                myKing.transform.position = board.tiles[myKing.X, myKing.Y].transform.position;
            if (board.tiles[targetPiece.X, targetPiece.Y] != null)
                targetPiece.transform.position = board.tiles[targetPiece.X, targetPiece.Y].transform.position;
        }
    }

    public void ResetCards()
    {
        selectedCard = null;
        if (CardDescriptionUI.Instance != null)
        {
            CardDescriptionUI.Instance.Hide();
            CardDescriptionUI.Instance.HidePieceInfo();
        }
        foreach (var card in hand) if (card != null) Destroy(card.gameObject);
        hand.Clear();
    }

    public void CancelTargeting()
    {
        DeselectCard();
    }
}