using UnityEngine;

namespace ChessGame
{
    /// <summary>
    /// 卡牌翻转行动 - 将卡牌从背面翻转为正面
    /// </summary>
    public class FlipCardAction : CardAction
    {
        private Vector2Int _position;
        
        public FlipCardAction(CardManager cardManager, Vector2Int position) 
            : base(cardManager)
        {
            _position = position;
        }
        
        public override bool CanExecute()
        {
            // 获取卡牌
            Card card = CardManager.GetCard(_position);
            if (card == null)
            {
                Debug.LogWarning($"翻转失败：位置 {_position} 没有卡牌");
                return false;
            }
            
            // 检查卡牌是否为背面状态
            if (!card.IsFaceDown)
            {
                Debug.LogWarning($"翻转失败：卡牌 {card.Data.Name} 已经是正面状态");
                return false;
            }
            
            return true;
        }
        
        public override bool Execute()
        {
            if (!CanExecute())
                return false;
                
            // 获取卡牌
            Card card = CardManager.GetCard(_position);
            
            // 翻转卡牌
            card.FlipToFaceUp();
            
            // 触发翻转事件
            GameEventSystem.Instance.NotifyCardFlipped(_position, false);
            
            // 更新卡牌视图
            CardView cardView = CardManager.GetCardView(_position);
            if (cardView != null)
            {
                cardView.UpdateVisuals();
            }
            
            Debug.Log($"卡牌 {card.Data.Name} 翻转为正面");
            
            return true;
        }
    }
} 