using NUnit.Framework;
using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static UnityEditor.PlayerSettings;

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


    public int stepsThisTurn = 0;


    void Start()
    {
        GenerateBoard();
        GeneratePieces();
        isWhiteTurn = true;

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


        whiteCardSystem.ResetCards();
        blackCardSystem.ResetCards();
        whiteCardSystem.board = blackCardSystem.board = this;
        whiteCardSystem.isWhite = true;
        blackCardSystem.isWhite = false;
        whiteCardSystem.OnTurnStart();
    }

    void Update()
    {
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
                Vector3 pos = new Vector3(startX + x*(tileSize + padding), startY + y*(tileSize + padding), 0);
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
        string currentTeam = isWhiteTurn ? "white" : "black";

        if (selectedPiece == null)
        {
            //there's no piece on the tile, do nothing
            if (targetPiece == null) return;

            //piece belongs to the current team, select it
            if (targetPiece.team == currentTeam)
            {
                clearAllTile();
                selectedPiece = targetPiece;
                selectedPiece.showValidMoveTile();
                targetTile.setSelected();
                return;
            }
            return;
        }
        else if(targetPiece !=null && targetPiece.team == currentTeam) //click on same team => change to it
        {
            clearAllTile();
            selectedPiece = targetPiece;
            selectedPiece.showValidMoveTile();
            targetTile.setSelected();
            return;
        }

        //click on legal move or attack move
        if (targetTile.is_legal_move)
        {
            selectedPiece.moveTo(x, y);
            selectedPiece = null;

            //special move
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

            Debug.Log($"{selectedPiece.type} attacks {targetPiece.type}, dmg={damage}, hp left={targetPiece.hp}");

            if (targetPiece.hp <= 0) //dead
            {
                pieces[x, y] = null;

                // king dead -> game over
                if (targetPiece.type == "king")
                {
                    Debug.Log(isWhiteTurn ? "White wins" : "Black wins");
                    Destroy(targetPiece.gameObject);
                    ResetGame();
                    return;
                }

                Destroy(targetPiece.gameObject);
                selectedPiece.moveTo(x, y);
            }
            else
            {
                // survived
                Debug.Log("Target survived");
            }

            selectedPiece = null;
            stepsThisTurn++;
            CheckTurnEnd();
        }
        else //clicked on illegal move => unselect
        {
            Debug.Log("hi");
            selectedPiece = null;
        }
        clearAllTile();
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
        return (x >= 0 && x<=7 && y >= 0 && y<=7);
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


}
