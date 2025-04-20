using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ChessGame.Cards;

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
        
        // 添加能力管理器引用
        private AbilityManager _abilityManager;
        
        private void Awake()
        {
            
        }
        
        private void Start()
        {
            // 初始化引用
             InitializeReferences();

            // 订阅回合变化事件
            if (_turnManager != null)
            {
                _turnManager.OnTurnChanged += OnTurnChanged;
            }
        }
        

        // 初始化引用
        private void InitializeReferences()
        {
            _cardManager = FindObjectOfType<CardManager>();
            _turnManager = FindObjectOfType<TurnManager>();
            _abilityManager = AbilityManager.Instance;
            
            if (_cardManager == null)
            {
                Debug.LogError("AIController无法找到CardManager");
            }
            
            if (_turnManager == null)
            {
                Debug.LogError("AIController无法找到TurnManager");
            }
            
            if (_abilityManager == null)
            {
                Debug.LogError("AIController无法找到AbilityManager");
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
        
        /// <summary>
        /// 执行AI回合
        /// </summary>
        public IEnumerator ExecuteAITurn()
        {
            Debug.Log("AIController.ExecuteAITurn");
            
            // 标记AI正在执行回合
            _isExecutingTurn = true;
            
            // 等待一小段时间，让玩家看到回合开始
            yield return new WaitForSeconds(actionDelay);
            
            // 获取所有敌方卡牌
            List<Card> enemyCards = GetEnemyCards();
            
            // 如果没有敌方卡牌，直接结束回合
            if (enemyCards.Count == 0)
            {
                Debug.Log("没有敌方卡牌，AI回合结束");
                _isExecutingTurn = false;
                _turnManager.EndEnemyTurn();
                yield break;
            }
            
            // 打乱敌方卡牌顺序，随机选择一张执行行动
            ShuffleList(enemyCards);
            
            // 找到第一张可以行动的卡牌
            Card actionCard = null;
            foreach (Card card in enemyCards)
            {
                // 如果卡牌已经行动过，跳过
                if (card.HasActed)
                    continue;
                    
                // 找到第一张可以行动的卡牌
                actionCard = card;
                break;
            }
            
            // 如果没有可行动的卡牌，直接结束回合
            if (actionCard == null)
            {
                Debug.Log("没有可行动的敌方卡牌，AI回合结束");
                _isExecutingTurn = false;
                _turnManager.EndEnemyTurn();
                yield break;
            }
            
            // 获取卡牌的能力列表
            List<AbilityConfiguration> abilities = _abilityManager.GetCardAbilities(actionCard);
            
            // 如果卡牌有能力，尝试使用能力
            bool actionTaken = false;
            if (abilities.Count > 0)
            {
                // 随机选择一个能力
                AbilityConfiguration ability = abilities[Random.Range(0, abilities.Count)];
                
                // 获取能力可用的目标位置
                List<Vector2Int> targetPositions = _abilityManager.GetAbilityRange(ability, actionCard);
                
                // 如果有可用的目标位置，随机选择一个执行能力
                if (targetPositions.Count > 0)
                {
                    Vector2Int targetPosition = targetPositions[Random.Range(0, targetPositions.Count)];
                    
                    // 检查能力是否可以触发
                    if (_abilityManager.CanTriggerAbility(ability, actionCard, targetPosition))
                    {
                        Debug.Log($"AI使用卡牌 {actionCard.Data.Name} 的能力 {ability.abilityName} 于位置 {targetPosition}");
                        
                        // 设置目标位置，这样会显示高亮
                        _cardManager.SetTargetPosition(targetPosition);
                        
                        // 等待一段时间，让玩家看清AI的选择
                        yield return new WaitForSeconds(selectionDelay);
                        
                        // 执行能力
                        yield return _abilityManager.ExecuteAbility(ability, actionCard, targetPosition);
                        
                        // 标记行动已执行
                        actionTaken = true;
                        actionCard.HasActed = true;
                        
                        // 通知敌方行动完成
                        GameEventSystem.Instance.NotifyEnemyActionCompleted(actionCard.OwnerId);
                    }
                }
            }
            
            // 如果没有使用能力，尝试执行攻击或移动
            if (!actionTaken)
            {
                // 尝试执行攻击
                bool attacked = TryAttackWithCard(actionCard);
                
                // 如果没有攻击，尝试移动
                if (!attacked)
                {
                    TryMoveCard(actionCard);
                }
            }
            
            // 等待一段时间，让玩家看清AI的行动
            yield return new WaitForSeconds(actionDelay);
            
            // 标记AI回合执行完毕
            _isExecutingTurn = false;
            
            // 结束敌方回合
            _turnManager.EndEnemyTurn();
            
            Debug.Log("AI回合执行完毕");
        }
        
        // 获取所有敌方卡牌
        private List<Card> GetEnemyCards()
        {
            List<Card> enemyCards = new List<Card>();
            Dictionary<Vector2Int, Card> allCards = _cardManager.GetAllCards();
            
            foreach (var pair in allCards)
            {
                Card card = pair.Value;
                if (card != null && card.OwnerId == 1 && !card.IsFaceDown) // 敌方卡牌OwnerId为1，且必须是正面的
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

        // 尝试使用卡牌攻击
        private bool TryAttackWithCard(Card card)
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
                
                // 执行攻击
                bool success = _cardManager.AttackCard(card.Position, targetPosition);
                
                if (success)
                {
                    Debug.Log($"AI攻击成功: {card.Position} -> {targetPosition}");
                    return true;
                }
                else
                {
                    Debug.LogWarning($"AI攻击失败: {card.Position} -> {targetPosition}");
                }
            }
            
            Debug.Log("AI没有可攻击的位置");
            return false;
        }
        
        // 尝试移动卡牌
        private bool TryMoveCard(Card card)
        {
            Debug.Log($"AI尝试移动，位置: {card.Position}, 移动范围: {card.MoveRange}");
            
            // 获取可移动的位置
            List<Vector2Int> movePositions = card.GetMovablePositions(_cardManager.BoardWidth, _cardManager.BoardHeight, _cardManager.GetAllCards());
            
            Debug.Log($"找到 {movePositions.Count} 个可移动位置");
            
            // 如果有可移动的位置，随机选择一个进行移动
            if (movePositions.Count > 0)
            {
                Vector2Int targetPosition = movePositions[Random.Range(0, movePositions.Count)];
                Debug.Log($"AI移动卡牌，从 {card.Position} 到 {targetPosition}");
                
                // 执行移动
                bool success = _cardManager.MoveCard(card.Position, targetPosition);
                
                if (success)
                {
                    Debug.Log($"AI移动成功: {card.Position} -> {targetPosition}");
                    return true;
                }
                else
                {
                    Debug.LogWarning($"AI移动失败: {card.Position} -> {targetPosition}");
                }
            }
            
            Debug.Log("AI没有可移动的位置");
            return false;
        }
    }
} 