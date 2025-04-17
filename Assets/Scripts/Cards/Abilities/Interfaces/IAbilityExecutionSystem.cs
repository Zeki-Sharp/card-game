using System.Collections;
using UnityEngine;

namespace ChessGame.Cards
{
    /// <summary>
    /// 能力执行系统接口 - 负责执行能力
    /// </summary>
    public interface IAbilityExecutionSystem
    {
        /// <summary>
        /// 执行能力
        /// </summary>
        IEnumerator ExecuteAbility(AbilityConfiguration ability, Card card, Vector2Int targetPosition, bool isAutomatic = false);
        
        /// <summary>
        /// 检查能力是否可以执行
        /// </summary>
        bool CanExecuteAbility(AbilityConfiguration ability, Card card, Vector2Int position);
        
        /// <summary>
        /// 获取当前是否正在执行能力
        /// </summary>
        bool IsExecutingAbility { get; }
    }
} 