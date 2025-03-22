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
            Debug.Log("AI开始执行回合");
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
            
            // 尝试攻击附近的玩家卡牌
            bool attacked = TryAttackNearbyPlayerCard(selectedCard);
            
            // 如果没有攻击，尝试移动
            if (!attacked)
            {
                TryMoveRandomly(selectedCard);
            }
            
            // 等待一小段时间
            yield return new WaitForSeconds(actionDelay);
            
            // 结束AI回合
            Debug.Log("AI行动完成，结束敌方回合");
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
                    
                    if (card != null && card.OwnerId == 1 && !card.HasActed) // 敌方卡牌OwnerId为1
                    {
                        enemyCards.Add(card);
                    }
                }
            }
            
            return enemyCards;
        }
        
        // 尝试攻击附近的玩家卡牌
        private bool TryAttackNearbyPlayerCard(Card card)
        {
            Vector2Int position = card.Position;
            int attackRange = _cardManager.AttackRange;
            
            // 检查周围是否有可攻击的玩家卡牌
            List<Vector2Int> attackablePositions = new List<Vector2Int>();
            
            for (int x = position.x - attackRange; x <= position.x + attackRange; x++)
            {
                for (int y = position.y - attackRange; y <= position.y + attackRange; y++)
                {
                    if (x >= 0 && x < _cardManager.BoardWidth && y >= 0 && y < _cardManager.BoardHeight)
                    {
                        // 计算曼哈顿距离
                        int distance = Mathf.Abs(x - position.x) + Mathf.Abs(y - position.y);
                        if (distance <= attackRange)
                        {
                            Vector2Int targetPos = new Vector2Int(x, y);
                            Card targetCard = _cardManager.GetCard(targetPos);
                            
                            if (targetCard != null && targetCard.OwnerId == 0) // 玩家卡牌OwnerId为0
                            {
                                attackablePositions.Add(targetPos);
                            }
                        }
                    }
                }
            }
            
            // 如果有可攻击的目标，随机选择一个进行攻击
            if (attackablePositions.Count > 0)
            {
                Vector2Int targetPosition = attackablePositions[Random.Range(0, attackablePositions.Count)];
                Debug.Log($"AI攻击玩家卡牌，位置: {targetPosition}");
                
                // 设置目标位置并执行攻击
                _cardManager.SetTargetPosition(targetPosition);
                _cardManager.ExecuteAttack();
                
                return true;
            }
            
            return false;
        }
        
        // 尝试随机移动
        private bool TryMoveRandomly(Card card)
        {
            Vector2Int position = card.Position;
            int moveRange = _cardManager.MoveRange;
            
            // 获取可移动的位置
            List<Vector2Int> movablePositions = new List<Vector2Int>();
            
            for (int x = position.x - moveRange; x <= position.x + moveRange; x++)
            {
                for (int y = position.y - moveRange; y <= position.y + moveRange; y++)
                {
                    if (x >= 0 && x < _cardManager.BoardWidth && y >= 0 && y < _cardManager.BoardHeight)
                    {
                        // 计算曼哈顿距离
                        int distance = Mathf.Abs(x - position.x) + Mathf.Abs(y - position.y);
                        if (distance <= moveRange)
                        {
                            Vector2Int targetPos = new Vector2Int(x, y);
                            if (!_cardManager.HasCard(targetPos))
                            {
                                movablePositions.Add(targetPos);
                            }
                        }
                    }
                }
            }
            
            // 如果有可移动的位置，随机选择一个进行移动
            if (movablePositions.Count > 0)
            {
                Vector2Int targetPosition = movablePositions[Random.Range(0, movablePositions.Count)];
                Debug.Log($"AI移动卡牌，从 {position} 到 {targetPosition}");
                
                // 设置目标位置并执行移动
                _cardManager.SetTargetPosition(targetPosition);
                _cardManager.ExecuteMove();
                
                return true;
            }
            
            Debug.Log("AI没有可移动的位置");
            return false;
        }
    }
} 