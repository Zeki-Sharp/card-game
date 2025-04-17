using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
namespace ChessGame.Cards
{
    /// <summary>
    /// 能力条件解析器 - 负责解析和检查能力的触发条件
    /// </summary>
    public class AbilityConditionResolver
    {
        
        /// <summary>
        /// 检查能力是否可以触发
        /// </summary>
        /// <param name="condition">条件表达式</param>
        /// <param name="sourceCard">源卡牌</param>
        /// <param name="targetPosition">目标位置</param>
        /// <param name="cardManager">卡牌管理器</param>
        /// <returns>是否满足条件</returns>
        public bool CheckCondition(string condition, Card sourceCard, Vector2Int targetPosition, CardManager cardManager)
        {
            Debug.Log($"【条件检查】检查条件: {condition}, 源卡牌: {sourceCard.Data.Name}, 目标位置: {targetPosition}");
            
            // 如果条件为空，默认为true
            if (string.IsNullOrEmpty(condition))
            {
                Debug.Log("【条件检查】条件为空，返回true");
                return true;
            }
            
            // 解析条件表达式
            bool result = EvaluateCondition(condition, sourceCard, targetPosition, cardManager);
            
            Debug.Log($"【条件检查】条件 {condition} 的结果: {result}");
            return result;
        }
        

        
 

        /// <summary>
        /// 检查攻击范围条件
        /// </summary>
        public bool CheckAttackRangeCondition(Card sourceCard, Vector2Int targetPosition, CardManager cardManager)
        {
            // 获取目标卡牌
            Card targetCard = cardManager.GetCard(targetPosition);
            if (targetCard == null)
            {
                return false;
            }
            
            // 检查是否是敌方卡牌
            if (sourceCard.OwnerId == targetCard.OwnerId)
            {
                return false;
            }
            
            // 检查是否在攻击范围内
            return sourceCard.CanAttack(targetPosition, cardManager.GetAllCards());
        }

        private string ReplaceVariables(string condition, Card card, Vector2Int targetPosition, CardManager cardManager)
        {
            try
            {
                int dx1 = Mathf.Abs(targetPosition.x - card.Position.x); // 水平方向距离
                int dy1 = Mathf.Abs(targetPosition.y - card.Position.y); // 垂直方向距离
                bool isStraight = dy1 == 0 || dx1 == 0;
                int straightDistance = isStraight ? Mathf.Max(dx1, dy1) : int.MaxValue;
                int manhattanDistance = dx1 + dy1;
                bool isDiagonal = dx1 == dy1 && dx1 > 0;
                int diagonalDistance = isDiagonal ? dx1 : int.MaxValue;

                Card targetCard = cardManager.GetCard(targetPosition);
                bool isEnemy = targetCard != null && targetCard.OwnerId != card.OwnerId && !targetCard.IsFaceDown;
                bool isFaceDown = targetCard != null && targetCard.IsFaceDown;
                bool isEnemyOrFaceDown = targetCard != null && (targetCard.OwnerId != card.OwnerId || targetCard.IsFaceDown);
                bool isEmpty = targetCard == null;
                bool isAlly = targetCard != null && targetCard.OwnerId == card.OwnerId && !targetCard.IsFaceDown;

                bool isPathBlocked = CheckPathBlocked(card.Position, targetPosition, cardManager);
                bool isDiagonalBlocked = CheckDiagonalBlocked(card.Position, targetPosition, cardManager);
                bool isBlocked = isPathBlocked || isDiagonalBlocked;

                // 替换数值变量
                condition = Regex.Replace(condition, @"\bStraightDistance\b", straightDistance.ToString());
                condition = Regex.Replace(condition, @"\bDistance\b", manhattanDistance.ToString());
                condition = Regex.Replace(condition, @"\bDiagonalDistance\b", diagonalDistance.ToString());
                condition = Regex.Replace(condition, @"\bMoveRange\b", card.MoveRange.ToString());
                condition = Regex.Replace(condition, @"\bAttackRange\b", card.AttackRange.ToString());

                // 替换布尔变量
                condition = ReplaceBool(condition, "Enemy", isEnemy);
                condition = ReplaceBool(condition, "FaceDown", isFaceDown);
                condition = ReplaceBool(condition, "EnemyOrFaceDown", isEnemyOrFaceDown);
                condition = ReplaceBool(condition, "Empty", isEmpty);
                condition = ReplaceBool(condition, "Ally", isAlly);
                condition = ReplaceBool(condition, "PathBlocked", isPathBlocked);
                condition = ReplaceBool(condition, "DiagonalBlocked", isDiagonalBlocked);
                condition = ReplaceBool(condition, "Blocked", isBlocked);

                // 替换 TurnCounter[AbilityId]
                condition = Regex.Replace(condition, @"TurnCounter\[([^\]]+)\]", match =>
                {
                    try
                    {
                        string abilityId = match.Groups[1].Value;
                        int count = card.GetTurnCounter(abilityId);
                        Debug.Log($"替换回合计数器变量: {match.Value} -> {count}, 能力ID: {abilityId}");
                        return count.ToString();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"替换回合计数器变量时发生错误: {e.Message}\n{e.StackTrace}");
                        return "0";
                    }
                });

                return condition;
            }
            catch (Exception e)
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

        private bool EvaluateCondition(string condition, Card sourceCard, Vector2Int targetPosition, CardManager cardManager)
        {
            // 替换变量
            string resolvedCondition = ReplaceVariables(condition, sourceCard, targetPosition, cardManager);
            Debug.Log($"替换变量后的条件: {resolvedCondition}");
            
            // 检查条件
            return EvaluateExpression(resolvedCondition);
        }

    

        private string ReplaceBool(string condition, string keyword, bool value)
        {       
            return Regex.Replace(condition, $@"\b{keyword}\b", value.ToString().ToLower());
        }

        /// <summary>
        /// 解析条件表达式
        /// </summary>
        public bool ResolveCondition(string condition, Card sourceCard, Vector2Int targetPosition, CardManager cardManager)
        {
            // 如果条件为空，默认为真
            if (string.IsNullOrEmpty(condition))
                return true;
        
            // 替换变量
            string resolvedCondition = ReplaceVariables(condition, sourceCard, targetPosition, cardManager);
            Debug.Log($"替换变量后的条件: {resolvedCondition}");
        
            // 检查条件
            return EvaluateExpression(resolvedCondition);
        }
    }
} 

 