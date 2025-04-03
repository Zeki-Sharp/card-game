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
            Debug.Log($"【能力执行器】开始执行能力: {ability.abilityName}, 动作数量: {ability.actionSequence.Count}");
            
            // 存储执行过程中的临时数据
            Dictionary<string, object> executionContext = new Dictionary<string, object>();
            
            // 执行动作序列
            for (int i = 0; i < ability.actionSequence.Count; i++)
            {
                var actionConfig = ability.actionSequence[i];
                Debug.Log($"【能力执行器】执行动作 {i+1}/{ability.actionSequence.Count}: {actionConfig.actionType}");
                
                // 解析目标位置
                Vector2Int actualTargetPos = ResolveTargetPosition(actionConfig.targetSelector, sourceCard.Position, targetPosition);
                Debug.Log($"【能力执行器】动作目标位置: {actualTargetPos}");
                
                // 执行动作
                yield return ExecuteAction(actionConfig, sourceCard, actualTargetPos, executionContext);
                
                // 等待短暂时间让动画播放
                Debug.Log($"【能力执行器】动作 {actionConfig.actionType} 等待动画播放");
                yield return new WaitForSeconds(0.2f);
                
                Debug.Log($"【能力执行器】动作 {actionConfig.actionType} 执行完成");
            }
            
            // 标记卡牌已行动
            sourceCard.HasActed = true;
            
            Debug.Log($"【能力执行器】能力 {ability.abilityName} 执行完成");
        }
        
        /// <summary>
        /// 执行单个动作
        /// </summary>
        private IEnumerator ExecuteAction(AbilityActionConfig actionConfig, Card sourceCard, Vector2Int targetPosition, Dictionary<string, object> context)
        {
            Dictionary<string, object> parameters = actionConfig.GetParameters();
            
            Debug.Log($"【能力执行器】开始执行动作: {actionConfig.actionType}, 目标位置: {targetPosition}");
            
            switch (actionConfig.actionType)
            {
                case AbilityActionConfig.ActionType.Attack:
                    // 普通攻击
                    Debug.Log($"【能力执行器】执行攻击动作: 从 {sourceCard.Position} 到 {targetPosition}");
                    
                    // 确保目标位置有卡牌
                    Card targetCard = _cardManager.GetCard(targetPosition);
                    if (targetCard != null)
                    {
                        // 执行攻击
                        int damageDealt = _cardManager.ExecuteAttack(sourceCard.Position, targetPosition);
                        context["dealtDamage"] = damageDealt;
                        Debug.Log($"【能力执行器】攻击完成，造成伤害: {damageDealt}");
                    }
                    else
                    {
                        Debug.LogError($"【能力执行器】攻击失败: 目标位置 {targetPosition} 没有卡牌");
                    }
                    break;
                    
                case AbilityActionConfig.ActionType.Move:
                    Debug.Log($"【能力执行器】执行移动动作: 从 {sourceCard.Position} 到 {targetPosition}");
                    ExecuteMoveAction(sourceCard.Position, targetPosition);
                    Debug.Log($"【能力执行器】移动完成，新位置: {sourceCard.Position}");
                    break;
                    
                case AbilityActionConfig.ActionType.Heal:
                    int healAmount = ResolveHealAmount(parameters, context);
                    ExecuteHealAction(targetPosition, healAmount);
                    break;
                    
                case AbilityActionConfig.ActionType.Wait:
                    float waitTime = ResolveWaitTime(parameters);
                    Debug.Log($"【能力执行器】执行等待动作: {waitTime}秒");
                    yield return new WaitForSeconds(waitTime);
                    Debug.Log($"【能力执行器】等待完成");
                    break;
                    
                case AbilityActionConfig.ActionType.ApplyEffect:
                    // 应用效果的逻辑
                    break;
            }
            
            // 确保每个动作执行后有足够的时间让游戏状态更新
            Debug.Log($"【能力执行器】动作 {actionConfig.actionType} 额外等待时间");
            yield return new WaitForSeconds(0.1f);
            Debug.Log($"【能力执行器】动作 {actionConfig.actionType} 完全完成");
        }
        
        /// <summary>
        /// 解析目标位置
        /// </summary>
        private Vector2Int ResolveTargetPosition(string targetSelector, Vector2Int sourcePosition, Vector2Int targetPosition)
        {
            // 解析目标选择器
            if (string.IsNullOrEmpty(targetSelector) || targetSelector == "Self")
                return sourcePosition;
                
            if (targetSelector == "Target")
                return targetPosition;
                
            if (targetSelector.StartsWith("TargetPosition"))
            {
                try
                {
                    // 解析相对位置，如 "TargetPosition-1,0"
                    string offsetStr = targetSelector.Substring("TargetPosition".Length);
                    string[] parts = offsetStr.Split(',');
                    int offsetX = int.Parse(parts[0]);
                    int offsetY = int.Parse(parts[1]);
                    return new Vector2Int(targetPosition.x + offsetX, targetPosition.y + offsetY);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"解析目标位置失败: {targetSelector}, 错误: {e.Message}");
                }
            }

            // 处理特殊的目标选择器
            if (targetSelector.StartsWith("TargetDirection"))
            {
                // 计算方向向量
                Vector2Int direction = targetPosition - sourcePosition;
                
                // 标准化方向（保持方向，但长度为1）
                if (direction.x != 0) direction.x = direction.x / Mathf.Abs(direction.x);
                if (direction.y != 0) direction.y = direction.y / Mathf.Abs(direction.y);
                
                // 检查是否有距离修饰符
                if (targetSelector.Contains("-"))
                {
                    string[] parts = targetSelector.Split('-');
                    if (parts.Length == 2 && int.TryParse(parts[1], out int distanceModifier))
                    {
                        // 计算目标位置：源位置 + 方向 * (距离 - 修饰符)
                        int distance = Mathf.Max(Mathf.Abs(targetPosition.x - sourcePosition.x), 
                                                Mathf.Abs(targetPosition.y - sourcePosition.y));
                        int adjustedDistance = distance - distanceModifier;
                        
                        // 确保距离至少为1
                        adjustedDistance = Mathf.Max(1, adjustedDistance);
                        
                        return sourcePosition + direction * adjustedDistance;
                    }
                }
                
                // 如果没有修饰符，直接返回目标位置
                return targetPosition;
            }
            
            return targetPosition; // 默认返回原始目标位置
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