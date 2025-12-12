using System.Collections.Generic;
using UnityEngine;

public class Bishop : ChessPieces
{
    public override List<Vector2Int> generateValidMoves()
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        moves.AddRange(generateLineValidMove(1, 1));
        moves.AddRange(generateLineValidMove(1, -1));
        moves.AddRange(generateLineValidMove(-1, 1));
        moves.AddRange(generateLineValidMove(-1, -1));
        return moves;
    }
}
