using UnityEngine;
using System.Collections.Generic;

namespace ChessGame.Cards
{
    public interface IMovementBehavior
    {
        List<Vector2Int> GetMovablePositions(Card card, int boardWidth, int boardHeight, Dictionary<Vector2Int, Card> allCards);
    }
} 