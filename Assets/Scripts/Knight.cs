using System.Collections.Generic;
using UnityEngine;

public class Knight : ChessPieces
{
    public override List<Vector2Int> generateValidMoves()
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        int[,] steps = new int[8, 2] { { 1, -2 }, { 1, 2 }, { -1, -2 }, { -1, 2 }, { 2, 1 }, { 2, -1 }, { -2, -1 }, { -2, 1 } };

        for(int i = 0; i < 8; i++)
        {
            int nextX = X + steps[i, 0];
            int nextY = Y + steps[i, 1];
            if (!board.isOnBoard(nextX, nextY)) continue;
            if (board.pieces[nextX, nextY] && board.pieces[nextX, nextY].team == team) continue;
            moves.Add(new Vector2Int(nextX, nextY));
        }

        return moves;
    }
}
