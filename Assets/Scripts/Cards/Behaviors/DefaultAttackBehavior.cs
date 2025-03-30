using UnityEngine;
using System.Collections.Generic;

namespace ChessGame.Cards
{
    public class DefaultAttackBehavior : IAttackBehavior
    {
        public List<Vector2Int> GetAttackablePositions(Card card, int boardWidth, int boardHeight, Dictionary<Vector2Int, Card> allCards)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            Vector2Int position = card.Position;
            int attackRange = card.Data.AttackRange;
            
            // 上下左右攻击
            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(0, 1),   // 上
                new Vector2Int(0, -1),  // 下
                new Vector2Int(-1, 0),  // 左
                new Vector2Int(1, 0)    // 右
            };
            
            foreach (Vector2Int dir in directions)
            {
                for (int i = 1; i <= attackRange; i++)
                {
                    Vector2Int newPos = position + dir * i;
                    
                    // 检查是否在棋盘范围内
                    if (newPos.x < 0 || newPos.x >= boardWidth || newPos.y < 0 || newPos.y >= boardHeight)
                        break;
                    
                    // 检查是否有其他卡牌
                    if (allCards.ContainsKey(newPos))
                    {
                        Card targetCard = allCards[newPos];
                        // 只能攻击敌方卡牌
                        if (targetCard.OwnerId != card.OwnerId)
                        {
                            positions.Add(newPos);
                        }
                        break;
                    }
                }
            }
            
            return positions;
        }
    }
} 