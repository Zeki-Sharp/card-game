using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ChessGame.Cards
{
    /// <summary>
    /// 能力接口 - 定义卡牌能力的基本行为
    /// </summary>
    public interface IAbility
    {
        // 能力名称
        string Name { get; }
        
        // 能力描述
        string Description { get; }
        
        // 检查能力是否可以在当前情况下触发
        bool CanTrigger(Card card, Vector2Int targetPosition, CardManager cardManager);
        
        // 执行能力
        IEnumerator Execute(Card card, Vector2Int targetPosition, CardManager cardManager);
    }
} 