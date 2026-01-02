using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines.ExtrusionShapes;

public class Board : MonoBehaviour
{
    //modify in inspector
    public GameObject tilePrefab;
    public GameObject kingPrefab;
    public GameObject queenPrefab;
    public GameObject bishopPrefab;
    public GameObject rookPrefab;
    public GameObject pawnPrefab;
    public GameObject knightPrefab;
    public float padding = 1f;
    public float tileSize = 1f;
    public float startX = -7f;
    public float startY = -7.5f;

    public Tile[,] tiles = new Tile[8, 8];
    public ChessPieces[,] pieces = new ChessPieces[8, 8];

    public ChessPieces selectedPiece = null;

    public bool isWhiteTurn;

    public ChessPieces lastWhiteMoved = null; //for enpassant
    public ChessPieces lastBlackMoved = null;

    public CardSystem whiteCardSystem;
    public CardSystem blackCardSystem;

    public GameObject blackEnergyBarPrefab;
    public GameObject whiteEnergyBarPrefab;

    private bool hasInitialized = false;
    public bool isGameActive = false;

    [System.NonSerialized] public bool isPromotionPending;
    public int stepsThisTurn = 0;

    public AudioClip attackSound;
    public AudioClip menuBGM;
    public AudioClip battleBGM;

    public float attackShakeDuration = 0.2f;
    public float attackShakeMagnitude = 0.3f;

    private List<Tile> redTiles = new List<Tile>();
    private bool redTilesByWhite = false;

    //(0: white, 1: black, -1: Local)
    public int currentTeam = -1;

    void Awake()
    {
        RegisterEvents();
    }

    void OnDestroy()
    {
        UnRegisterEvents();
    }

    [SerializeField] Vector2Int aspectRatio = new(16, 9);
    public bool fullScreen = true;

    void Start()
    {
        Vector2Int size = default;
        float currentRatio = (float)Screen.width / Screen.height;
        float ratioGoal = (float)aspectRatio.x / aspectRatio.y;
        if (currentRatio > ratioGoal)
        {
            size.y = Screen.height;
            size.x = (int)(Screen.height * ratioGoal);
        }
        else
        {
            size.y = (int)(Screen.width / ratioGoal);
            size.x = Screen.width;
        }
        Screen.SetResolution(size.x, size.y, fullScreen);


        if (AudioManager.Instance != null && menuBGM != null) AudioManager.Instance.PlayBGM(menuBGM);
        if (Client.Instance != null)
        {
            Client.Instance.connectionDropped -= OnServerDisconnected;
            Client.Instance.connectionDropped += OnServerDisconnected;
        }
    }

    void Update()
    {
        if (!hasInitialized || !isGameActive || (PromotionUI.Instance != null && PromotionUI.Instance.IsActive)) return;

        if (currentTeam != -1)
        {
            if ((currentTeam == 0 && !isWhiteTurn) || (currentTeam == 1 && isWhiteTurn))
                return;
        }

        CardSystem activeSystem = isWhiteTurn ? whiteCardSystem : blackCardSystem;

        if (activeSystem != null && activeSystem.isTargeting && activeSystem.GetPendingCardName() == "FireBall")
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hitInfo = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hitInfo.collider != null)
            {
                Tile tile = hitInfo.collider.GetComponent<Tile>();
                if (tile != null)
                {
                    HighlightFireBallArea(tile.X, tile.Y);
                }
            }
            else clearAllTile();
        }

        if (!Input.GetMouseButtonDown(0)) return;

        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero);

        if (hit.collider != null) return;

        CancelAllSelections();
    }


    private void ResetGame()
    {
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (pieces[x, y] != null)
                {
                    Destroy(pieces[x, y].gameObject);
                    pieces[x, y] = null;
                }
            }
            if (CardDescriptionUI.Instance != null)
            {
                CardDescriptionUI.Instance.Hide();
                CardDescriptionUI.Instance.HidePieceInfo();
            }
        }

        ResetViewToDefault();
        clearAllTile();
        selectedPiece = null;
        isWhiteTurn = true;

        GeneratePieces();
        isWhiteTurn = true;

        lastWhiteMoved = null;
        lastBlackMoved = null;

        redTiles.Clear();

        whiteCardSystem.ResetCards();
        blackCardSystem.ResetCards();
        whiteCardSystem.energyBar.SetEnergy(0);
        blackCardSystem.energyBar.SetEnergy(0);
        whiteCardSystem.OnTurnStart();

        if (CardDescriptionUI.Instance != null)
            CardDescriptionUI.Instance.UpdateTurnText(true, 0);
    }

    public void StartLocalGame()
    {
        InitBoard();
        if (AudioManager.Instance != null && menuBGM != null) AudioManager.Instance.PlayBGM(battleBGM);
        currentTeam = -1;
        isGameActive = true;
        ResetGame();
    }

    public float blackBarX, blackBarY;
    public float whiteBarX, whiteBarY;

    public void InitBoard()
    {
        if (!hasInitialized)
        {
            GenerateBoard();

            EnergyBar blackBar = Instantiate(blackEnergyBarPrefab).GetComponent<EnergyBar>();
            blackBar.transform.position = new Vector3(blackBarX, blackBarY, 0);
            blackBar.SetEnergy(0);
            blackCardSystem.energyBar = blackBar;

            EnergyBar whiteBar = Instantiate(whiteEnergyBarPrefab).GetComponent<EnergyBar>();
            whiteBar.transform.position = new Vector3(whiteBarX, whiteBarY, 0);
            whiteBar.SetEnergy(0);
            whiteCardSystem.energyBar = whiteBar;

            whiteCardSystem.board = blackCardSystem.board = this;
            whiteCardSystem.isWhite = true;
            blackCardSystem.isWhite = false;

            hasInitialized = true;
        }
    }

    public void ResetViewToDefault()
    {
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (tiles[x, y] != null)
                {
                    tiles[x, y].transform.rotation = Quaternion.identity;
                }
            }
        }
        if (whiteCardSystem != null)
        {
            whiteCardSystem.transform.rotation = Quaternion.identity;
            if (whiteCardSystem.energyBar != null)
                whiteCardSystem.energyBar.transform.rotation = Quaternion.identity;
        }

        if (blackCardSystem != null)
        {
            blackCardSystem.transform.rotation = Quaternion.identity;
            if (blackCardSystem.energyBar != null)
                blackCardSystem.energyBar.transform.rotation = Quaternion.identity;
        }
    }

    public void AdjustViewForBlackTeam()
    {
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (pieces[x, y] != null)
                {
                    pieces[x, y].transform.rotation = Quaternion.Euler(0, 0, 180);
                }

                if (tiles[x, y] != null)
                {
                    tiles[x, y].transform.rotation = Quaternion.Euler(0, 0, 180);
                }
            }
        }

        if (whiteCardSystem.energyBar != null)
            whiteCardSystem.energyBar.transform.rotation = Quaternion.Euler(0, 0, 180);

        if (blackCardSystem.energyBar != null)
            blackCardSystem.energyBar.transform.rotation = Quaternion.Euler(0, 0, 180);

        if (whiteCardSystem != null)
            whiteCardSystem.transform.rotation = Quaternion.Euler(0, 0, 180);

        if (blackCardSystem != null)
            blackCardSystem.transform.rotation = Quaternion.Euler(0, 0, 180);
    }

    public void AddRedTile(int x, int y)
    {

        if (redTiles.Count > 0 && redTilesByWhite != isWhiteTurn) ClearRedTiles();
        redTilesByWhite = isWhiteTurn;

        if (isOnBoard(x, y))
        {
            Tile t = tiles[x, y];
            if (!redTiles.Contains(t))
            {
                t.SetAsLastAttacked(true);
                redTiles.Add(t);
            }
        }
    }

    public void ClearRedTiles()
    {
        foreach (var t in redTiles) if (t != null) t.SetAsLastAttacked(false);
        redTiles.Clear();
    }

    void GenerateBoard()
    {
        //offset
        float sX = (startX * tileSize) / 2f;
        float sY = (startY * tileSize) / 2f;

        //generate tiles
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                Vector3 pos = new Vector3(sX + x * (tileSize + padding), sY + y * (tileSize + padding), 0);
                Tile tile = Instantiate(tilePrefab, pos, Quaternion.identity, transform).GetComponent<Tile>();

                tiles[x, y] = tile;
                tile.SetTile((x + y) % 2 == 0); //determine white or black
                tile.X = x;
                tile.Y = y;
            }
        }
    }

    public void GenerateSinglePiece(string team, string type, int x, int y)
    {
        GameObject prefab;
        if (type == "king") prefab = kingPrefab;
        else if (type == "queen") prefab = queenPrefab;
        else if (type == "rook") prefab = rookPrefab;
        else if (type == "bishop") prefab = bishopPrefab;
        else if (type == "knight") prefab = knightPrefab;
        else if (type == "pawn") prefab = pawnPrefab;
        else prefab = null;

        ChessPieces cp = Instantiate(prefab).GetComponent<ChessPieces>();
        pieces[x, y] = cp;
        cp.SetPiece(team, type);
        cp.setPos(x, y);
    }

    void GeneratePieces()
    {
        GenerateSinglePiece("black", "king", 4, 7);
        GenerateSinglePiece("black", "queen", 3, 7);
        GenerateSinglePiece("black", "bishop", 2, 7);
        GenerateSinglePiece("black", "bishop", 5, 7);
        GenerateSinglePiece("black", "knight", 1, 7);
        GenerateSinglePiece("black", "knight", 6, 7);
        GenerateSinglePiece("black", "rook", 0, 7);
        GenerateSinglePiece("black", "rook", 7, 7);
        for (int i = 0; i < 8; i++)
        {
            GenerateSinglePiece("black", "pawn", i, 6);
        }

        GenerateSinglePiece("white", "king", 4, 0);
        GenerateSinglePiece("white", "queen", 3, 0);
        GenerateSinglePiece("white", "bishop", 2, 0);
        GenerateSinglePiece("white", "bishop", 5, 0);
        GenerateSinglePiece("white", "knight", 1, 0);
        GenerateSinglePiece("white", "knight", 6, 0);
        GenerateSinglePiece("white", "rook", 0, 0);
        GenerateSinglePiece("white", "rook", 7, 0);
        for (int i = 0; i < 8; i++)
        {
            GenerateSinglePiece("white", "pawn", i, 1);
        }
    }

    public void clearAllTile()
    {
        foreach (var tile in tiles)
        {

            tile.clearTile();
        }
    }

    public void HighlightFireBallArea(int refX, int refY)
    {
        clearAllTile(); 
        if(currentTeam == 1)
        {
            for (int x = refX; x > refX - 2; x--)
            {
                for (int y = refY; y > refY - 2; y--)
                {
                    if (isOnBoard(x, y)) tiles[x, y].setAttackMove();
                }
            }
        }
        else
        {
            for (int x = refX; x < refX + 2; x++)
            {
                for (int y = refY; y < refY + 2; y++)
                {
                    if (isOnBoard(x, y)) tiles[x, y].setAttackMove();
                }
            }
        }
    }

    public void OnTileClicked(int x, int y)
    {
        CardSystem activeSystem = isWhiteTurn ? whiteCardSystem : blackCardSystem;

        if (activeSystem != null && activeSystem.isTargeting)
        {
            activeSystem.OnTargetSelected(x, y);
            return;
        }


        ChessPieces targetPiece = pieces[x, y];
        Tile targetTile = tiles[x, y];
        string activeTeamName = isWhiteTurn ? "white" : "black";


        if (selectedPiece == null && currentTeam != -1)
        {
            if (targetPiece != null)
            {
                if (currentTeam == 0 && targetPiece.team == "black") return;
                if (currentTeam == 1 && targetPiece.team == "white") return;
            }
        }

        if (selectedPiece == null)
        {
            if (targetPiece == null) return;
            if (targetPiece.team == activeTeamName)
            {
                clearAllTile();
                selectedPiece = targetPiece;
                selectedPiece.showValidMoveTile();
                targetTile.setSelected();
                return;
            }
            return;
        }
        else if (targetPiece != null && targetPiece.team == activeTeamName)
        {

            if (activeSystem.waitForSkyCastleMove)
            {
                if (targetPiece != activeSystem.skyCastleRook)
                {
                    Debug.Log("選取了其他棋子，取消天空之城狀態");
                    activeSystem.DeselectCard(); 
                }
                else return;
            }
            clearAllTile();
            selectedPiece = targetPiece;
            selectedPiece.showValidMoveTile();
            targetTile.setSelected();
            return;
        }


        int originalX = selectedPiece.X;
        int originalY = selectedPiece.Y;
        bool validMove = false;

        if (isWhiteTurn && lastWhiteMoved == selectedPiece) return;
        if (!isWhiteTurn && lastBlackMoved == selectedPiece) return;

        if (targetTile.is_legal_move)
        { 
            selectedPiece.moveTo(x, y);
            selectedPiece = null;

            if (targetTile.is_left_castling)
            {
                if (isWhiteTurn) pieces[0, 0].moveTo(3, 0);
                else pieces[0, 7].moveTo(3, 7);
            }
            else if (targetTile.is_right_castling)
            {
                if (isWhiteTurn) pieces[7, 0].moveTo(5, 0);
                else pieces[7, 7].moveTo(5, 7);
            }

            stepsThisTurn++;

            if (stepsThisTurn < 2 && CardDescriptionUI.Instance != null)
                CardDescriptionUI.Instance.UpdateTurnText(isWhiteTurn, stepsThisTurn);

            validMove = true;

            ChessPieces movedPiece = pieces[x, y];
            if (TryTriggerPromotion(movedPiece))
            {
                if (currentTeam != -1)
                {
                    NetMakeMove mm = new NetMakeMove();
                    mm.originalX = originalX;
                    mm.originalY = originalY;
                    mm.targetX = x;
                    mm.targetY = y;
                    Client.Instance.SendToServer(mm);
                }
                clearAllTile();
                return;
            }
            CheckTurnEnd();
        }
        else if (targetTile.is_attack_move)
        {
            if (AudioManager.Instance != null && attackSound != null) AudioManager.Instance.PlaySFX(attackSound);
            if (CameraShake.Instance != null) CameraShake.Instance.Shake(attackShakeDuration, attackShakeMagnitude);

            if (selectedPiece.team == "white") lastWhiteMoved = selectedPiece;
            else if (selectedPiece.team == "black") lastBlackMoved = selectedPiece;

            int victimX = x;
            int victimY = y;

            if (targetTile.is_enpassant)
            {
                Debug.Log("enpassant attack");
                if (isWhiteTurn) victimY = y - 1;
                else victimY = y + 1;
                targetPiece = pieces[victimX, victimY];
            }

            int damage = selectedPiece.atk;
            targetPiece.hp -= damage;

            if (targetPiece.hp <= 0)
            {

                pieces[victimX, victimY] = null;

                if (targetPiece.type == "king")
                {
                    int winner = isWhiteTurn ? 0 : 1;
                    Debug.Log(winner == 0 ? "White wins" : "Black wins");
                    Destroy(targetPiece.gameObject);
                    GameUI.Instance.OnGameWon(winner);
                    return;
                }

                Destroy(targetPiece.gameObject);
                selectedPiece.moveTo(x, y);
            }
            else AddRedTile(victimX, victimY);


            selectedPiece = null;
            stepsThisTurn++;

            if (stepsThisTurn < 2 && CardDescriptionUI.Instance != null)
                CardDescriptionUI.Instance.UpdateTurnText(isWhiteTurn, stepsThisTurn);

            validMove = true;

            ChessPieces pieceOnTile = pieces[x, y];
            if (pieceOnTile != null && pieceOnTile.team == activeTeamName)
            {
                if (TryTriggerPromotion(pieceOnTile))
                {
                    if (currentTeam != -1)
                    {
                        NetMakeMove mm = new NetMakeMove();
                        mm.originalX = originalX;
                        mm.originalY = originalY;
                        mm.targetX = x;
                        mm.targetY = y;
                        Client.Instance.SendToServer(mm);
                    }
                    clearAllTile();
                    return;
                }
            }
            CheckTurnEnd();
        }
        else
        {
            selectedPiece = null;
        }


        if (validMove && activeSystem.waitForSkyCastleMove)
        {
            activeSystem.FinishSkyCastleMove();
        }

        clearAllTile();

        //if it was valid and we're in muti, sent it to server
        if (validMove && currentTeam != -1)
        {
            NetMakeMove mm = new NetMakeMove();
            mm.originalX = originalX;
            mm.originalY = originalY;
            mm.targetX = x;
            mm.targetY = y;
            Client.Instance.SendToServer(mm);
        }
    }

    public void CheckTurnEnd()
    {
        if (isPromotionPending) return; 

        if (stepsThisTurn >= 2)
        {
            stepsThisTurn = 0;
            isWhiteTurn = !isWhiteTurn;

            if (CardDescriptionUI.Instance != null)
                CardDescriptionUI.Instance.UpdateTurnText(isWhiteTurn, 0);

            ProcessStatusEffects();
            if (redTiles.Count > 0 && isWhiteTurn == redTilesByWhite) ClearRedTiles();
            if (isWhiteTurn) whiteCardSystem.OnTurnStart();
            else blackCardSystem.OnTurnStart();
        }
    }

    public bool isOnBoard(int x, int y)
    {
        return (x >= 0 && x <= 7 && y >= 0 && y <= 7);
    }

    public bool TryTriggerPromotion(ChessPieces piece)
    {
        if (piece.type != "pawn") return false;

        int endY = (piece.team == "white") ? 7 : 0;
        if (piece.Y != endY) return false;

        isPromotionPending = true;
        if (currentTeam == -1 || IsLocalTurn())
        {
            PromotionUI.Instance.Show(piece.team, (selectedType) =>
            {
                PromotePawn(piece.X, piece.Y, selectedType);
                CheckTurnEnd();  
            });
        }

        return true;
    }

    public void PromotePawn(int x, int y, string newType)
    {
        isPromotionPending = false; 
        ChessPieces pawn = pieces[x, y];
        if (pawn == null) return;

        string team = pawn.team;
        int cx = pawn.X;
        int cy = pawn.Y;

        Destroy(pawn.gameObject);
        pieces[cx, cy] = null;

        GenerateSinglePiece(team, newType, cx, cy);

        if (currentTeam == 1 && pieces[cx, cy] != null)
        {
            pieces[cx, cy].transform.rotation = Quaternion.Euler(0, 0, 180);
        }

        if (currentTeam != -1 && IsLocalTurn())
        {
            NetPromote np = new NetPromote();
            np.x = cx;
            np.y = cy;
            np.newType = newType;
            Client.Instance.SendToServer(np);
        }
    }

    private bool IsLocalTurn()
    {
        return (currentTeam == 0 && isWhiteTurn) || (currentTeam == 1 && !isWhiteTurn);
    }


    public bool IsSquareUnderAttack(int targetX, int targetY, string attackingTeam)
    {
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                ChessPieces piece = pieces[i, j];

                if (piece != null && piece.team == attackingTeam)
                {
                    List<Vector2Int> validMoves = piece.generateValidMoves();

                    foreach (var move in validMoves)
                    {
                        if (move.x == targetX && move.y == targetY)
                        {
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    private void BackToMenu()
    {
        GameUI.Instance.OnMenuButton();
    }


    //card ability

    public void HighlightDeathJudgementTargets(string myTeam)
    {
        clearAllTile(); 

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                ChessPieces piece = pieces[i, j];
                if (piece == null) continue;
                if (piece.team == myTeam) continue;
                if (piece.type.ToLower() == "king") continue;

                if (IsSquareUnderAttack(i, j, myTeam)) tiles[i, j].setAttackMove();
            }
        }
    }
    public void HighlightSkyCastleMoves(ChessPieces rook)
    {
        clearAllTile();

        int[] dx = { 0, 0, 1, -1 };
        int[] dy = { 1, -1, 0, 0 };

        for (int i = 0; i < 4; i++)
        {
            int obstaclesFound = 0;
            for (int dist = 1; dist < 8; dist++)
            {
                int nextX = rook.X + dx[i] * dist;
                int nextY = rook.Y + dy[i] * dist;

                if (!isOnBoard(nextX, nextY)) break;

                ChessPieces targetPiece = pieces[nextX, nextY];

                if (targetPiece == null) tiles[nextX, nextY].setLegalMove();
                else
                {
                    obstaclesFound++;

                    if (obstaclesFound == 1) continue;

                    else if (targetPiece.team != rook.team)
                    {
                        tiles[nextX, nextY].setAttackMove();
                        break;
                    }
                }
            }
        }
    }

    void CancelAllSelections()
    {
        // chesspiece
        if (selectedPiece != null)
        {
            Tile t = tiles[selectedPiece.X, selectedPiece.Y];
            t.clearTile();
            selectedPiece = null;
        }

        clearAllTile();

        //card
        whiteCardSystem.DeselectCard();
        blackCardSystem.DeselectCard();
    }

    public void OnSkipButtonUser()
    {
        if (currentTeam != -1)
        {
            if (currentTeam == 0 && !isWhiteTurn) return;
            if (currentTeam == 1 && isWhiteTurn) return;
        }

        if (currentTeam == -1)
        {
            stepsThisTurn++;
            if (stepsThisTurn < 2 && CardDescriptionUI.Instance != null)
                CardDescriptionUI.Instance.UpdateTurnText(isWhiteTurn, stepsThisTurn);
            CheckTurnEnd();
        }
        else
        {
            Client.Instance.SendToServer(new NetSkipTurn());
        }
    }

    public void ProcessStatusEffects()
    {
        string currentTeamName = isWhiteTurn ? "white" : "black";

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                ChessPieces piece = pieces[x, y];
                if (piece != null && piece.team == currentTeamName && piece.poisonTurns > 0)
                {
                    if (CameraShake.Instance != null) CameraShake.Instance.Shake(attackShakeDuration, attackShakeMagnitude);
                    piece.hp -= 1;
                    piece.poisonTurns -= 1;
                    piece.UpdateStatusColor();

                    if (piece.hp <= 0)
                    {
                        pieces[x, y] = null;
                        tiles[x, y].clearTile();

                        if (piece.type == "king") GameUI.Instance.OnGameWon(isWhiteTurn ? 1 : 0);
                        Destroy(piece.gameObject);
                    }
                }
            }
        }
    }


    #region Network Events

    private void RegisterEvents()
    {
        NetUtility.S_WELCOME += OnWelcomeServer;
        NetUtility.S_MAKE_MOVE += OnMakeMoveServer;

        NetUtility.C_WELCOME += OnWelcomeClient;
        NetUtility.C_MAKE_MOVE += OnMakeMoveClient;

        NetUtility.C_START_GAME += OnStartGameClient;

        NetUtility.S_DRAW_CARD += OnDrawCardServer;
        NetUtility.C_DRAW_CARD += OnDrawCardClient;

        NetUtility.S_USE_CARD += OnUseCardServer;
        NetUtility.C_USE_CARD += OnUseCardClient;

        NetUtility.S_SKIP_STEP += OnSkipServer;
        NetUtility.C_SKIP_STEP += OnSkipClient;

        NetUtility.S_PROMOTE += OnPromoteServer;
        NetUtility.C_PROMOTE += OnPromoteClient;

        NetUtility.C_PLAYER_LEFT += OnPlayerLeftClient;

        NetUtility.S_SURRENDER += OnSurrenderServer;
        NetUtility.C_SURRENDER += OnSurrenderClient;

        if (Client.Instance != null)
            Client.Instance.connectionDropped += OnServerDisconnected;
    }

    private void UnRegisterEvents()
    {
        NetUtility.S_WELCOME -= OnWelcomeServer;
        NetUtility.S_MAKE_MOVE -= OnMakeMoveServer;

        NetUtility.C_WELCOME -= OnWelcomeClient;
        NetUtility.C_MAKE_MOVE -= OnMakeMoveClient;

        NetUtility.C_START_GAME -= OnStartGameClient;

        NetUtility.S_DRAW_CARD -= OnDrawCardServer;
        NetUtility.C_DRAW_CARD -= OnDrawCardClient;

        NetUtility.S_USE_CARD -= OnUseCardServer;
        NetUtility.C_USE_CARD -= OnUseCardClient;

        NetUtility.S_SKIP_STEP -= OnSkipServer;
        NetUtility.C_SKIP_STEP -= OnSkipClient;

        NetUtility.S_PROMOTE -= OnPromoteServer;
        NetUtility.C_PROMOTE -= OnPromoteClient;

        NetUtility.C_PLAYER_LEFT -= OnPlayerLeftClient;

        NetUtility.S_SURRENDER -= OnSurrenderServer;
        NetUtility.C_SURRENDER -= OnSurrenderClient;



        if (Client.Instance != null)
            Client.Instance.connectionDropped -= OnServerDisconnected;
    }


    // Server Side Logic
    private void OnWelcomeServer(NetMessage msg, NetworkConnection cnn)
    {
        Debug.Log("Server received welcome message (if any).");
    }

    private void OnMakeMoveServer(NetMessage msg, NetworkConnection cnn)
    {
        NetMakeMove mm = msg as NetMakeMove;
        Server.Instance.Broadcast(mm);
    }

    private void OnPromoteServer(NetMessage msg, NetworkConnection cnn)
    {
        NetPromote mp = msg as NetPromote;
        Server.Instance.Broadcast(mp);
    }

    private void OnDrawCardServer(NetMessage msg, NetworkConnection cnn)
    {
        NetDrawCard dc = msg as NetDrawCard;
        Server.Instance.Broadcast(dc);
    }

    private void OnUseCardServer(NetMessage msg, NetworkConnection cnn)
    {
        NetUseCard uc = msg as NetUseCard;
        Server.Instance.Broadcast(uc);
    }

    private void OnSkipServer(NetMessage msg, NetworkConnection cnn)
    {
        Server.Instance.Broadcast(msg);
    }

    private void OnServerDisconnected()
    {
        Debug.Log("Server disconnected.");
        BackToMenu();
    }

    private void OnSurrenderServer(NetMessage msg, NetworkConnection cnn)
    {
        NetSurrender ns = msg as NetSurrender;
        Server.Instance.Broadcast(ns);
    }

    // Client Side Logic
    private void OnWelcomeClient(NetMessage msg)
    {
        NetWelcome nw = msg as NetWelcome;
        currentTeam = nw.AssignedTeam;
        Debug.Log($"My team is: {currentTeam}. Waiting for opponent...");

        InitBoard();
    }

    private void OnStartGameClient(NetMessage msg)
    {
        Debug.Log("Both players connected. Starting game!");
        if (AudioManager.Instance != null && battleBGM != null)
            AudioManager.Instance.PlayBGM(battleBGM);
        isGameActive = true;
        ResetGame();
        if (currentTeam == 1) AdjustViewForBlackTeam();
        GameUI.Instance.OnOnlineGameStart();
    }

    private void OnMakeMoveClient(NetMessage msg)
    {
        NetMakeMove mm = msg as NetMakeMove;

        if (!isOnBoard(mm.originalX, mm.originalY) || !isOnBoard(mm.targetX, mm.targetY))
        {
            Debug.LogError("Network move out of bounds!");
            return;
        }



        ChessPieces targetP = pieces[mm.originalX, mm.originalY];
        ChessPieces enemyPiece = pieces[mm.targetX, mm.targetY];

        bool isEnPassant = enemyPiece == null && targetP.type == "pawn" &&
            mm.originalX != mm.targetX;

        if (isEnPassant)
        {
            int epY = (targetP.team == "white") ? mm.targetY - 1 : mm.targetY + 1;
            ChessPieces epPawn = pieces[mm.targetX, epY];
            AddRedTile(mm.targetX, mm.targetY);

            if (epPawn != null)
            {
                if (CameraShake.Instance != null && attackSound != null)
                {
                    CameraShake.Instance.Shake(attackShakeDuration, attackShakeMagnitude);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(attackSound);
                }

                epPawn.hp -= targetP.atk;

                if (epPawn.hp <= 0)
                {
                    pieces[mm.targetX, epY] = null;
                    Destroy(epPawn.gameObject);
                    targetP.moveTo(mm.targetX, mm.targetY);
                }
            }

            stepsThisTurn++;
            if (stepsThisTurn < 2 && CardDescriptionUI.Instance != null)
                CardDescriptionUI.Instance.UpdateTurnText(isWhiteTurn, stepsThisTurn);

            CheckTurnEnd();
            return;
        }


        if (targetP == null) return;

        bool isPieceWhite = (targetP.team == "white");
        int pieceTeamCode = isPieceWhite ? 0 : 1;
        if (pieceTeamCode == currentTeam) return;

        if (enemyPiece != null)
        {
            if (CameraShake.Instance != null)
            {
                CameraShake.Instance.Shake(attackShakeDuration, attackShakeMagnitude);
                if (AudioManager.Instance != null && attackSound != null) AudioManager.Instance.PlaySFX(attackSound);
            }
            int damage = targetP.atk;
            enemyPiece.hp -= damage;
            if (enemyPiece.hp <= 0)
            {
                pieces[mm.targetX, mm.targetY] = null;
                if (enemyPiece.type == "king")
                {
                    Debug.Log("Game Over (Network)");

                    bool isWhiteKing = (enemyPiece.team == "white");
                    int winner = isWhiteKing ? 1 : 0;

                    Destroy(enemyPiece.gameObject);

                    GameUI.Instance.OnGameWon(winner);

                    return;
                }
                Destroy(enemyPiece.gameObject);
                targetP.moveTo(mm.targetX, mm.targetY);
            }
            else AddRedTile(mm.targetX, mm.targetY);
        }
        else
        {
            targetP.moveTo(mm.targetX, mm.targetY);
        }

        if (targetP != null && targetP.type == "king")
        {
            int dx = mm.targetX - mm.originalX;

            if (Mathf.Abs(dx) == 2)
            {
                int y = mm.originalY;
                if (dx < 0)
                {
                    ChessPieces rook = pieces[0, y];
                    if (rook != null && rook.type == "rook")
                        rook.moveTo(3, y);
                }
                else
                {
                    ChessPieces rook = pieces[7, y];
                    if (rook != null && rook.type == "rook")
                        rook.moveTo(5, y);
                }
            }
        }

        stepsThisTurn++;

        if (stepsThisTurn < 2 && CardDescriptionUI.Instance != null)
            CardDescriptionUI.Instance.UpdateTurnText(isWhiteTurn, stepsThisTurn);

        ChessPieces moved = pieces[mm.targetX, mm.targetY];
        if (moved != null && TryTriggerPromotion(moved))
        {
            clearAllTile();
            return; 
        }

        CheckTurnEnd();
    }



    private void OnDrawCardClient(NetMessage msg)
    {
        NetDrawCard dc = msg as NetDrawCard;

        CardSystem cs = (dc.team == 0) ? whiteCardSystem : blackCardSystem;
        cs.DrawById(dc.cardId);
    }

    private void OnUseCardClient(NetMessage msg)
    {
        NetUseCard uc = msg as NetUseCard;

        if ((currentTeam == uc.team)) return;

        CardSystem cs = (uc.team == 0) ? whiteCardSystem : blackCardSystem;

        cs.UseSpecificCardById(
            uc.cardId,
            uc.targetX,
            uc.targetY,
            uc.stepCost
        );
    }

    private void OnSkipClient(NetMessage msg)
    {
        if (GameUI.Instance != null && GameUI.Instance.btnSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(GameUI.Instance.btnSound);
        }

        stepsThisTurn++;
        if (stepsThisTurn < 2 && CardDescriptionUI.Instance != null)
            CardDescriptionUI.Instance.UpdateTurnText(isWhiteTurn, stepsThisTurn);
        CheckTurnEnd();
    }

    private void OnPromoteClient(NetMessage msg)
    {
        NetPromote np = msg as NetPromote;
        //if (IsLocalTurn()) return;
        PromotePawn(np.x, np.y, np.newType.ToString());
        CheckTurnEnd();
    }

    private void OnPlayerLeftClient(NetMessage msg)
    {
        Debug.Log("Opponent has left the game.");
        BackToMenu();
    }

    private void OnSurrenderClient(NetMessage msg)
    {
        NetSurrender ns = msg as NetSurrender;
        Debug.Log($"Team {ns.teamId} has surrendered.");

        int winner = (ns.teamId == 0) ? 1 : 0;

        GameUI.Instance.OnGameWon(winner);
    }
    #endregion
}