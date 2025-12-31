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

    public AudioClip drawCardSound;
    public AudioClip menuBackgoundSound;

    void Awake()
    {
        hand.Clear();
        selectedCard = null;

        // 【新增】自動尋找場景中的 EnergyBar，防止 Inspector 忘記拉
        if (energyBar == null)
        {
            energyBar = FindFirstObjectByType<EnergyBar>();
            if (energyBar == null)
            {
                Debug.LogError("嚴重錯誤：場景中找不到 EnergyBar 物件！");
            }
        }
    }

    private void Update()
    {
        if (PromotionUI.Instance != null && PromotionUI.Instance.IsActive) return;
    }

    public void OnTurnStart()
    {
        if (isWhite)
        {
            board.blackCardSystem.DeselectCard();
            board.lastWhiteMoved = null;
            for (int i = 0; i < 8; i++) board.tiles[i, 5].is_enpassant = false;
        }
        else
        {
            board.whiteCardSystem.DeselectCard();
            board.lastBlackMoved = null;
            for (int i = 0; i < 8; i++) board.tiles[i, 2].is_enpassant = false;
        }
        

        energyBar.AddEnergy(2);

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
        if (AudioManager.Instance != null && drawCardSound != null) AudioManager.Instance.PlaySFX(drawCardSound);
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
        if (AudioManager.Instance != null && drawCardSound != null) AudioManager.Instance.PlaySFX(drawCardSound);
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
        else if (cName == "SkyCastle" || cName == "FireBall" || cName == "Blood" || cName == "KingChange" 
            || cName == "Poison" || cName == "Rush" || cName == "Heart" || cName == "Magic")
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

    public void UseSpecificCard(int index, int expectedCardId, int targetX = -1, int targetY = -1, int stepCost = 0)
    {
        if (index < 0 || index >= hand.Count) return;

        Card targetCard = hand[index];
        int actualId = targetCard.data.id;

        PlayCardSound(targetCard.data);

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
            // 印出目前手牌所有 ID，幫助對照
            string currentHandIds = "";
            foreach(var c in hand) currentHandIds += c.data.id + ", ";
            
            Debug.LogError($"嚴重錯誤：手牌同步失敗！\n" +
                        $"預期尋找 ID: {expectedCardId}\n" +
                        $"指定 Index: {index}\n" +
                        $"目前手牌數量: {hand.Count}\n" +
                        $"目前手牌內容 IDs: [{currentHandIds}]");
                        
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

        else if (cName == "Royal")
        {
            string myTeam = isWhite ? "white" : "black";

            foreach (var piece in board.pieces)
            {
                if (piece != null && piece.team == myTeam)
                {
                    if ((piece.type == "king" && piece.hp <= 7) || (piece.type == "queen" && piece.hp <= 4))
                    {
                        piece.hp = piece.maxHP;
                    }
                }
            }
        }
        else if (cName == "Wall")
        {
            string myTeam = isWhite ? "white" : "black";
            foreach (var piece in board.pieces)
            {
                if (piece != null && piece.team == myTeam && piece.type == "pawn")
                    piece.hp = 10;
            }
        }
        else if (cName == "Poison")
        {
            if (targetX != -1 && targetY != -1)
            {
                ChessPieces target = board.pieces[targetX, targetY];
                if (target != null) ApplyPoisonEffect(target);
            }
        }
        else if (cName == "Rush")
        {
            if (targetX != -1 && targetY != -1)
            {
                ChessPieces target = board.pieces[targetX, targetY];
                if (target != null)
                {
                    
                    ApplyRushEffect(target);
                }
            }
        }
        else if (cName == "Heart")
        {
            if (targetX != -1 && targetY != -1)
            {
                ChessPieces target = board.pieces[targetX, targetY];
                if (target != null) target.hp += 2;
            }
        }
        else if (cName == "Magic")
        {
            int sourceX = targetX / 10;
            int destX = targetX % 10;

            int sourceY = targetY / 10;
            int destY = targetY % 10;

            if (board.isOnBoard(sourceX, sourceY) && board.isOnBoard(destX, destY))
            {
                ChessPieces target = board.pieces[sourceX, sourceY];
                if (target != null) ApplyMagicEffect(target, destX, destY);
            }
        }

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

        if (stepCost > 0)
{
        board.stepsThisTurn += stepCost;

        if (CardDescriptionUI.Instance != null && board.stepsThisTurn < 2)
            CardDescriptionUI.Instance.UpdateTurnText(board.isWhiteTurn, board.stepsThisTurn);

        board.CheckTurnEnd();
}
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
        else if (cName == "Poison")
        {
            if (targetPiece == null || targetPiece.team != myTeamString) return;
            ExecutePoison(targetPiece);
        }
        else if (cName == "Rush")
        {
            if (targetPiece == null || targetPiece.team != myTeamString || targetPiece.type != "pawn" || !IsPathClear(targetPiece)) return;
            ExecuteRush(targetPiece);
        }
        else if(cName == "Heart")
        {
            if (targetPiece == null || targetPiece.team != myTeamString) return;
            ExecuteHeart(targetPiece);
        }
        else if (cName == "Magic")
        {
            if (targetPiece == null || targetPiece.team != myTeamString) return;
            if (board.stepsThisTurn >= 2) return;
            ExecuteMagic(targetPiece);
        }
    }


    public void FinishSkyCastleMove()
    {
        if (!waitForSkyCastleMove) return;

        PlayCardSound(pendingCard.data);

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
        PlayCardSound(pendingCard.data);
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
        PlayCardSound(pendingCard.data);
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
        PlayCardSound(pendingCard.data);
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
        PlayCardSound(pendingCard.data);
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


        if (!isWhite && board.currentTeam!=-1)
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

    private void ExecutePoison(ChessPieces sourcePiece)
    {
        PlayCardSound(pendingCard.data);
        if (CardDescriptionUI.Instance != null)
            CardDescriptionUI.Instance.ShowSkillLog(isWhite, pendingCard.data.displayName);

        int index = hand.IndexOf(pendingCard);
        int cardId = pendingCard.data.id;

        ApplyPoisonEffect(sourcePiece);

        energyBar.MinusEnergy(pendingCard.data.cost);
        hand.RemoveAt(index);
        Destroy(pendingCard.gameObject);
        RepositionHand();

        if (board.currentTeam != -1)
        {
            NetUseCard uc = new NetUseCard();
            uc.handIndex = index;
            uc.cardId = cardId;
            uc.targetX = sourcePiece.X;
            uc.targetY = sourcePiece.Y;
            Client.Instance.SendToServer(uc);
        }
        CancelTargeting();
    }

    private void ApplyPoisonEffect(ChessPieces sourcePiece)
    {
        List<Vector2Int> validMoves = sourcePiece.generateValidMoves();
        string sourceTeam = sourcePiece.team;

        foreach (var move in validMoves)
        {
            ChessPieces target = board.pieces[move.x, move.y];

            if (target != null && target.team != sourceTeam)
            {
                target.SetPoison(2);
            }
        }
    }

    private bool IsPathClear(ChessPieces pawn)
    {
        int dir = (pawn.team == "white") ? 1 : -1; 
        int endY = (pawn.team == "white") ? 7 : 0; 
        int currentX = pawn.X;

        for (int y = pawn.Y + dir; y != endY + dir; y += dir)
        {
            if (board.pieces[currentX, y] != null) return false;
        }
        return true;
    }

    private void ExecuteRush(ChessPieces pawn)
    {
        CardData data = pendingCard.data; 
        GameObject cardObj = pendingCard.gameObject;
        int cost = data.cost;
        int index = hand.IndexOf(pendingCard);
        int cardId = data.id;
        int originalX = pawn.X;
        int originalY = pawn.Y;

        PlayCardSound(data);
        if (CardDescriptionUI.Instance != null)
            CardDescriptionUI.Instance.ShowSkillLog(isWhite, data.displayName);

        ApplyRushEffect(pawn);

        board.stepsThisTurn += 1;

        if (CardDescriptionUI.Instance != null && board.stepsThisTurn < 2)
            CardDescriptionUI.Instance.UpdateTurnText(board.isWhiteTurn, board.stepsThisTurn);

        int endY = (pawn.team == "white") ? 7 : 0;
        if (pawn.Y != endY)
            board.CheckTurnEnd();

        energyBar.MinusEnergy(cost);
        hand.RemoveAt(index);
        Destroy(cardObj); 
        RepositionHand();

        if (board.currentTeam != -1)
        {
            NetUseCard uc = new NetUseCard();
            uc.handIndex = index;
            uc.cardId = cardId;
            uc.targetX = originalX;
            uc.targetY = originalY;
            uc.stepCost = 1;
            Client.Instance.SendToServer(uc);
        }

        CancelTargeting();
        
    }

   private void ApplyRushEffect(ChessPieces pawn)
{
    int endY = (pawn.team == "white") ? 7 : 0;
    pawn.moveTo(pawn.X, endY);
    board.TryTriggerPromotion(pawn);
}

    private void ExecuteHeart(ChessPieces targetPiece)
    {
        PlayCardSound(pendingCard.data);
        if (CardDescriptionUI.Instance != null)
            CardDescriptionUI.Instance.ShowSkillLog(isWhite, pendingCard.data.displayName);

        int index = hand.IndexOf(pendingCard);
        int cardId = pendingCard.data.id;

        targetPiece.hp += 2;

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

    private void ExecuteMagic(ChessPieces targetPiece)
    {


        PlayCardSound(pendingCard.data);
        List<Vector2Int> emptyTiles = new List<Vector2Int>();
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (board.pieces[x, y] == null)
                {
                    emptyTiles.Add(new Vector2Int(x, y));
                }
            }
        }

        if (emptyTiles.Count == 0) return;

        if (CardDescriptionUI.Instance != null)
            CardDescriptionUI.Instance.ShowSkillLog(isWhite, pendingCard.data.displayName);

        int index = hand.IndexOf(pendingCard);
        int cardId = pendingCard.data.id;

        int r = Random.Range(0, emptyTiles.Count);
        Vector2Int dest = emptyTiles[r];


        int packedX = (targetPiece.X * 10) + dest.x;
        int packedY = (targetPiece.Y * 10) + dest.y;
        

        energyBar.MinusEnergy(pendingCard.data.cost);
        hand.RemoveAt(index);
        Destroy(pendingCard.gameObject);
        RepositionHand();

        ApplyMagicEffect(targetPiece, dest.x, dest.y);

        board.stepsThisTurn += 1;

        if (CardDescriptionUI.Instance != null && board.stepsThisTurn < 2)
            CardDescriptionUI.Instance.UpdateTurnText(board.isWhiteTurn, board.stepsThisTurn);

        // 如果你有做 Magic->pawn 升變，就要像 Rush 一樣判斷是否在終點列才決定要不要 CheckTurnEnd
        board.CheckTurnEnd();

        if (board.currentTeam != -1)
        {
            NetUseCard uc = new NetUseCard();
            uc.handIndex = index;
            uc.cardId = cardId;
            uc.targetX = packedX;
            uc.targetY = packedY;
            uc.stepCost = 1;
            Client.Instance.SendToServer(uc);
        }

        CancelTargeting();
    }

    private void ApplyMagicEffect(ChessPieces piece, int destX, int destY)
    {
        piece.moveTo(destX, destY);
        if (piece.team == "white") board.lastWhiteMoved = null;
        else board.lastBlackMoved = null;
        if (board.TryTriggerPromotion(piece)) return;
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

    private void PlayCardSound(CardData data)
    {
        if (AudioManager.Instance != null && data.cardSfx != null)
        {
            AudioManager.Instance.PlaySFX(data.cardSfx);
        }
    }
}