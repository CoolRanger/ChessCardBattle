using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Pawn : ChessPieces
{
    public override void PieceInit()
    {
        maxHP = 1;
        hp = 1;
        atk = 1;
    }
    public override List<Vector2Int> generateValidMoves()
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        if (team == "white")
        {
            if (board.isOnBoard(X, Y + 1) && board.pieces[X, Y + 1] == null) moves.Add(new Vector2Int(X, Y + 1));
            if (step == 0 && board.pieces[X, Y + 1] == null && board.pieces[X, Y + 2] == null) moves.Add(new Vector2Int(X, Y + 2));
            if(board.isOnBoard(X + 1, Y + 1) && board.pieces[X + 1, Y + 1] != null && board.pieces[X + 1, Y + 1].team != team) 
                moves.Add(new Vector2Int(X + 1, Y + 1));
            if (board.isOnBoard(X - 1, Y + 1) && board.pieces[X - 1, Y + 1] != null && board.pieces[X - 1, Y + 1].team != team)
                moves.Add(new Vector2Int(X - 1, Y + 1));
        }
        else
        {
            if (board.isOnBoard(X, Y - 1) && board.pieces[X, Y - 1] == null) moves.Add(new Vector2Int(X, Y - 1));
            if (step == 0 && board.pieces[X, Y - 1] == null && board.pieces[X, Y - 2] == null) moves.Add(new Vector2Int(X, Y - 2));
            if (board.isOnBoard(X + 1, Y - 1) && board.pieces[X + 1, Y - 1] != null && board.pieces[X + 1, Y - 1].team != team)
                moves.Add(new Vector2Int(X + 1, Y - 1));
            if (board.isOnBoard(X - 1, Y - 1) && board.pieces[X - 1, Y - 1] != null && board.pieces[X - 1, Y - 1].team != team)
                moves.Add(new Vector2Int(X - 1, Y - 1));
        }

        //enpassant
        if(board.isOnBoard(X-1, Y))
        {
            ChessPieces cp = board.pieces[X - 1, Y];
            if(team == "white")
            {
                if (cp != null && cp.type == "pawn" && cp.team != team && cp.step == 1 && cp.Y == 4 && board.lastBlackMoved == cp)
                {
                    moves.Add(new Vector2Int(X - 1, Y + 1));
                    board.tiles[X - 1, Y + 1].is_enpassant = true;
                }
            }
            else
            {
                if (cp != null && cp.type == "pawn" && cp.team != team && cp.step == 1 && cp.Y == 3 && board.lastWhiteMoved == cp)
                {
                    moves.Add(new Vector2Int(X - 1, Y - 1));
                    board.tiles[X - 1, Y - 1].is_enpassant = true;
                }
            }
            
        }
        if(board.isOnBoard(X + 1, Y))
        {
            ChessPieces cp = board.pieces[X + 1, Y];
            if (team == "white")
            {
                if (cp != null && cp.type == "pawn" && cp.team != team && cp.step == 1 && cp.Y == 4 && board.lastBlackMoved == cp)
                {
                    moves.Add(new Vector2Int(X + 1, Y + 1));
                    board.tiles[X + 1, Y + 1].is_enpassant = true;
                }
            }
            else
            {
                if (cp != null && cp.type == "pawn" && cp.team != team && cp.step == 1 && cp.Y == 3 && board.lastWhiteMoved == cp)
                {
                    moves.Add(new Vector2Int(X + 1, Y - 1));
                    board.tiles[X + 1, Y - 1].is_enpassant = true;
                }
            }
        }
        return moves;
    }
}
