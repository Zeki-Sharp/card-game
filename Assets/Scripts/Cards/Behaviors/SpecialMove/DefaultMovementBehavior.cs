using UnityEngine;
using System.Collections.Generic;

namespace ChessGame.Cards
{
    public class DefaultMovementBehavior : IMovementBehavior
    {
        public List<Vector2Int> GetMovablePositions(Card card, int boardWidth, int boardHeight, Dictionary<Vector2Int, Card> allCards)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            Vector2Int position = card.Position;
            int moveRange = card.Data.MoveRange;
            
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
                for (int i = 1; i <= moveRange; i++)
                {
                    Vector2Int newPos = position + dir * i;
                    
                    // 检查是否在棋盘范围内
                    if (newPos.x < 0 || newPos.x >= boardWidth || newPos.y < 0 || newPos.y >= boardHeight)
                        break;
                    
                    // 检查是否有其他卡牌
                    if (allCards.ContainsKey(newPos))
                        break;
                    
                    positions.Add(newPos);
                }
            }
            
            return positions;
        }
    }
} 