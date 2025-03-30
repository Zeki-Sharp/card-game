using UnityEngine;
using System.Collections.Generic;

namespace ChessGame.Cards
{
    // 刺客攻击：上下左右距离1和2的位置，以及斜向四个方向距离1的位置
    public class AssassinAttackBehavior : IAttackBehavior
    {
        public List<Vector2Int> GetAttackablePositions(Card card, int boardWidth, int boardHeight, Dictionary<Vector2Int, Card> allCards)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            
            // 如果卡牌已经行动过或是背面状态，不能攻击
            if (card.HasActed || card.IsFaceDown)
                return positions;
            
            Vector2Int position = card.Position;
            
            // 上下左右距离1的位置
            Vector2Int[] straightDirections1 = new Vector2Int[]
            {
                new Vector2Int(0, 1),   // 上1
                new Vector2Int(0, -1),  // 下1  
                new Vector2Int(1, 0),   // 右1
                new Vector2Int(-1, 0),  // 左1
            };
            
            // 上下左右距离2的位置
            Vector2Int[] straightDirections2 = new Vector2Int[]
            {
                new Vector2Int(0, 2),   // 上2
                new Vector2Int(0, -2),  // 下2
                new Vector2Int(-2, 0),  // 左2
                new Vector2Int(2, 0)    // 右2
            };
            
            // 斜向四个方向距离1的位置
            Vector2Int[] diagonalDirections = new Vector2Int[]
            {
                new Vector2Int(1, 1),   // 右上
                new Vector2Int(1, -1),  // 右下
                new Vector2Int(-1, 1),  // 左上
                new Vector2Int(-1, -1)  // 左下
            };
            
            // 检查所有方向
            CheckDirections(card, position, straightDirections1, boardWidth, boardHeight, allCards, positions, false);
            CheckDirections(card, position, straightDirections2, boardWidth, boardHeight, allCards, positions, true);
            CheckDirections(card, position, diagonalDirections, boardWidth, boardHeight, allCards, positions, false);
            
            return positions;
        }
        
        private void CheckDirections(Card card, Vector2Int position, Vector2Int[] directions, int boardWidth, int boardHeight, 
                                    Dictionary<Vector2Int, Card> allCards, List<Vector2Int> positions, bool checkMiddle)
        {
            foreach (Vector2Int dir in directions)
            {
                Vector2Int newPos = position + dir;
                
                // 检查是否在棋盘范围内
                if (newPos.x < 0 || newPos.x >= boardWidth || newPos.y < 0 || newPos.y >= boardHeight)
                    continue;
                
                // 如果需要检查中间位置
                if (checkMiddle)
                {
                    // 检查中间位置是否有卡牌阻挡视线
                    Vector2Int midPos = position + new Vector2Int(dir.x / 2, dir.y / 2);
                    if (allCards.ContainsKey(midPos))
                        continue;
                }
                
                // 检查目标位置是否有卡牌
                if (allCards.ContainsKey(newPos))
                {
                    Card targetCard = allCards[newPos];
                    // 只能攻击敌方卡牌或任何背面卡牌
                    if (targetCard.OwnerId != card.OwnerId || targetCard.IsFaceDown)
                    {
                        positions.Add(newPos);
                    }
                }
            }
        }
    }
} 