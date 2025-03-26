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
        }
        
        public override bool CanExecute()
        {
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
                
                // 特殊规则：如果是友方卡牌，则翻面但不受伤
                if (targetCard.OwnerId == attackerCard.OwnerId)
                {
                    Debug.Log("翻开的是我方卡牌，不执行攻击");
                    
                    // 触发攻击事件（仅用于动画）
                    GameEventSystem.Instance.NotifyCardAttacked(_attackerPosition, _targetPosition);
                    
                    // 标记攻击者已行动
                    attackerCard.HasActed = true;
                    
                    return true;
                }
                
                // 执行攻击
                bool success = attackerCard.Attack(targetCard);
                
                // 特殊规则：如果背面卡牌血量降至0或以下，保留1点血量
                if (targetCard.Data.Health <= 0)
                {
                    Debug.Log($"背面卡牌 {targetCard.Data.Name} 血量降至0或以下，保留1点血量");
                    targetCard.Data.Health = 1;
                }

                if(attackerCard.Data.Health <= 0)
                {
                    Debug.Log($"攻击者卡牌 {attackerCard.Data.Name} 生命值为 {attackerCard.Data.Health}，将被移除");
                    
                    // 移除攻击者卡牌
                    bool removeResult = CardManager.RemoveCard(_attackerPosition);
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
                bool success = attackerCard.Attack(targetCard);
                
                if (success)
                {
                    // 标记攻击者已行动
                    attackerCard.HasActed = true;
                    
                    // 触发攻击事件
                    GameEventSystem.Instance.NotifyCardAttacked(_attackerPosition, _targetPosition);
                    
                    // 触发受伤事件
                    GameEventSystem.Instance.NotifyCardDamaged(_targetPosition);
                    
                    // 检查目标卡牌是否死亡
                    if (targetCard.Data.Health <= 0)
                    {
                        Debug.Log($"目标卡牌 {targetCard.Data.Name} 生命值为 {targetCard.Data.Health}，将被移除");
                        
                        // 确保移除前记录一些信息，以便调试
                        
                        // 立即更新目标卡牌视图，显示生命值为0
                        Dictionary<Vector2Int, CardView> cardViews = CardManager.GetAllCardViews();
                        if (cardViews.TryGetValue(_targetPosition, out CardView targetView))
                        {
                            targetView.UpdateVisuals();
                        }
                        
                        // 移除目标卡牌
                        bool removeResult = CardManager.RemoveCard(_targetPosition);
                    }
                    
                    // 检查攻击者是否死亡（反伤机制）
                    if (attackerCard.Data.Health <= 0)
                    {
                        Debug.Log($"攻击者卡牌 {attackerCard.Data.Name} 生命值为 {attackerCard.Data.Health}，将被移除");
                        
                        // 移除攻击者卡牌
                        bool removeResult = CardManager.RemoveCard(_attackerPosition);
                        
                    }
                    
                    Debug.Log($"卡牌 {attackerCard.Data.Name} 攻击 {targetCard.Data.Name} 成功");
                }
                else
                {
                    Debug.LogWarning($"卡牌 {attackerCard.Data.Name} 攻击 {targetCard.Data.Name} 失败");
                }
                
                return success;
            }
        }
    }
} 