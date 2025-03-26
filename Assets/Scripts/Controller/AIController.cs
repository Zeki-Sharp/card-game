using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ChessGame
{
    public class AIController : MonoBehaviour
    {
        [SerializeField] private float actionDelay = 0.5f; // 行动之间的延迟
        [SerializeField] private float selectionDelay = 0.8f; // 选中卡牌后的延迟
        
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
            {
                Debug.LogError("AIController无法找到CardManager");
            }
            
            if (_turnManager == null)
            {
                Debug.LogError("AIController无法找到TurnManager");
            }
        }
        
        private void Start()
        {
            // 订阅回合变化事件
            if (_turnManager != null)
            {
                _turnManager.OnTurnChanged += OnTurnChanged;
            }
        }
        
        // 处理回合变化
        private void OnTurnChanged(TurnState turnState)
        {
            if (turnState == TurnState.EnemyTurn && !_isExecutingTurn)
            {
                Debug.Log("AI检测到敌方回合开始");
                StartCoroutine(ExecuteAITurn());
            }
        }
        
        // 执行AI回合
        public IEnumerator ExecuteAITurn()
        {
            if (_isExecutingTurn)
            {
                Debug.LogWarning("AI已经在执行回合，忽略重复调用");
                yield break;
            }
            
            _isExecutingTurn = true;
            Debug.Log("AI开始执行回合");
            
            // 重置所有敌方卡牌的行动状态
            _cardManager.ResetAllCardActions();
            
            // 获取所有敌方卡牌
            List<Card> enemyCards = GetEnemyCards();
            Debug.Log($"找到 {enemyCards.Count} 张敌方卡牌");
            
            // 随机排序敌方卡牌，使AI行为更加随机
            ShuffleList(enemyCards);
            
            // 随机选择一张卡牌行动
            Card selectedCard = enemyCards[0]; // 取第一张，因为已经随机排序过
            Debug.Log($"AI选择卡牌 {selectedCard.Data.Name} 在位置 {selectedCard.Position} 行动");
            
            // 选中卡牌并高亮
            _cardManager.SelectCard(selectedCard.Position);
            
            // 尝试攻击
            bool hasActed = TryAttackRandomly(selectedCard);
            
            // 如果没有攻击，尝试移动
            if (!hasActed)
            {
                TryMoveRandomly(selectedCard);
            }
            
            // 无论是否行动成功，都标记该卡牌为已行动
            selectedCard.HasActed = true;
            
            // 取消选中卡牌
            _cardManager.DeselectCard();
            
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
            Dictionary<Vector2Int, Card> allCards = _cardManager.GetAllCards();
            
            foreach (var pair in allCards)
            {
                Card card = pair.Value;
                if (card != null && card.OwnerId == 1 && !card.HasActed && !card.IsFaceDown) // 敌方卡牌OwnerId为1，且必须是正面的
                {
                    enemyCards.Add(card);
                }
            }
            
            return enemyCards;
        }
        
        // 随机排序列表
        private void ShuffleList<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }



        // 尝试随机攻击
        private bool TryAttackRandomly(Card card)
        {
            Debug.Log($"AI尝试攻击，位置: {card.Position}, 攻击范围: {card.AttackRange}");
            
            // 获取可攻击的位置
            List<Vector2Int> attackablePositions = card.GetAttackablePositions(_cardManager.BoardWidth, _cardManager.BoardHeight, _cardManager.GetAllCards());
            
            Debug.Log($"找到 {attackablePositions.Count} 个可攻击位置");
            
            // 如果有可攻击的位置，随机选择一个进行攻击
            if (attackablePositions.Count > 0)
            {
                Vector2Int targetPosition = attackablePositions[Random.Range(0, attackablePositions.Count)];
                Debug.Log($"AI攻击卡牌，从 {card.Position} 到 {targetPosition}");
                
                // 设置目标位置，这样会显示高亮
                _cardManager.SetTargetPosition(targetPosition);
                
                // 等待一小段时间，让玩家看到目标高亮
                StartCoroutine(DelayedAttack(card.Position, targetPosition));
                
                return true;
            }
            
            Debug.Log("AI没有可攻击的位置");
            return false;
        }
        
        // 延迟攻击，以便显示高亮效果
        private IEnumerator DelayedAttack(Vector2Int attackerPosition, Vector2Int targetPosition)
        {
            yield return new WaitForSeconds(selectionDelay);
            
            // 直接使用CardManager的AttackCard方法
            bool success = _cardManager.AttackCard(attackerPosition, targetPosition);
            
            if (success)
            {
                Debug.Log($"AI攻击成功: {attackerPosition} -> {targetPosition}");
            }
            else
            {
                Debug.LogWarning($"AI攻击失败: {attackerPosition} -> {targetPosition}");
            }
            
            // 清除目标位置
            _cardManager.ClearTargetPosition();
        }
        
        // 尝试随机移动
        private bool TryMoveRandomly(Card card)
        {
            Debug.Log($"AI尝试移动，位置: {card.Position}, 移动范围: {card.MoveRange}");
            
            // 获取可移动的位置
            List<Vector2Int> movablePositions = card.GetMovablePositions(_cardManager.BoardWidth, _cardManager.BoardHeight, _cardManager.GetAllCards());
            
            Debug.Log($"找到 {movablePositions.Count} 个可移动位置");
            
            // 如果有可移动的位置，随机选择一个进行移动
            if (movablePositions.Count > 0)
            {
                Vector2Int targetPosition = movablePositions[Random.Range(0, movablePositions.Count)];
                Debug.Log($"AI移动卡牌，从 {card.Position} 到 {targetPosition}");
                
                // 设置目标位置，这样会显示高亮
                _cardManager.SetTargetPosition(targetPosition);
                
                // 等待一小段时间，让玩家看到目标高亮
                StartCoroutine(DelayedMove(card.Position, targetPosition));
                
                return true;
            }
            
            Debug.Log("AI没有可移动的位置");
            return false;
        }
        
        // 延迟移动，以便显示高亮效果
        private IEnumerator DelayedMove(Vector2Int fromPosition, Vector2Int toPosition)
        {
            yield return new WaitForSeconds(selectionDelay);
            
            // 直接使用CardManager的MoveCard方法
            bool success = _cardManager.MoveCard(fromPosition, toPosition);
            
            if (success)
            {
                Debug.Log($"AI移动成功: {fromPosition} -> {toPosition}");
            }
            else
            {
                Debug.LogWarning($"AI移动失败: {fromPosition} -> {toPosition}");
            }
            
            // 清除目标位置
            _cardManager.ClearTargetPosition();
        }
    }
} 