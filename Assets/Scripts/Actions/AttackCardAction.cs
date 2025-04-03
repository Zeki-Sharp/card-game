using UnityEngine;
using System.Collections.Generic;

namespace ChessGame
{
    /// <summary>
    /// 卡牌攻击行动 - 包含完整的攻击逻辑
    /// </summary>
    public class AttackCardAction : CardAction
    {
        private Vector2Int _attackerPosition;
        private Vector2Int _targetPosition;
        
        public AttackCardAction(CardManager cardManager, Vector2Int attackerPosition, Vector2Int targetPosition) 
            : base(cardManager)
        {
            _attackerPosition = attackerPosition;
            _targetPosition = targetPosition;
            
            // 确保CardManager不为空
            if (CardManager == null)
            {
                Debug.LogError("AttackCardAction: CardManager 为空");
                // 尝试再次从场景中获取
                CardManager = GameObject.FindObjectOfType<CardManager>();
                if (CardManager == null)
                {
                    Debug.LogError("AttackCardAction: 无法从场景中找到 CardManager");
                }
            }
        }
        
        public override bool CanExecute()
        {
            // 检查CardManager是否为空
            if (CardManager == null)
            {
                Debug.LogError("AttackCardAction.CanExecute: CardManager 为空");
                // 最后一次尝试获取
                CardManager = GameObject.FindObjectOfType<CardManager>();
                if (CardManager == null)
                {
                    return false;
                }
            }
            
            // 获取攻击者卡牌
            Card attackerCard = CardManager.GetCard(_attackerPosition);
            if (attackerCard == null)
            {
                Debug.LogWarning($"攻击失败：攻击者位置 {_attackerPosition} 没有卡牌");
                return false;
            }
            
            // 获取目标卡牌
            Card targetCard = CardManager.GetCard(_targetPosition);
            if (targetCard == null)
            {
                Debug.LogWarning($"攻击失败：目标位置 {_targetPosition} 没有卡牌");
                return false;
            }
            
            // 检查是否可以攻击
            if (!attackerCard.CanAttack(_targetPosition, CardManager.GetAllCards()))
            {
                Debug.LogWarning($"攻击失败：目标位置 {_targetPosition} 不在合法攻击范围内");
                return false;
            }
            
            return true;
        }
        
        public override bool Execute()
        {    
            if (!CanExecute())
                return false;
                
            // 获取攻击者和目标卡牌
            Card attackerCard = CardManager.GetCard(_attackerPosition);
            Card targetCard = CardManager.GetCard(_targetPosition);
            
            // 记录攻击前的生命值
            int attackerHpBefore = attackerCard.Data.Health;
            int targetHpBefore = targetCard.Data.Health;
            
            // 处理背面卡牌的特殊情况
            if (targetCard.IsFaceDown)
            {
                Debug.Log("目标是背面卡牌，先翻面");
                
                // 翻面
                targetCard.FlipToFaceUp();
                
                // 触发翻面事件
                GameEventSystem.Instance.NotifyCardFlipped(_targetPosition, false);
                
                // 执行攻击
                attackerCard.Attack(targetCard);
                
                // 特殊规则：如果背面卡牌血量降至0或以下，保留1点血量
                if (targetCard.Data.Health <= 0)
                {
                    Debug.Log($"背面卡牌 {targetCard.Data.Name} 血量降至0或以下，保留1点血量");
                    targetCard.Data.Health = 1;
                }
                
                // 标记攻击者已行动
                attackerCard.HasActed = true;
                
                // 触发攻击事件
                GameEventSystem.Instance.NotifyCardAttacked(_attackerPosition, _targetPosition);
                
                // 触发受伤事件
                GameEventSystem.Instance.NotifyCardDamaged(_targetPosition);
                
                return true;
            }
            else
            {
                // 正面卡牌正常攻击
                Debug.Log("目标是正面卡牌，直接执行攻击");
                
                // 记录攻击前的生命值，用于调试
                int attackerHealthBefore = attackerCard.Data.Health;
                int targetHealthBefore = targetCard.Data.Health;
                
                // 执行攻击
                attackerCard.Attack(targetCard);
                
                // 判断是否应该受到反伤
                if (targetCard.ShouldReceiveCounterAttack(attackerCard, CardManager.GetAllCards()))
                {
                    // 执行反击
                    targetCard.AntiAttack(attackerCard);
                    Debug.Log($"卡牌 {targetCard.Data.Name} 对 {attackerCard.Data.Name} 进行反击");
                }
                else
                {
                    Debug.Log($"卡牌 {attackerCard.Data.Name} 不在 {targetCard.Data.Name} 的攻击范围内，不受反伤");
                }
                
                // 记录攻击后的生命值，用于调试
                int attackerHealthAfter = attackerCard.Data.Health;
                int targetHealthAfter = targetCard.Data.Health;
                
                Debug.Log($"攻击前生命值 - 攻击者: {attackerHealthBefore}, 目标: {targetHealthBefore}");
                Debug.Log($"攻击后生命值 - 攻击者: {attackerHealthAfter}, 目标: {targetHealthAfter}");
                
                // 标记攻击者已行动
                attackerCard.HasActed = true;
                
                // 更新被攻击者的视图，以及触发受伤事件
                CardView targetView = CardManager.GetCardView(_targetPosition);
                if (targetView != null) targetView.UpdateVisuals();
                GameEventSystem.Instance.NotifyCardAttacked(_attackerPosition, _targetPosition);
                GameEventSystem.Instance.NotifyCardDamaged(_targetPosition);

                // 更新攻击者的视图，以及触发受伤事件
                CardView attackerView = CardManager.GetCardView(_attackerPosition);
                if (attackerView != null) attackerView.UpdateVisuals();
                GameEventSystem.Instance.NotifyCardDamaged(_attackerPosition); // 别忘了攻击者也可能受伤
                
                // 检查目标卡牌是否死亡
                if (targetCard.Data.Health <= 0)
                {
                    Debug.Log($"目标卡牌 {targetCard.Data.Name} 生命值为 {targetCard.Data.Health}，将被移除");
                    
                    // 立即更新目标卡牌视图，显示生命值为0
                    if (targetView != null) targetView.UpdateVisuals();
                    
                    // 移除目标卡牌
                    CardManager.RemoveCard(_targetPosition);
                }
                
                // 检查攻击者是否死亡（反伤机制）
                if (attackerCard.Data.Health <= 0)
                {
                    Debug.Log($"攻击者卡牌 {attackerCard.Data.Name} 生命值为 {attackerCard.Data.Health}，将被移除");
                    
                    // 移除攻击者卡牌
                    CardManager.RemoveCard(_attackerPosition);
                }
                    
                Debug.Log($"卡牌 {attackerCard.Data.Name} 攻击 {targetCard.Data.Name} 成功");
                return true;
            }
        }
    }
} 