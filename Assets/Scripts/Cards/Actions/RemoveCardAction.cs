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
            
            Debug.Log($"RemoveCardAction.Execute: 开始移除位置 {_position} 的卡牌 {card?.Data?.Name}，生命值: {card?.Data?.Health}");
            
            // 首先触发移除事件，这应该在实际移除卡牌之前发生，从而允许其他系统（如动画系统）响应
            Debug.Log($"触发卡牌移除事件: {_position}");
            GameEventSystem.Instance.NotifyCardRemoved(_position);
            
            // 从数据结构中移除卡牌
            Dictionary<Vector2Int, Card> cards = CardManager.GetAllCards();
            
            // 检查卡牌是否存在于字典中
            if (!cards.ContainsKey(_position))
            {
                Debug.LogError($"移除卡牌失败：位置 {_position} 的卡牌不存在于数据字典中");
                return false;
            }
            
            // 保存对卡牌的引用，以便后续使用
            string cardName = card?.Data?.Name ?? "未知卡牌";
            
            // 尝试移除卡牌
            bool cardRemoved = cards.Remove(_position);
            
            if (!cardRemoved)
            {
                Debug.LogError($"移除卡牌失败：位置 {_position} 的卡牌无法从数据字典中移除");
                return false;
            }
            
            Debug.Log($"成功从数据字典中移除位置 {_position} 的卡牌");
            
            // 从视图字典中移除卡牌视图
            Dictionary<Vector2Int, CardView> cardViews = CardManager.GetAllCardViews();
            CardView cardView = null;
            
            if (cardViews.TryGetValue(_position, out cardView))
            {
                // 在销毁视图前从字典中移除 - 避免后续代码访问已删除的视图
                bool viewRemoved = cardViews.Remove(_position);
                
                if (!viewRemoved)
                {
                    Debug.LogError($"移除卡牌视图失败：位置 {_position} 的卡牌视图无法从视图字典中移除");
                }
                else
                {
                    Debug.Log($"成功从视图字典中移除位置 {_position} 的卡牌视图");
                }
                
                if (cardView != null && cardView.gameObject != null)
                {
                    try
                    {
                        // 禁用卡牌视图的交互，防止在销毁过程中被点击
                        BoxCollider collider = cardView.GetComponent<BoxCollider>();
                        if (collider != null)
                        {
                            collider.enabled = false;
                        }
                        
                        // 延迟销毁游戏对象，给动画播放时间
                        GameObject.Destroy(cardView.gameObject, 1.0f);
                        
                        Debug.Log($"卡牌视图已标记为销毁: {_position}");
                    }
                    catch (MissingReferenceException)
                    {
                        Debug.LogWarning($"尝试标记销毁时，位置 {_position} 的卡牌视图已不存在");
                    }
                }
                else
                {
                    Debug.LogWarning($"位置 {_position} 的卡牌视图已为null或其GameObject已为null");
                }
            }
            else
            {
                Debug.LogWarning($"找不到位置 {_position} 的卡牌视图");
            }
            
            // 确保卡牌对象被正确释放
            if (card != null)
            {
                // 清理卡牌对象引用
                card.Position = new Vector2Int(-1, -1); // 表示已不在棋盘上
            }
            
            Debug.Log($"卡牌 {cardName} 从位置 {_position} 移除成功");
            
            return true;
        }
    }
} 