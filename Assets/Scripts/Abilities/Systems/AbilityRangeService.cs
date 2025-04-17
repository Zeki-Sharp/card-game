using System.Collections.Generic;
using UnityEngine;

namespace ChessGame.Cards
{
    /// <summary>
    /// 能力范围服务 - 负责计算能力范围
    /// </summary>
    public class AbilityRangeService : IAbilityRangeService
    {
        private AbilityRangeCalculator _rangeCalculator;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public AbilityRangeService(AbilityRangeCalculator rangeCalculator)
        {
            _rangeCalculator = rangeCalculator;
        }
        
        /// <summary>
        /// 获取能力可作用的范围
        /// </summary>
        public List<Vector2Int> GetAbilityRange(AbilityConfiguration ability, Card card, Vector2Int targetPosition  )
        {
            return _rangeCalculator.GetAbilityRange(ability, card, targetPosition);
        }
    }
} 