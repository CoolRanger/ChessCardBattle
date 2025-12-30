using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ChessPieces : MonoBehaviour
{
    public Sprite white, black;
    public SpriteRenderer sr;

    public string team;
    public string type;
    public int X, Y; //it's coord
    public bool isMoving = false; //use for lerp

    public int step = 0;
    public bool canBeEnpassant = false;
    public Board board;

    public int maxHP;
    public int hp;
    public int atk;

    public int poisonTurns = 0;

    private Color originColor;

    public AudioClip moveSound;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        board = Object.FindFirstObjectByType<Board>();
        originColor = sr.color;
        PieceInit();
    }

    public virtual void PieceInit()
    {
        hp = 1;
        maxHP = 1;
        atk = 1;
    }

    //set sprite and variable in this object
    public void SetPiece(string team, string type)
    {
        this.type = type;
        this.team = team;
        if(team=="white") sr.sprite = white;
        else sr.sprite = black;
    }

    
    //for init setup
    public void setPos(int x, int y)
    {
        transform.position = board.tiles[x, y].transform.position;
        X = x; Y = y;
    }



    //for move pieces
    public void moveTo(int x, int y)
    {

       AudioManager.Instance.PlaySFX(moveSound);
        int oldX = X, oldY = Y;
        if (type == "pawn")
        {
            if (step == 0) canBeEnpassant = true;
            else canBeEnpassant = false;
        }
        //for lerp
        Vector3 targetPos = board.tiles[x, y].transform.position; 
        StartCoroutine(smoothlyMoveTo(targetPos, 0.15f));

        //update board
        board.tiles[oldX, oldY].clearTile();
        board.pieces[oldX, oldY] = null;
        board.pieces[x, y] = this;
        X = x; Y = y;
        step++;
        if (team == "white") board.lastWhiteMoved = this;
        else board.lastBlackMoved = this;

        
    }
    private IEnumerator smoothlyMoveTo(Vector3 targetPos, float duration = 0.15f)
    {
        isMoving = true;
        Vector3 startPos = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        transform.position = targetPos;
        isMoving = false;
    }

    void OnMouseUpAsButton()
    {
        if (isMoving) return;
        showValidMoveTile();
        board.OnTileClicked(X, Y);
    }

    public virtual List<Vector2Int> generateValidMoves()
    {
        return new List<Vector2Int>();
    }

    public List<Vector2Int> generateLineValidMove(int dx, int dy)
    {
        List<Vector2Int> ret = new List<Vector2Int>();
        int nextX = X;
        int nextY = Y;
        while (true)
        {
            nextX += dx;
            nextY += dy;
            if (!board.isOnBoard(nextX, nextY)) break;
            ChessPieces cp = board.pieces[nextX, nextY];
            if (cp == null) ret.Add(new Vector2Int(nextX, nextY));
            else
            {
                if (cp.team != team) ret.Add(new Vector2Int(nextX, nextY)); 
                break;
            }
        }
        return ret;
    }

    public List<Vector2Int> generateAttackMoves(List<Vector2Int> moves)
    {
        List<Vector2Int> ret = new List<Vector2Int>();
        foreach (var move in moves)
        {
            ChessPieces cp = board.pieces[move.x, move.y];
            if (cp && cp.team != team)
            {
                ret.Add(move);
            }
        }
        return ret;
    }

    public void showValidMoveTile()
    {
        board.clearAllTile();
        List<Vector2Int> moves = generateValidMoves();
        List<Vector2Int> atkMoves = generateAttackMoves(moves);

        foreach (var move in moves)
        {
            int x = move.x;
            int y = move.y;
            board.tiles[x, y].setLegalMove();
        }

        foreach(var move in atkMoves)
        {
            int x = move.x;
            int y = move.y;

            //can only attack on the first step
            if (board.stepsThisTurn != 0) board.tiles[x, y].clearTile();
            else board.tiles[x, y].setAttackMove();
        }
    }

    public void SetPoison(int turns)
    {
        poisonTurns = turns;
        GetComponent<SpriteRenderer>().color = new Color(0.6f, 1f, 0.6f);
    }

    public void UpdateStatusColor()
    {
        if (poisonTurns <= 0) GetComponent<SpriteRenderer>().color = originColor;
    }

}
