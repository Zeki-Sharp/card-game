// 创建 HealCardAction.cs 文件
using UnityEngine;

namespace ChessGame
{
    /// <summary>
    /// 卡牌治疗行动 - 为卡牌恢复生命值
    /// </summary>
    public class HealCardAction : CardAction
    {
        private Vector2Int _targetPosition;
        private int _healAmount;
        
        public HealCardAction(CardManager cardManager, Vector2Int targetPosition, int healAmount) 
            : base(cardManager)
        {
            _targetPosition = targetPosition;
            _healAmount = healAmount;
        }
        
        public override bool CanExecute()
        {
            // 获取目标卡牌
            Card targetCard = CardManager.GetCard(_targetPosition);
            if (targetCard == null)
            {
                Debug.LogWarning($"治疗失败：目标位置 {_targetPosition} 没有卡牌");
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
            
            // 记录治疗前的生命值
            int healthBefore = targetCard.Data.Health;
            
            // 增加目标的生命值
            targetCard.Data.Health += _healAmount;
            
            // 如果目标卡牌的生命值超过最大生命值，则设置为最大生命值
            if (targetCard.Data.Health > targetCard.Data.MaxHealth)
            {
                targetCard.Data.Health = targetCard.Data.MaxHealth;
            }
            
            // 更新卡牌视图
            CardView cardView = CardManager.GetCardView(_targetPosition);
            if (cardView != null)
            {
                cardView.UpdateVisuals();
            }
            
            Debug.Log($"治疗效果: {targetCard.Data.Name} 从 {healthBefore} 恢复 {_healAmount} 点生命值，现在为 {targetCard.Data.Health}");
            
            return true;
        }
    }
}