using System.Collections.Generic;
using UnityEngine;

public class Rook : ChessPieces
{
    public override List<Vector2Int> generateValidMoves()
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        moves.AddRange(generateLineValidMove(1, 0));
        moves.AddRange(generateLineValidMove(-1, 0));
        moves.AddRange(generateLineValidMove(0, 1));
        moves.AddRange(generateLineValidMove(0, -1));
        return moves;
    }
}
