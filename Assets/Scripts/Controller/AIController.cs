using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ChessGame
{
    public class AIController : MonoBehaviour
    {
        [SerializeField] private float actionDelay = 0.5f; // 行动之间的延迟
        
        private CardManager _cardManager;
        private TurnManager _turnManager;
        private bool hasActed = false;
        
        // 添加一个标志，表示AI是否正在执行回合
        private bool _isExecutingTurn = false;
        
        private void Awake()
        {
            _cardManager = FindObjectOfType<CardManager>();
            _turnManager = FindObjectOfType<TurnManager>();
            
            if (_cardManager == null)
                Debug.LogError("找不到CardManager组件");
                
            if (_turnManager == null)
                Debug.LogError("找不到TurnManager组件");
        }
        
        // 执行AI回合
        public void ExecuteAITurn()
        {
            // 如果已经在执行回合，则不再重复执行
            if (_isExecutingTurn)
            {
                Debug.LogWarning("AI已经在执行回合，忽略重复调用");
                return;
            }
            
            Debug.Log("AI开始执行回合");
            _isExecutingTurn = true;
            hasActed = false; // 重置hasActed变量
            StartCoroutine(ExecuteAITurnCoroutine());
        }
        
        // AI回合执行协程
        private IEnumerator ExecuteAITurnCoroutine()
        {
            // 等待一小段时间，让玩家看清楚AI回合开始
            Debug.Log("AI等待行动...");
            yield return new WaitForSeconds(actionDelay);
            
            // 获取所有敌方卡牌
            List<Card> enemyCards = GetEnemyCards();
            
            if (enemyCards.Count == 0)
            {
                Debug.Log("没有敌方卡牌可以行动");
                _turnManager.EndEnemyTurn();
                yield break;
            }
            
            // 随机选择一张敌方卡牌
            Card selectedCard = enemyCards[Random.Range(0, enemyCards.Count)];
            Debug.Log($"AI选择了卡牌: {selectedCard.Data.Name}, 位置: {selectedCard.Position}");
            
            // 选中卡牌
            _cardManager.SelectCard(selectedCard.Position);
            
            // 等待一小段时间
            yield return new WaitForSeconds(actionDelay);
            
            
            // 尝试攻击附近的玩家卡牌或背面卡牌
            bool attacked = TryAttackNearbyPlayerCard(selectedCard);
            Debug.Log($"AI攻击结果: {(attacked ? "成功" : "失败")}");
            
            if (attacked)
            {
                hasActed = true;
            }
            else
            {
                // 如果没有攻击，尝试移动
                bool moved = TryMoveRandomly(selectedCard);
                Debug.Log($"AI移动结果: {(moved ? "成功" : "失败")}");
                
                if (moved)
                {
                    hasActed = true;
                }
            }
            
            // 无论是否行动成功，都标记该卡牌为已行动
            selectedCard.HasActed = hasActed;
            
            // 等待一小段时间
            yield return new WaitForSeconds(actionDelay);
            
            // 结束AI回合
            Debug.Log("AI行动完成，结束敌方回合");
            _isExecutingTurn = false; // 重置执行状态
            _turnManager.EndEnemyTurn();
        }
        
        // 获取所有敌方卡牌
        private List<Card> GetEnemyCards()
        {
            List<Card> enemyCards = new List<Card>();
            
            for (int x = 0; x < _cardManager.BoardWidth; x++)
            {
                for (int y = 0; y < _cardManager.BoardHeight; y++)
                {
                    Vector2Int position = new Vector2Int(x, y);
                    Card card = _cardManager.GetCard(position);
                    
                    if (card != null && card.OwnerId == 1 && !card.HasActed && !card.IsFaceDown) // 敌方卡牌OwnerId为1，且必须是正面的
                    {
                        enemyCards.Add(card);
                    }
                }
            }
            
            return enemyCards;
        }
        
        // 尝试攻击附近的玩家卡牌或背面卡牌
        private bool TryAttackNearbyPlayerCard(Card card)
        {
            Vector2Int position = card.Position;
            int attackRange = _cardManager.AttackRange;
            
            Debug.Log($"AI尝试攻击，位置: {position}, 攻击范围: {attackRange}");
            
            // 获取可攻击的位置
            List<Vector2Int> attackablePositions = new List<Vector2Int>();
            
            // 检查周围的格子
            for (int dx = -attackRange; dx <= attackRange; dx++)
            {
                for (int dy = -attackRange; dy <= attackRange; dy++)
                {
                    // 跳过原位置
                    if (dx == 0 && dy == 0) continue;
                    
                    // 计算曼哈顿距离
                    int manhattanDistance = Mathf.Abs(dx) + Mathf.Abs(dy);
                    Debug.Log($"检查位置偏移: ({dx}, {dy}), 曼哈顿距离: {manhattanDistance}, 攻击范围: {attackRange}");
                    
                    if (manhattanDistance <= attackRange)
                    {
                        int x = position.x + dx;
                        int y = position.y + dy;
                        
                        // 检查是否在棋盘范围内
                        if (x >= 0 && x < _cardManager.BoardWidth && y >= 0 && y < _cardManager.BoardHeight)
                        {
                            Vector2Int targetPos = new Vector2Int(x, y);
                            Card targetCard = _cardManager.GetCard(targetPos);
                            
                            // 可以攻击玩家卡牌或任何背面卡牌
                            if (targetCard != null && (targetCard.OwnerId == 0 || targetCard.IsFaceDown))
                            {
                                attackablePositions.Add(targetPos);
                                Debug.Log($"找到可攻击位置: {targetPos}, 目标卡牌: {targetCard.Data.Name}, 所有者: {targetCard.OwnerId}, 是否背面: {targetCard.IsFaceDown}");
                            }
                        }
                    }
                }
            }
            
            Debug.Log($"找到 {attackablePositions.Count} 个可攻击位置");
            
            // 如果有可攻击的目标，随机选择一个进行攻击
            if (attackablePositions.Count > 0)
            {
                Vector2Int targetPosition = attackablePositions[Random.Range(0, attackablePositions.Count)];
                Debug.Log($"AI攻击卡牌，位置: {targetPosition}");
                
                // 设置目标位置并执行攻击
                _cardManager.SelectCard(card.Position);
                _cardManager.SetTargetPosition(targetPosition);
                bool result = _cardManager.ExecuteAttack();
                Debug.Log($"攻击执行结果: {result}");
                
                return result;
            }
            
            return false;
        }
        
        // 尝试随机移动
        private bool TryMoveRandomly(Card card)
        {
            Vector2Int position = card.Position;
            int moveRange = _cardManager.MoveRange; // 使用CardManager中定义的移动范围
            
            Debug.Log($"AI尝试移动，位置: {position}, 移动范围: {moveRange}");
            
            // 获取可移动的位置
            List<Vector2Int> movablePositions = new List<Vector2Int>();
            
            // 检查周围的格子
            for (int dx = -moveRange; dx <= moveRange; dx++)
            {
                for (int dy = -moveRange; dy <= moveRange; dy++)
                {
                    // 跳过原位置
                    if (dx == 0 && dy == 0) continue;
                    
                    // 计算曼哈顿距离
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) <= moveRange)
                    {
                        int x = position.x + dx;
                        int y = position.y + dy;
                        
                        // 检查是否在棋盘范围内
                        if (x >= 0 && x < _cardManager.BoardWidth && y >= 0 && y < _cardManager.BoardHeight)
                        {
                            Vector2Int targetPos = new Vector2Int(x, y);
                            if (!_cardManager.HasCard(targetPos))
                            {
                                movablePositions.Add(targetPos);
                                Debug.Log($"找到可移动位置: {targetPos}");
                            }
                        }
                    }
                }
            }
            
            Debug.Log($"找到 {movablePositions.Count} 个可移动位置");
            
            // 如果有可移动的位置，随机选择一个进行移动
            if (movablePositions.Count > 0)
            {
                Vector2Int targetPosition = movablePositions[Random.Range(0, movablePositions.Count)];
                Debug.Log($"AI移动卡牌，从 {position} 到 {targetPosition}");
                
                // 设置目标位置并执行移动
                _cardManager.SelectCard(card.Position);
                _cardManager.SetTargetPosition(targetPosition);
                _cardManager.ExecuteMove();
                
                return true;
            }
            
            Debug.Log("AI没有可移动的位置");
            return false;
        }

        // 在AIController中添加SetTargetCard方法
        private void SetTargetCard(Vector2Int position)
        {
            // 选中卡牌
            _cardManager.SelectCard(position);
            
            // 设置目标位置
            _cardManager.SetTargetPosition(position);
        }
    }
} 