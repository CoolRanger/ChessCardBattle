using System.Collections.Generic;
using UnityEngine;

public class Queen : ChessPieces
{

    public override void PieceInit()
    {
        maxHP = 4;
        hp = 4;
        atk = 4;
    }
    public override List<Vector2Int> generateValidMoves()
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        moves.AddRange(generateLineValidMove(1, 0));
        moves.AddRange(generateLineValidMove(-1, 0));
        moves.AddRange(generateLineValidMove(0, 1));
        moves.AddRange(generateLineValidMove(0, -1));
        moves.AddRange(generateLineValidMove(1, 1));
        moves.AddRange(generateLineValidMove(1, -1));
        moves.AddRange(generateLineValidMove(-1, 1));
        moves.AddRange(generateLineValidMove(-1, -1));
        return moves;
    }
}
