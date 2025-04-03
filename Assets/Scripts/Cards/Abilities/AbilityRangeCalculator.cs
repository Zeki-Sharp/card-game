using System.Collections.Generic;
using UnityEngine;

namespace ChessGame.Cards
{
    /// <summary>
    /// 能力范围计算器 - 计算能力可以作用的范围
    /// </summary>
    public class AbilityRangeCalculator
    {
        private CardManager _cardManager;
        private AbilityConditionResolver _conditionResolver;
        
        public AbilityRangeCalculator(CardManager cardManager, AbilityConditionResolver conditionResolver)
        {
            _cardManager = cardManager;
            _conditionResolver = conditionResolver;
        }
        
        /// <summary>
        /// 获取能力可作用的所有位置
        /// </summary>
        public List<Vector2Int> GetAbilityRange(AbilityConfiguration ability, Card sourceCard)
        {
            List<Vector2Int> validPositions = new List<Vector2Int>();
            
            // 遍历棋盘上所有位置
            for (int x = 0; x < _cardManager.BoardWidth; x++)
            {
                for (int y = 0; y < _cardManager.BoardHeight; y++)
                {
                    Vector2Int targetPos = new Vector2Int(x, y);
                    
                    // 跳过源位置
                    if (targetPos == sourceCard.Position)
                        continue;
                    
                    // 检查能力是否可以在该位置触发
                    if (_conditionResolver.CheckCondition(ability.triggerCondition, sourceCard, targetPos, _cardManager))
                    {
                        validPositions.Add(targetPos);
                    }
                }
            }
            
            return validPositions;
        }
    }
} 