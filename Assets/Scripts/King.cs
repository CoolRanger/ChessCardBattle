using System.Collections.Generic;
using UnityEngine;

public class King : ChessPieces
{
    public override void PieceInit()
    {
        maxHP = 7;
        hp = 7;
        atk = 7;
    }
    public override List<Vector2Int> generateValidMoves()
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        int[,] steps = new int[8, 2] { { 0, -1 }, { 0, 1 }, { 1, 0 }, { -1, 0 }, { 1, 1 }, { 1, -1 }, { -1, -1 }, { -1, 1 } };

        for (int i = 0; i < 8; i++)
        {
            int nextX = X + steps[i, 0];
            int nextY = Y + steps[i, 1];
            if (!board.isOnBoard(nextX, nextY)) continue;
            if (board.pieces[nextX, nextY] && board.pieces[nextX, nextY].team == team) continue;
            moves.Add(new Vector2Int(nextX, nextY));
        }

        //castling
        if (step == 0)
        {
            if(team == "white")
            {
                ChessPieces left = board.pieces[0, 0];
                ChessPieces right = board.pieces[7, 0];
                if (left != null)
                {
                    if (board.pieces[1,0] == null && board.pieces[2, 0] == null && board.pieces[3, 0] == null && left.step == 0)
                    {
                        moves.Add(new Vector2Int(2, 0));
                        board.tiles[2, 0].is_left_castling = true;
                    }
                }
                if (right != null)
                {
                    if (board.pieces[5, 0] == null && board.pieces[6, 0] == null && right.step == 0)
                    {
                        moves.Add(new Vector2Int(6, 0));
                        board.tiles[6, 0].is_right_castling = true;
                    }
                }
                
            }
            else
            {
                ChessPieces left = board.pieces[0, 7];
                ChessPieces right = board.pieces[7, 7];
                if (left != null)
                {
                    if (board.pieces[1, 7] == null && board.pieces[2, 7] == null && board.pieces[3, 7] == null && left.step == 0)
                    {
                        moves.Add(new Vector2Int(2, 7));
                        board.tiles[2, 7].is_left_castling = true;
                    }
                }
                if (right != null)
                {
                    if (board.pieces[5, 7] == null && board.pieces[6, 7] == null && right.step == 0)
                    {   
                        moves.Add(new Vector2Int(6, 7));
                        board.tiles[6, 7].is_right_castling = true;
                    }
                }
            }
        }
        return moves;
    }
}
