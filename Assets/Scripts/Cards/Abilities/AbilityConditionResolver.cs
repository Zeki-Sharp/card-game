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
            // 添加调试信息
            Debug.Log($"解析条件: {condition}, 卡牌: {card.Data.Name}, 目标位置: {targetPosition}");
            
            // 替换变量
            string resolvedCondition = ReplaceVariables(condition, card, targetPosition, cardManager);
            Debug.Log($"替换变量后的条件: {resolvedCondition}");
            
            // 解析条件表达式
            bool result = EvaluateExpression(resolvedCondition);
            Debug.Log($"条件解析结果: {result}");
            
            return result;
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

        private string ReplaceVariables(string condition, Card card, Vector2Int targetPosition, CardManager cardManager)
        {
            // 替换MoveRange
            condition = condition.Replace("MoveRange", card.MoveRange.ToString());
            
            // 替换AttackRange
            condition = condition.Replace("AttackRange", card.AttackRange.ToString());
            
            // 替换Distance
            int distance = Mathf.Abs(targetPosition.x - card.Position.x) + Mathf.Abs(targetPosition.y - card.Position.y);
            condition = condition.Replace("Distance", distance.ToString());
            
            // 替换Empty
            bool isEmpty = !cardManager.HasCardAt(targetPosition);
            condition = condition.Replace("Empty", isEmpty.ToString().ToLower());
            
            // 替换Enemy
            bool isEnemy = false;
            Card targetCard = cardManager.GetCardAt(targetPosition);
            if (targetCard != null)
            {
                isEnemy = targetCard.OwnerId != card.OwnerId;
            }
            condition = condition.Replace("Enemy", isEnemy.ToString().ToLower());
            
            // 替换FaceDown
            bool isFaceDown = false;
            if (targetCard != null)
            {
                isFaceDown = targetCard.IsFaceDown;
            }
            condition = condition.Replace("FaceDown", isFaceDown.ToString().ToLower());
            
            return condition;
        }

        private bool EvaluateExpression(string expression)
        {
            // 处理括号
            if (expression.Contains("(") && expression.Contains(")"))
            {
                int startIndex = expression.IndexOf('(');
                int endIndex = expression.LastIndexOf(')');
                if (startIndex < endIndex)
                {
                    string innerExpression = expression.Substring(startIndex + 1, endIndex - startIndex - 1);
                    bool innerResult = EvaluateExpression(innerExpression);
                    string newExpression = expression.Substring(0, startIndex) + innerResult.ToString().ToLower() + expression.Substring(endIndex + 1);
                    return EvaluateExpression(newExpression);
                }
            }
            
            // 处理逻辑运算符
            if (expression.Contains("&&"))
            {
                string[] parts = expression.Split(new string[] { "&&" }, System.StringSplitOptions.None);
                bool result = true;
                foreach (string part in parts)
                {
                    result = result && EvaluateExpression(part.Trim());
                    if (!result) return false; // 短路求值
                }
                return result;
            }
            
            if (expression.Contains("||"))
            {
                string[] parts = expression.Split(new string[] { "||" }, System.StringSplitOptions.None);
                bool result = false;
                foreach (string part in parts)
                {
                    result = result || EvaluateExpression(part.Trim());
                    if (result) return true; // 短路求值
                }
                return result;
            }
            
            // 处理比较运算符
            if (expression.Contains("<="))
            {
                string[] parts = expression.Split(new string[] { "<=" }, System.StringSplitOptions.None);
                float left = float.Parse(parts[0].Trim());
                float right = float.Parse(parts[1].Trim());
                return left <= right;
            }
            
            if (expression.Contains(">="))
            {
                string[] parts = expression.Split(new string[] { ">=" }, System.StringSplitOptions.None);
                float left = float.Parse(parts[0].Trim());
                float right = float.Parse(parts[1].Trim());
                return left >= right;
            }
            
            if (expression.Contains("<"))
            {
                string[] parts = expression.Split(new string[] { "<" }, System.StringSplitOptions.None);
                float left = float.Parse(parts[0].Trim());
                float right = float.Parse(parts[1].Trim());
                return left < right;
            }
            
            if (expression.Contains(">"))
            {
                string[] parts = expression.Split(new string[] { ">" }, System.StringSplitOptions.None);
                float left = float.Parse(parts[0].Trim());
                float right = float.Parse(parts[1].Trim());
                return left > right;
            }
            
            if (expression.Contains("=="))
            {
                string[] parts = expression.Split(new string[] { "==" }, System.StringSplitOptions.None);
                string left = parts[0].Trim();
                string right = parts[1].Trim();
                
                // 尝试解析为数字
                float leftNum, rightNum;
                if (float.TryParse(left, out leftNum) && float.TryParse(right, out rightNum))
                {
                    return leftNum == rightNum;
                }
                
                // 否则作为字符串比较
                return left == right;
            }
            
            if (expression.Contains("!="))
            {
                string[] parts = expression.Split(new string[] { "!=" }, System.StringSplitOptions.None);
                string left = parts[0].Trim();
                string right = parts[1].Trim();
                
                // 尝试解析为数字
                float leftNum, rightNum;
                if (float.TryParse(left, out leftNum) && float.TryParse(right, out rightNum))
                {
                    return leftNum != rightNum;
                }
                
                // 否则作为字符串比较
                return left != right;
            }
            
            // 处理布尔值
            if (expression.Trim() == "true") return true;
            if (expression.Trim() == "false") return false;
            
            // 无法解析的表达式
            Debug.LogError($"无法解析的表达式: {expression}");
            return false;
        }
    }
} 