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
            
            // 确保CardManager不为空
            if (CardManager == null)
            {
                Debug.LogError("MoveCardAction: CardManager 为空");
                // 尝试再次从场景中获取
                CardManager = GameObject.FindObjectOfType<CardManager>();
                if (CardManager == null)
                {
                    Debug.LogError("MoveCardAction: 无法从场景中找到 CardManager");
                }
            }
        }
        
        public override bool CanExecute()
        {
            // 检查CardManager是否为空
            if (CardManager == null)
            {
                Debug.LogError("MoveCardAction.CanExecute: CardManager 为空");
                // 最后一次尝试获取
                CardManager = GameObject.FindObjectOfType<CardManager>();
                if (CardManager == null)
                {
                    return false;
                }
            }
            
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
            // 检查是否可以执行
            if (!CanExecute())
            {
                return false;
            }
            
            // 检查CardManager是否为空
            if (CardManager == null)
            {
                Debug.LogError("MoveCardAction.Execute: CardManager 为空");
                return false;
            }
            
            // 获取卡牌和视图
            Card card = CardManager.GetCard(_fromPosition);
            if (card == null)
            {
                Debug.LogError($"移动失败：起始位置 {_fromPosition} 没有卡牌");
                return false;
            }
            
            CardView cardView = CardManager.GetCardView(_fromPosition);
            
            Debug.Log($"开始移动卡牌: {card.Data.Name}, 从 {_fromPosition} 到 {_toPosition}");
            
            // 获取数据结构
            Dictionary<Vector2Int, Card> cards = CardManager.GetAllCards();
            Dictionary<Vector2Int, CardView> cardViews = CardManager.GetAllCardViews();
            
            // 从原位置移除卡牌数据
            if (!cards.Remove(_fromPosition))
            {
                Debug.LogError($"移动失败：无法从位置 {_fromPosition} 移除卡牌数据");
                return false;
            }
            
            // 更新卡牌位置属性
            card.Position = _toPosition;
            
            // 添加到新位置
            cards[_toPosition] = card;
            
            // 更新视图字典
            if (cardView != null)
            {
                if (!cardViews.Remove(_fromPosition))
                {
                    Debug.LogWarning($"移动警告：无法从位置 {_fromPosition} 移除卡牌视图");
                }
                
                cardViews[_toPosition] = cardView;
            }
            
            // 标记卡牌已行动
            card.HasActed = true;
            
            // 触发移动事件 - 动画服务会监听此事件并播放动画
            GameEventSystem.Instance.NotifyCardMoved(_fromPosition, _toPosition);
            
            Debug.Log($"卡牌 {card.Data.Name} 移动完成：{_fromPosition} -> {_toPosition}");
            
            return true;
        }
    }
} 