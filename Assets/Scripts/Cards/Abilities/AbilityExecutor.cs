using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChessGame.Cards
{
    /// <summary>
    /// 能力执行器 - 负责解析能力配置并执行动作序列
    /// </summary>
    public class AbilityExecutor
    {
        private CardManager _cardManager;
        
        public AbilityExecutor(CardManager cardManager)
        {
            _cardManager = cardManager;
        }
        
        /// <summary>
        /// 执行能力
        /// </summary>
        /// <param name="ability">能力配置</param>
        /// <param name="sourceCard">源卡牌</param>
        /// <param name="targetPosition">目标位置</param>
        /// <returns>协程</returns>
        public IEnumerator ExecuteAbility(AbilityConfiguration ability, Card sourceCard, Vector2Int targetPosition)
        {
            Debug.Log($"执行能力: {ability.abilityName}");
            
            // 存储执行过程中的临时数据
            Dictionary<string, object> executionContext = new Dictionary<string, object>();
            
            // 执行动作序列
            foreach (var actionConfig in ability.actionSequence)
            {
                // 解析目标位置
                Vector2Int actualTargetPos = ResolveTargetPosition(actionConfig.targetSelector, sourceCard.Position, targetPosition);
                
                // 执行动作
                yield return ExecuteAction(actionConfig, sourceCard, actualTargetPos, executionContext);
                
                // 等待短暂时间让动画播放
                yield return new WaitForSeconds(0.2f);
            }
            
            // 标记卡牌已行动
            sourceCard.HasActed = true;
            
            Debug.Log($"能力 {ability.abilityName} 执行完成");
        }
        
        /// <summary>
        /// 执行单个动作
        /// </summary>
        private IEnumerator ExecuteAction(AbilityActionConfig actionConfig, Card sourceCard, Vector2Int targetPosition, Dictionary<string, object> context)
        {
            Dictionary<string, object> parameters = actionConfig.GetParameters();
            
            switch (actionConfig.actionType)
            {
                case AbilityActionConfig.ActionType.Move:
                    ExecuteMoveAction(sourceCard.Position, targetPosition);
                    break;
                    
                case AbilityActionConfig.ActionType.Attack:
                    int damageDealt = ExecuteAttackAction(sourceCard.Position, targetPosition);
                    // 存储造成的伤害，供后续动作使用
                    context["dealtDamage"] = damageDealt;
                    break;
                    
                case AbilityActionConfig.ActionType.Heal:
                    int healAmount = ResolveHealAmount(parameters, context);
                    ExecuteHealAction(targetPosition, healAmount);
                    break;
                    
                case AbilityActionConfig.ActionType.Wait:
                    float waitTime = ResolveWaitTime(parameters);
                    yield return new WaitForSeconds(waitTime);
                    break;
                    
                case AbilityActionConfig.ActionType.ApplyEffect:
                    // 应用效果的逻辑
                    break;
            }
            
            yield return null;
        }
        
        /// <summary>
        /// 解析目标位置
        /// </summary>
        private Vector2Int ResolveTargetPosition(string targetSelector, Vector2Int sourcePos, Vector2Int clickedPos)
        {
            // 解析目标选择器
            if (string.IsNullOrEmpty(targetSelector) || targetSelector == "Self")
                return sourcePos;
                
            if (targetSelector == "Target")
                return clickedPos;
                
            if (targetSelector.StartsWith("TargetPosition"))
            {
                try
                {
                    // 解析相对位置，如 "TargetPosition-1,0"
                    string offsetStr = targetSelector.Substring("TargetPosition".Length);
                    string[] parts = offsetStr.Split(',');
                    int offsetX = int.Parse(parts[0]);
                    int offsetY = int.Parse(parts[1]);
                    return new Vector2Int(clickedPos.x + offsetX, clickedPos.y + offsetY);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"解析目标位置失败: {targetSelector}, 错误: {e.Message}");
                }
            }
            
            return clickedPos; // 默认
        }
        
        /// <summary>
        /// 执行移动动作
        /// </summary>
        private void ExecuteMoveAction(Vector2Int fromPos, Vector2Int toPos)
        {
            MoveCardAction moveAction = new MoveCardAction(_cardManager, fromPos, toPos);
            moveAction.Execute();
        }
        
        /// <summary>
        /// 执行攻击动作
        /// </summary>
        private int ExecuteAttackAction(Vector2Int fromPos, Vector2Int toPos)
        {
            // 记录目标卡牌攻击前的生命值
            Card targetCard = _cardManager.GetCard(toPos);
            int healthBefore = targetCard != null ? targetCard.Data.Health : 0;
            
            // 执行攻击
            AttackCardAction attackAction = new AttackCardAction(_cardManager, fromPos, toPos);
            attackAction.Execute();
            
            // 计算造成的伤害
            int damageDealt = 0;
            if (targetCard != null)
            {
                if (targetCard.Data.Health > 0)
                {
                    damageDealt = healthBefore - targetCard.Data.Health;
                }
                else
                {
                    damageDealt = healthBefore; // 目标被击杀
                }
            }
            
            return damageDealt;
        }
        
        /// <summary>
        /// 执行治疗动作
        /// </summary>
        private void ExecuteHealAction(Vector2Int targetPos, int amount)
        {
            Card targetCard = _cardManager.GetCard(targetPos);
            if (targetCard == null) return;
            
            // 增加目标的生命值
            targetCard.Data.Health += amount;
            
            // 更新卡牌视图
            CardView cardView = _cardManager.GetCardView(targetPos);
            if (cardView != null)
            {
                cardView.UpdateVisuals();
            }
            
            Debug.Log($"治疗效果: {targetCard.Data.Name} 恢复 {amount} 点生命值");
        }
        
        /// <summary>
        /// 解析治疗量
        /// </summary>
        private int ResolveHealAmount(Dictionary<string, object> parameters, Dictionary<string, object> context)
        {
            if (parameters.TryGetValue("amount", out object amountObj))
            {
                string amountStr = amountObj.ToString();
                
                // 检查是否是百分比表达式
                if (amountStr.Contains("%") && amountStr.Contains("of"))
                {
                    // 解析百分比表达式，如 "50% of dealtDamage"
                    string[] parts = amountStr.Split(new string[] { "% of " }, System.StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        float percentage = float.Parse(parts[0]) / 100f;
                        string contextKey = parts[1].Trim();
                        
                        if (context.TryGetValue(contextKey, out object contextValue) && contextValue is int)
                        {
                            return Mathf.RoundToInt(percentage * (int)contextValue);
                        }
                    }
                }
                
                // 尝试直接解析为整数
                if (int.TryParse(amountStr, out int amount))
                {
                    return amount;
                }
            }
            
            return 0; // 默认
        }
        
        /// <summary>
        /// 解析等待时间
        /// </summary>
        private float ResolveWaitTime(Dictionary<string, object> parameters)
        {
            if (parameters.TryGetValue("time", out object timeObj))
            {
                if (float.TryParse(timeObj.ToString(), out float time))
                {
                    return time;
                }
            }
            
            return 0.2f; // 默认
        }
    }
} 