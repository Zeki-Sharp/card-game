using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
            Debug.Log($"检查条件: {condition}, 卡牌: {card.Data.Name}, 位置: {card.Position}, 目标位置: {targetPosition}");
            
            // 替换变量
            string resolvedCondition = ReplaceVariables(condition, card, targetPosition, cardManager);
            Debug.Log($"替换变量后的条件: {resolvedCondition}");
            
            // 检查目标位置的卡牌
            Card targetCard = cardManager.GetCard(targetPosition);
            if (targetCard != null)
            {
                Debug.Log($"目标位置有卡牌: {targetCard.Data.Name}, 所有者: {targetCard.OwnerId}, 背面: {targetCard.IsFaceDown}");
            }
            else
            {
                Debug.Log($"目标位置没有卡牌");
            }
            
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
            try
            {
                // 使用正则表达式替换完整的变量名
                // 替换StraightDistance - 检查是否在同一直线上（横向或纵向）
                int dx1 = Mathf.Abs(targetPosition.x - card.Position.x); // 水平方向距离
                int dy1 = Mathf.Abs(targetPosition.y - card.Position.y); // 垂直方向距离
                bool isStraight = dy1 == 0 || dx1 == 0;
                int straightDistance = isStraight ? Mathf.Max(dx1, dy1) : int.MaxValue;
                condition = Regex.Replace(condition, @"\bStraightDistance\b", straightDistance.ToString());
                
                // 替换Distance - 曼哈顿距离（横向+纵向）
                int manhattanDistance = dx1 + dy1;
                condition = Regex.Replace(condition, @"\bDistance\b", manhattanDistance.ToString());
                
                // 替换DiagonalDistance - 对角线距离
                bool isDiagonal = dx1 == dy1 && dx1 > 0;
                int diagonalDistance = isDiagonal ? dx1 : int.MaxValue;
                condition = Regex.Replace(condition, @"\bDiagonalDistance\b", diagonalDistance.ToString());
                
                // 替换MoveRange和AttackRange
                condition = Regex.Replace(condition, @"\bMoveRange\b", card.MoveRange.ToString());
                condition = Regex.Replace(condition, @"\bAttackRange\b", card.AttackRange.ToString());
                
                // 替换Enemy
                Card targetCard = cardManager.GetCard(targetPosition);
                bool isEnemy = targetCard != null && targetCard.OwnerId != card.OwnerId && !targetCard.IsFaceDown;
                condition = Regex.Replace(condition, @"\bEnemy\b", isEnemy.ToString().ToLower());
                
                // 替换FaceDown
                bool isFaceDown = targetCard != null && targetCard.IsFaceDown;
                condition = Regex.Replace(condition, @"\bFaceDown\b", isFaceDown.ToString().ToLower());

                // 替换EnemyOrFaceDown
                bool isEnemyOrFaceDown = targetCard != null && (targetCard.OwnerId != card.OwnerId || targetCard.IsFaceDown);
                condition = Regex.Replace(condition, @"\bEnemyOrFaceDown\b", isEnemyOrFaceDown.ToString().ToLower());
                
                // 替换Empty
                bool isEmpty = targetCard == null;
                condition = Regex.Replace(condition, @"\bEmpty\b", isEmpty.ToString().ToLower());
            
                // 添加路径遮挡检查
                bool isPathBlocked = CheckPathBlocked(card.Position, targetPosition, cardManager);
                condition = Regex.Replace(condition, @"\bPathBlocked\b", isPathBlocked.ToString().ToLower());
                
                // 添加对角线遮挡检查
                bool isDiagonalBlocked = CheckDiagonalBlocked(card.Position, targetPosition, cardManager);
                condition = Regex.Replace(condition, @"\bDiagonalBlocked\b", isDiagonalBlocked.ToString().ToLower());

                // 替换Blocked
                bool isBlocked = isPathBlocked || isDiagonalBlocked;
                condition = Regex.Replace(condition, @"\bBlocked\b", isBlocked.ToString().ToLower());
                
                // 添加更详细的日志
                Debug.Log($"- PathBlocked: {isPathBlocked}");
                Debug.Log($"- DiagonalBlocked: {isDiagonalBlocked}");
                
                return condition;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"替换变量时发生错误: {e.Message}\n{e.StackTrace}");
                return "false";
            }
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

        // 检查直线路径上是否有遮挡
        private bool CheckPathBlocked(Vector2Int start, Vector2Int end, CardManager cardManager)
        {
            // 如果不是在同一直线上，返回true（视为被阻挡）
            if (start.x != end.x && start.y != end.y)
                return true;
            
            // 计算方向
            int dx = end.x - start.x;
            int dy = end.y - start.y;
            
            // 标准化方向
            if (dx != 0) dx = dx / Mathf.Abs(dx);
            if (dy != 0) dy = dy / Mathf.Abs(dy);
            
            // 检查路径上的每个格子
            Vector2Int current = start;
            while (current != end)
            {
                current.x += dx;
                current.y += dy;
                
                // 如果到达目标位置，跳过检查（目标位置可以有卡牌）
                if (current == end)
                    continue;
                
                // 检查当前位置是否有卡牌
                if (cardManager.GetCard(current) != null)
                {
                    Debug.Log($"路径被阻挡: 位置 {current} 有卡牌");
                    return true;
                }
            }
            
            return false;
        }

        // 检查对角线路径上是否有遮挡
        private bool CheckDiagonalBlocked(Vector2Int start, Vector2Int end, CardManager cardManager)
        {
            // 如果不是对角线移动，返回false
            int dx = Mathf.Abs(end.x - start.x);
            int dy = Mathf.Abs(end.y - start.y);
            if (dx != dy || dx == 0)
                return false;
            
            // 计算方向
            int dirX = (end.x - start.x) / dx;
            int dirY = (end.y - start.y) / dy;
            
            // 检查路径上的每个格子
            Vector2Int current = start;
            while (current != end)
            {
                current.x += dirX;
                current.y += dirY;
                
                // 如果到达目标位置，跳过检查
                if (current == end)
                    continue;
                
                // 检查当前位置是否有卡牌
                if (cardManager.GetCard(current) != null)
                {
                    Debug.Log($"对角线路径被阻挡: 位置 {current} 有卡牌");
                    return true;
                }
                
                // 对角线移动时，还需要检查"拐角"位置
                Vector2Int corner1 = new Vector2Int(start.x, current.y);
                Vector2Int corner2 = new Vector2Int(current.x, start.y);
                
                if (cardManager.GetCard(corner1) != null && cardManager.GetCard(corner2) != null)
                {
                    Debug.Log($"对角线路径被拐角阻挡: 位置 {corner1} 和 {corner2} 都有卡牌");
                    return true;
                }
            }
            
            return false;
        }
    }
} 