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
                
            // 获取卡牌和视图
            Card card = CardManager.GetCard(_fromPosition);
            CardView cardView = CardManager.GetCardView(_fromPosition);
            
            // 获取数据结构
            Dictionary<Vector2Int, Card> cards = CardManager.GetAllCards();
            Dictionary<Vector2Int, CardView> cardViews = CardManager.GetAllCardViews();
            
            // 更新卡牌位置
            CardManager.UpdateCardPosition(_fromPosition, _toPosition);
            
            // 更新视图位置
            CardManager.UpdateCardViewPosition(_fromPosition, _toPosition);
            
            // 标记卡牌已行动
            card.HasActed = true;
            
            // 触发移动事件 - 动画服务会监听此事件并播放动画
            GameEventSystem.Instance.NotifyCardMoved(_fromPosition, _toPosition);
            
            Debug.Log($"卡牌 {card.Data.Name} 移动完成：{_fromPosition} -> {_toPosition}");
            
            return true;
        }
    }
} 