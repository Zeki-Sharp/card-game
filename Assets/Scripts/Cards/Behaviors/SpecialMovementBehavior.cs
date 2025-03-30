using UnityEngine;
using System.Collections.Generic;

namespace ChessGame.Cards
{
    // 特殊移动：移动距离=2
    public class SpecialMovementBehavior : IMovementBehavior
    {
        public List<Vector2Int> GetMovablePositions(Card card, int boardWidth, int boardHeight, Dictionary<Vector2Int, Card> allCards)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            Vector2Int position = card.Position;
            
            // 固定移动距离为2
            int moveRange = 2;
            
            // 上下左右移动
            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(0, 1),   // 上
                new Vector2Int(0, -1),  // 下
                new Vector2Int(-1, 0),  // 左
                new Vector2Int(1, 0)    // 右
            };
            
            foreach (Vector2Int dir in directions)
            {
                // 只检查距离为2的位置
                Vector2Int newPos = position + dir * moveRange;
                
                // 检查是否在棋盘范围内
                if (newPos.x < 0 || newPos.x >= boardWidth || newPos.y < 0 || newPos.y >= boardHeight)
                    continue;
                
                // 检查是否有其他卡牌
                if (allCards.ContainsKey(newPos))
                    continue;
                
                // 检查中间位置是否有卡牌
                Vector2Int midPos = position + dir;
                if (allCards.ContainsKey(midPos))
                    continue;
                
                positions.Add(newPos);
            }
            
            return positions;
        }
    }
} 