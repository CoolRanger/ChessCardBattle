using NUnit.Framework;
using System;
using Unity.Networking.Transport;
using UnityEngine;

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
    public int stepsThisTurn = 0;

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


    void Start()
    {
        
    }

    void Update()
    {
        if (!hasInitialized || !isGameActive) return;

        if (currentTeam != -1)
        {
            if ((currentTeam == 0 && !isWhiteTurn) || (currentTeam == 1 && isWhiteTurn))
                return;
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
        }


        clearAllTile();
        selectedPiece = null;
        isWhiteTurn = true;

        GeneratePieces();
        isWhiteTurn = true;

        lastWhiteMoved = null;
        lastBlackMoved = null;

        whiteCardSystem.ResetCards();
        blackCardSystem.ResetCards();
        whiteCardSystem.energyBar.SetEnergy(0);
        blackCardSystem.energyBar.SetEnergy(0);
        whiteCardSystem.OnTurnStart();
    }

    public void StartLocalGame()
    {
        InitBoard();
        currentTeam = -1;
        isGameActive = true;
        ResetGame();
    }

    public void InitBoard()
    {
        if (!hasInitialized)
        {
            GenerateBoard();

            EnergyBar blackBar = Instantiate(blackEnergyBarPrefab).GetComponent<EnergyBar>();
            blackBar.transform.position = new Vector3(-8, 0, 0);
            blackBar.SetEnergy(0);
            blackCardSystem.energyBar = blackBar;

            EnergyBar whiteBar = Instantiate(whiteEnergyBarPrefab).GetComponent<EnergyBar>();
            whiteBar.transform.position = new Vector3(-10, 0, 0);
            whiteBar.SetEnergy(0);
            whiteCardSystem.energyBar = whiteBar;

            CardInfoPanel infoPanel = FindFirstObjectByType<CardInfoPanel>(FindObjectsInactive.Include);
            whiteCardSystem.infoPanel = infoPanel;
            blackCardSystem.infoPanel = infoPanel;

            whiteCardSystem.board = blackCardSystem.board = this;
            whiteCardSystem.isWhite = true;
            blackCardSystem.isWhite = false;

            hasInitialized = true;
        }
    }

    void GenerateBoard()
    {
        //offset
        float startX = -(10 * tileSize) / 2f;
        float startY = -(7.5f * tileSize) / 2f;

        //generate tiles
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                Vector3 pos = new Vector3(startX + x * (tileSize + padding), startY + y * (tileSize + padding), 0);
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

    public void OnTileClicked(int x, int y)
    {
        ChessPieces targetPiece = pieces[x, y];
        Tile targetTile = tiles[x, y];
        string activeTeamName = isWhiteTurn ? "white" : "black";

        if (currentTeam != -1)
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
            clearAllTile();
            selectedPiece = targetPiece;
            selectedPiece.showValidMoveTile();
            targetTile.setSelected();
            return;
        }


        int originalX = selectedPiece.X;
        int originalY = selectedPiece.Y;
        bool validMove = false;

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
            else if (targetTile.is_enpassant)
            {
                if (isWhiteTurn)
                {
                    Destroy(pieces[x, y - 1].gameObject);
                    pieces[x, y - 1] = null;
                }
                else
                {
                    Destroy(pieces[x, y + 1].gameObject);
                    pieces[x, y + 1] = null;
                }
            }

            stepsThisTurn++;
            validMove = true;
            CheckTurnEnd();
        }
        else if (targetTile.is_attack_move)
        {
            if (stepsThisTurn != 0)
            {
                Debug.Log("Second move cannot attack");
                selectedPiece = null;
                clearAllTile();
                return;
            }

            int damage = selectedPiece.atk;
            targetPiece.hp -= damage;

            if (targetPiece.hp <= 0)
            {
                pieces[x, y] = null;
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

            selectedPiece = null;
            stepsThisTurn++;
            validMove = true;
            CheckTurnEnd();
        }
        else
        {
            selectedPiece = null;
        }

        clearAllTile();

        //if it was valid and we're not in muti, sent it to server
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

    void CheckTurnEnd()
    {
        if (stepsThisTurn >= 2)
        {
            stepsThisTurn = 0;
            isWhiteTurn = !isWhiteTurn;

            //new turn
            if (isWhiteTurn)
                whiteCardSystem.OnTurnStart();
            else
                blackCardSystem.OnTurnStart();
        }
    }

    public bool isOnBoard(int x, int y)
    {
        return (x >= 0 && x <= 7 && y >= 0 && y <= 7);
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

    #region Network Events

    private void RegisterEvents()
    {
        NetUtility.S_WELCOME += OnWelcomeServer;
        NetUtility.S_MAKE_MOVE += OnMakeMoveServer; 

        NetUtility.C_WELCOME += OnWelcomeClient;    
        NetUtility.C_MAKE_MOVE += OnMakeMoveClient;

        NetUtility.C_START_GAME += OnStartGameClient;
    }

    private void UnRegisterEvents()
    {
        NetUtility.S_WELCOME -= OnWelcomeServer;
        NetUtility.S_MAKE_MOVE -= OnMakeMoveServer;

        NetUtility.C_WELCOME -= OnWelcomeClient;
        NetUtility.C_MAKE_MOVE -= OnMakeMoveClient;

        NetUtility.C_START_GAME -= OnStartGameClient;
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
        isGameActive = true;
        ResetGame();
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
        if (targetP == null) return;

        bool isPieceWhite = (targetP.team == "white");
        int pieceTeamCode = isPieceWhite ? 0 : 1;
        if (pieceTeamCode == currentTeam) return;

        ChessPieces enemyPiece = pieces[mm.targetX, mm.targetY];

        if (enemyPiece != null)
        {
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
        }
        else
        {
            targetP.moveTo(mm.targetX, mm.targetY);
        }

        stepsThisTurn++;
        CheckTurnEnd();
    }
    #endregion
}