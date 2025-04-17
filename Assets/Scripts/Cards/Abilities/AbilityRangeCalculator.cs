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
        public List<Vector2Int> GetAbilityRange(AbilityConfiguration ability, Card sourceCard, Vector2Int targetPosition)
        {
            // 根据范围类型选择不同的计算方法
            switch (ability.rangeType)
            {
                case AbilityConfiguration.RangeType.AttackRange:
                    return GetRangeBasedOnAttackRange(sourceCard, ability.rangeCondition, targetPosition);
                    
                case AbilityConfiguration.RangeType.MoveRange:
                    return GetRangeBasedOnMoveRange(sourceCard, ability.rangeCondition, targetPosition);
                    
                case AbilityConfiguration.RangeType.Custom:
                    return GetRangeBasedOnCustomValue(sourceCard, ability.customRangeValue, ability.rangeCondition, targetPosition);
                    
                case AbilityConfiguration.RangeType.Unlimited:
                    return GetUnlimitedRange(sourceCard, ability.rangeCondition, targetPosition);
                    
                case AbilityConfiguration.RangeType.Default:
                default:
                    return GetRangeBasedOnTriggerCondition(ability, sourceCard, targetPosition);
            }
        }
        
        /// <summary>
        /// 基于触发条件获取范围
        /// </summary>
        private List<Vector2Int> GetRangeBasedOnTriggerCondition(AbilityConfiguration ability, Card sourceCard, Vector2Int targetPosition)
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
        
        /// <summary>
        /// 基于攻击范围获取范围
        /// </summary>
        private List<Vector2Int> GetRangeBasedOnAttackRange(Card sourceCard, string additionalCondition, Vector2Int targetPosition)
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
                    
                    // 检查是否在攻击范围内
                    if (sourceCard.CanAttack(targetPos, _cardManager.GetAllCards()))
                    {
                        // 检查额外条件
                        if (string.IsNullOrEmpty(additionalCondition) || 
                            _conditionResolver.CheckCondition(additionalCondition, sourceCard, targetPos, _cardManager))
                        {
                            validPositions.Add(targetPos);
                        }
                    }
                }
            }
            
            return validPositions;
        }
        
        /// <summary>
        /// 基于移动范围获取范围
        /// </summary>
        private List<Vector2Int> GetRangeBasedOnMoveRange(Card sourceCard, string additionalCondition, Vector2Int targetPosition)
        {
            List<Vector2Int> validPositions = new List<Vector2Int>();
            int moveRange = sourceCard.MoveRange;
            
            // 遍历棋盘上所有位置
            for (int x = 0; x < _cardManager.BoardWidth; x++)
            {
                for (int y = 0; y < _cardManager.BoardHeight; y++)
                {
                    Vector2Int targetPos = new Vector2Int(x, y);
                    
                    // 跳过源位置
                    if (targetPos == sourceCard.Position)
                        continue;
                    
                    // 计算曼哈顿距离
                    int distance = Mathf.Abs(targetPos.x - sourceCard.Position.x) + 
                                  Mathf.Abs(targetPos.y - sourceCard.Position.y);
                    
                    // 检查是否在移动范围内
                    if (distance <= moveRange)
                    {
                        // 检查额外条件
                        if (string.IsNullOrEmpty(additionalCondition) || 
                            _conditionResolver.CheckCondition(additionalCondition, sourceCard, targetPos, _cardManager))
                        {
                            validPositions.Add(targetPos);
                        }
                    }
                }
            }
            
            return validPositions;
        }
        
        /// <summary>
        /// 基于自定义值获取范围
        /// </summary>
        private List<Vector2Int> GetRangeBasedOnCustomValue(Card sourceCard, int rangeValue, string additionalCondition, Vector2Int targetPosition)
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
                    
                    // 计算曼哈顿距离
                    int distance = Mathf.Abs(targetPos.x - sourceCard.Position.x) + 
                                  Mathf.Abs(targetPos.y - sourceCard.Position.y);
                    
                    // 检查是否在自定义范围内
                    if (distance <= rangeValue)
                    {
                        // 检查额外条件
                        if (string.IsNullOrEmpty(additionalCondition) || 
                            _conditionResolver.CheckCondition(additionalCondition, sourceCard, targetPos, _cardManager))
                        {
                            validPositions.Add(targetPos);
                        }
                    }
                }
            }
            
            return validPositions;
        }
        
        /// <summary>
        /// 获取无限范围（全场）
        /// </summary>
        private List<Vector2Int> GetUnlimitedRange(Card sourceCard, string additionalCondition, Vector2Int targetPosition)
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
                    
                    // 检查额外条件
                    if (string.IsNullOrEmpty(additionalCondition) || 
                        _conditionResolver.CheckCondition(additionalCondition, sourceCard, targetPos, _cardManager))
                    {
                        validPositions.Add(targetPos);
                    }
                }
            }
            
            return validPositions;
        }

        /// <summary>
        /// 获取基础攻击的有效范围
        /// </summary>
        public List<Vector2Int> GetBasicAttackRange(Card sourceCard)
        {
            List<Vector2Int> validPositions = new List<Vector2Int>();
            
            // 遍历所有卡牌位置
            foreach (var pair in _cardManager.GetAllCards())
            {
                Vector2Int position = pair.Key;
                Card targetCard = pair.Value;
                
                // 检查是否是敌方卡牌且在攻击范围内
                if (targetCard.OwnerId != sourceCard.OwnerId && 
                    sourceCard.CanAttack(position, _cardManager.GetAllCards()))
                {
                    validPositions.Add(position);
                }
            }
            
            return validPositions;
        }
    }
} 