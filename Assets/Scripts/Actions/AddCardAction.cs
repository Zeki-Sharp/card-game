using UnityEngine;
using System.Collections.Generic;

namespace ChessGame
{
    /// <summary>
    /// 卡牌添加行动 - 向棋盘添加新卡牌
    /// </summary>
    public class AddCardAction : CardAction
    {
        private Card _card;
        private Vector2Int _position;
        
        public AddCardAction(CardManager cardManager, Card card, Vector2Int position) 
            : base(cardManager)
        {
            _card = card;
            _position = position;
        }
        
        public override bool CanExecute()
        {
            // 检查位置是否已被占用
            if (CardManager.GetCard(_position) != null)
            {
                Debug.LogWarning($"添加失败：位置 {_position} 已有卡牌");
                return false;
            }
            
            return true;
        }
        
        public override bool Execute()
        {
            if (!CanExecute())
                return false;
                
            // 更新卡牌位置
            _card.Position = _position;
            
            // 添加到数据结构
            Dictionary<Vector2Int, Card> cards = CardManager.GetAllCards();
            cards[_position] = _card;
            
            // 创建卡牌视图
            CardView cardView = CardManager.CreateCardView(_card, _position);
            
            // 添加到视图字典
            if (cardView != null)
            {
                Dictionary<Vector2Int, CardView> cardViews = CardManager.GetAllCardViews();
                cardViews[_position] = cardView;
            }
            
            // 触发添加事件
            GameEventSystem.Instance.NotifyCardAdded(_position, _card.OwnerId, _card.IsFaceDown);
            
            Debug.Log($"卡牌 {_card.Data.Name} 添加到位置 {_position}");
            
            return true;
        }
    }
} 