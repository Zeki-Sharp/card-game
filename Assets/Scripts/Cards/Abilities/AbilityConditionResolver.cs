using UnityEngine;
using System.Collections.Generic;

namespace ChessGame.Cards
{
    /// <summary>
    /// 能力条件解析器 - 负责解析和检查能力的触发条件
    /// </summary>
    public class AbilityConditionResolver
    {
        // 存储卡牌能力的冷却时间
        private Dictionary<int, Dictionary<string, int>> _abilityCooldowns = new Dictionary<int, Dictionary<string, int>>();
        
        /// <summary>
        /// 检查能力是否可以触发
        /// </summary>
        /// <param name="condition">条件表达式</param>
        /// <param name="card">源卡牌</param>
        /// <param name="targetPosition">目标位置</param>
        /// <param name="cardManager">卡牌管理器</param>
        /// <returns>是否满足条件</returns>
        public bool CheckCondition(string condition, Card card, Vector2Int targetPosition, CardManager cardManager)
        {
            // 如果没有条件，默认可以触发
            if (string.IsNullOrEmpty(condition) || condition == "Always")
                return true;
                
            // 解析条件表达式
            if (condition.StartsWith("Distance=="))
            {
                int requiredDistance = int.Parse(condition.Substring("Distance==".Length));
                int actualDistance = Mathf.Abs(targetPosition.x - card.Position.x) + 
                                    Mathf.Abs(targetPosition.y - card.Position.y);
                return actualDistance == requiredDistance;
            }
            
            if (condition.StartsWith("Distance>="))
            {
                int requiredDistance = int.Parse(condition.Substring("Distance>=".Length));
                int actualDistance = Mathf.Abs(targetPosition.x - card.Position.x) + 
                                    Mathf.Abs(targetPosition.y - card.Position.y);
                return actualDistance >= requiredDistance;
            }
            
            if (condition.StartsWith("Distance<="))
            {
                int requiredDistance = int.Parse(condition.Substring("Distance<=".Length));
                int actualDistance = Mathf.Abs(targetPosition.x - card.Position.x) + 
                                    Mathf.Abs(targetPosition.y - card.Position.y);
                return actualDistance <= requiredDistance;
            }
            
            if (condition.StartsWith("Cooldown=="))
            {
                int cardId = card.Data.Id;
                string abilityName = condition.Split(':')[1]; // 格式: "Cooldown==0:AbilityName"
                int requiredCooldown = int.Parse(condition.Substring("Cooldown==".Length).Split(':')[0]);
                int currentCooldown = GetAbilityCooldown(cardId, abilityName);
                return currentCooldown == requiredCooldown;
            }
            
            if (condition.StartsWith("Health<="))
            {
                int percentage = int.Parse(condition.Substring("Health<=".Length));
                float healthPercentage = (float)card.Data.Health / card.Data.MaxHealth * 100f;
                return healthPercentage <= percentage;
            }
            
            if (condition.StartsWith("IsEnemy"))
            {
                Card targetCard = cardManager.GetCard(targetPosition);
                return targetCard != null && targetCard.OwnerId != card.OwnerId;
            }
            
            if (condition.StartsWith("IsFaceDown"))
            {
                Card targetCard = cardManager.GetCard(targetPosition);
                return targetCard != null && targetCard.IsFaceDown;
            }
            
            // 组合条件（使用AND和OR）
            if (condition.Contains("&&"))
            {
                string[] subConditions = condition.Split(new string[] { "&&" }, System.StringSplitOptions.None);
                foreach (var subCondition in subConditions)
                {
                    if (!CheckCondition(subCondition.Trim(), card, targetPosition, cardManager))
                        return false;
                }
                return true;
            }
            
            if (condition.Contains("||"))
            {
                string[] subConditions = condition.Split(new string[] { "||" }, System.StringSplitOptions.None);
                foreach (var subCondition in subConditions)
                {
                    if (CheckCondition(subCondition.Trim(), card, targetPosition, cardManager))
                        return true;
                }
                return false;
            }
            
            Debug.LogWarning($"未知条件: {condition}");
            return false;
        }
        
        /// <summary>
        /// 设置能力冷却
        /// </summary>
        public void SetAbilityCooldown(int cardId, string abilityName, int cooldown)
        {
            if (!_abilityCooldowns.ContainsKey(cardId))
            {
                _abilityCooldowns[cardId] = new Dictionary<string, int>();
            }
            _abilityCooldowns[cardId][abilityName] = cooldown;
        }
        
        /// <summary>
        /// 获取能力冷却
        /// </summary>
        public int GetAbilityCooldown(int cardId, string abilityName)
        {
            if (_abilityCooldowns.TryGetValue(cardId, out var cooldowns) && 
                cooldowns.TryGetValue(abilityName, out int cooldown))
            {
                return cooldown;
            }
            return 0;
        }
        
        /// <summary>
        /// 减少所有能力的冷却时间
        /// </summary>
        public void ReduceCooldowns(int playerId)
        {
            foreach (var cardCooldowns in _abilityCooldowns)
            {
                foreach (var ability in new List<string>(cardCooldowns.Value.Keys))
                {
                    if (cardCooldowns.Value[ability] > 0)
                    {
                        cardCooldowns.Value[ability]--;
                    }
                }
            }
        }
    }
} 