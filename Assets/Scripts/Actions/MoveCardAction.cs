using UnityEngine;
using System.Collections.Generic;

namespace ChessGame
{
    /// <summary>
    /// 卡牌移动行动 - 包含完整的移动逻辑
    /// </summary>
    public class MoveCardAction : CardAction
    {
        private Vector2Int _fromPosition;
        private Vector2Int _toPosition;
        
        public MoveCardAction(CardManager cardManager, Vector2Int fromPosition, Vector2Int toPosition) 
            : base(cardManager)
        {
            _fromPosition = fromPosition;
            _toPosition = toPosition;
        }
        
        public override bool CanExecute()
        {
            // 获取卡牌
            Card card = CardManager.GetCard(_fromPosition);
            if (card == null)
            {
                Debug.LogWarning($"移动失败：起始位置 {_fromPosition} 没有卡牌");
                return false;
            }
            
            // 检查目标位置是否合法
            if (!card.CanMoveTo(_toPosition, CardManager.GetAllCards()))
            {
                Debug.LogWarning($"移动失败：目标位置 {_toPosition} 不在合法移动范围内");
                return false;
            }
            
            // 检查目标位置是否已有卡牌
            if (CardManager.GetCard(_toPosition) != null)
            {
                Debug.LogWarning($"移动失败：目标位置 {_toPosition} 已有卡牌");
                return false;
            }
            
            return true;
        }
        
        public override bool Execute()
        {
            if (!CanExecute())
                return false;
                
            // 获取卡牌
            Card card = CardManager.GetCard(_fromPosition);
            
            // 更新卡牌数据位置
            CardManager.UpdateCardPosition(_fromPosition, _toPosition);
            
            // 更新卡牌视图位置
            CardManager.UpdateCardViewPosition(_fromPosition, _toPosition);
            
            // 标记卡牌已行动
            card.HasActed = true;
            
            // 触发移动事件
            GameEventSystem.Instance.NotifyCardMoved(_fromPosition, _toPosition);
            
            Debug.Log($"卡牌 {card.Data.Name} 移动完成：{_fromPosition} -> {_toPosition}");
            
            // 添加调试代码
            Dictionary<Vector2Int, Card> allCards = CardManager.GetAllCards();
            Dictionary<Vector2Int, CardView> allViews = CardManager.GetAllCardViews();

            Debug.Log($"移动后的卡牌数据位置: {string.Join(", ", allCards.Keys)}");
            Debug.Log($"移动后的卡牌视图位置: {string.Join(", ", allViews.Keys)}");

            // 验证卡牌是否在新位置
            if (allCards.ContainsKey(_toPosition))
            {
                Debug.Log($"卡牌数据已在新位置 {_toPosition}");
            }
            else
            {
                Debug.LogError($"卡牌数据不在新位置 {_toPosition}");
            }

            // 验证卡牌视图是否在新位置
            if (allViews.ContainsKey(_toPosition))
            {
                Debug.Log($"卡牌视图已在新位置 {_toPosition}");
            }
            else
            {
                Debug.LogError($"卡牌视图不在新位置 {_toPosition}");
            }
            
            return true;
        }
    }
} 