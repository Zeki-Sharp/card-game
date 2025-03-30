using UnityEngine;
using System.Collections.Generic;

namespace ChessGame.Cards
{
    public interface IAttackBehavior
    {
        List<Vector2Int> GetAttackablePositions(Card card, int boardWidth, int boardHeight, Dictionary<Vector2Int, Card> allCards);
    }
} 