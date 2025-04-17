// 创建 ModifyStatAction.cs 文件
using UnityEngine;
using System.Collections;

namespace ChessGame
{
    /// <summary>
    /// 卡牌属性修改行动 - 修改卡牌的攻击力或生命值
    /// </summary>
    public class ModifyStatAction : CardAction
    {
        private Vector2Int _targetPosition;
        private string _statType;
        private int _amount;
        
        public ModifyStatAction(CardManager cardManager, Vector2Int targetPosition, string statType, int amount) 
            : base(cardManager)
        {
            _targetPosition = targetPosition;
            _statType = statType;
            _amount = amount;
        }
        
        public override bool CanExecute()
        {
            // 获取目标卡牌
            Card targetCard = CardManager.GetCard(_targetPosition);
            if (targetCard == null)
            {
                Debug.LogWarning($"属性修改失败：目标位置 {_targetPosition} 没有卡牌");
                return false;
            }
            
            return true;
        }
        
        public override bool Execute()
        {
            if (!CanExecute())
                return false;
                
            // 获取目标卡牌
            Card targetCard = CardManager.GetCard(_targetPosition);
            
            Debug.Log($"修改卡牌属性: {targetCard.Data.Name}, 属性类型: {_statType}, 数值: {_amount}");
            
            // 根据属性类型修改卡牌属性
            switch (_statType.ToLower())
            {
                case "attack":
                    int attackBefore = targetCard.Data.Attack;
                    targetCard.Data.Attack += _amount;
                    Debug.Log($"卡牌 {targetCard.Data.Name} 的攻击力从 {attackBefore} 增加到 {targetCard.Data.Attack}");
                    break;
                case "health":
                    int healthBefore = targetCard.Data.Health;
                    targetCard.Data.Health += _amount;
                    Debug.Log($"卡牌 {targetCard.Data.Name} 的生命值从 {healthBefore} 增加到 {targetCard.Data.Health}");
                    break;
                case "both":
                    int attackBefore2 = targetCard.Data.Attack;
                    int healthBefore2 = targetCard.Data.Health;
                    targetCard.Data.Attack += _amount;
                    targetCard.Data.Health += _amount;
                    Debug.Log($"卡牌 {targetCard.Data.Name} 的攻击力从 {attackBefore2} 增加到 {targetCard.Data.Attack}，生命值从 {healthBefore2} 增加到 {targetCard.Data.Health}");
                    break;
                default:
                    Debug.LogWarning($"未知的属性类型: {_statType}");
                    return false;
            }
            
            // 更新卡牌视图
            CardView cardView = CardManager.GetCardView(_targetPosition);
            if (cardView != null)
            {
                cardView.UpdateVisuals();
            }
            
            // 播放成长动画
            if (_amount > 0 && CardAnimationService.Instance != null)
            {
                CardAnimationService.Instance.PlayGrowAnimation(_targetPosition);
            }
            
            return true;
        }
    }
}