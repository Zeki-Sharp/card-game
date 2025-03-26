using UnityEngine;
using System.Collections.Generic;

namespace ChessGame
{
    /// <summary>
    /// 卡牌移除行动 - 从棋盘上移除卡牌
    /// </summary>
    public class RemoveCardAction : CardAction
    {
        private Vector2Int _position;
        
        public RemoveCardAction(CardManager cardManager, Vector2Int position) 
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
                Debug.LogWarning($"移除失败：位置 {_position} 没有卡牌");
                return false;
            }
            
            return true;
        }
        
        public override bool Execute()
        {
            if (!CanExecute())
                return false;
                
            // 获取卡牌和视图
            Card card = CardManager.GetCard(_position);
            
            // 从数据结构中移除卡牌
            Dictionary<Vector2Int, Card> cards = CardManager.GetAllCards();
            bool cardRemoved = cards.Remove(_position);
            
            if (!cardRemoved)
            {
                Debug.LogError($"移除卡牌失败：位置 {_position} 的卡牌无法从数据字典中移除");
            }
            
            // 从视图字典中移除卡牌视图
            Dictionary<Vector2Int, CardView> cardViews = CardManager.GetAllCardViews();
            CardView cardView = null;
            
            if (cardViews.TryGetValue(_position, out cardView))
            {
                bool viewRemoved = cardViews.Remove(_position);
                
                if (!viewRemoved)
                {
                    Debug.LogError($"移除卡牌视图失败：位置 {_position} 的卡牌视图无法从视图字典中移除");
                }
                
                // 销毁卡牌视图游戏对象
                if (cardView != null)
                {
                    // 触发销毁动画
                    cardView.PlayDestroyAnimation();
                    
                    // 延迟销毁游戏对象，给动画播放时间
                    GameObject.Destroy(cardView.gameObject, 1.0f);
                }
            }
            
            // 触发移除事件
            GameEventSystem.Instance.NotifyCardRemoved(_position);
            
            Debug.Log($"卡牌 {card.Data.Name} 从位置 {_position} 移除成功");
            
            return true;
        }
    }
} 