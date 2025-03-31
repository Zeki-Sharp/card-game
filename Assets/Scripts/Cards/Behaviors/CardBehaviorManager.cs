using UnityEngine;
using System.Collections.Generic;

namespace ChessGame.Cards
{
    // 临时保留的类，将逐步移除
    public class CardBehaviorManager
    {
        private static CardBehaviorManager _instance;
        public static CardBehaviorManager Instance => _instance ?? (_instance = new CardBehaviorManager());
        
        // 空方法，不执行任何操作
        public void ModifyMovablePositions(Card card, int boardWidth, int boardHeight, Dictionary<Vector2Int, Card> allCards, ref List<Vector2Int> movablePositions)
        {
            // 不执行任何操作
        }
        
        // 空方法，不执行任何操作
        public void ModifyAttackablePositions(Card card, int boardWidth, int boardHeight, Dictionary<Vector2Int, Card> allCards, ref List<Vector2Int> attackablePositions)
        {
            // 不执行任何操作
        }
        
        // 空方法，不执行任何操作
        public void SetCardBehaviors(Card card, MovementType movementType, AttackType attackType)
        {
            // 不执行任何操作
        }
    }
} 