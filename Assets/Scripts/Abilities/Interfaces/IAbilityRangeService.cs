using System.Collections.Generic;
using UnityEngine;

namespace ChessGame.Cards
{
    /// <summary>
    /// 能力范围服务接口 - 负责计算能力范围
    /// </summary>
    public interface IAbilityRangeService
    {
        /// <summary>
        /// 获取能力可作用的范围
        /// </summary>
        List<Vector2Int> GetAbilityRange(AbilityConfiguration ability, Card card, Vector2Int targetPosition);
    }
} 