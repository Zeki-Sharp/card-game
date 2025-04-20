using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ChessGame.Cards;
using ChessGame.Utils;

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
        /// 执行AI回合 - 改进版，按照行动类型优先级分组卡牌
        /// </summary>
        public IEnumerator ExecuteAITurn()
        {
            Debug.Log("AIController.ExecuteAITurn - AI回合开始执行");
            
            // 避免重复执行
            if (_isExecutingTurn)
            {
                Debug.LogWarning("AI已经在执行回合，忽略重复执行");
                yield break;
            }
            
            // 标记AI正在执行回合
            _isExecutingTurn = true;
            
            // 等待一小段时间，让玩家看到回合开始
            yield return new WaitForSeconds(actionDelay);
            
            // 获取所有敌方可行动卡牌
            List<Card> actionableCards = GetActionableEnemyCards();
            Debug.Log($"可行动的敌方卡牌数量: {actionableCards.Count}");
            
            // 如果没有可行动的卡牌，直接结束回合
            if (actionableCards.Count == 0)
            {
                Debug.Log("没有可行动的敌方卡牌，AI回合结束");
                _isExecutingTurn = false;
                _turnManager.EndEnemyTurn();
                yield break;
            }
            
            // 按优先级分组卡牌及其行动
            var cardsWithAbilities = new List<CardActionInfo>();
            var cardsCanAttack = new List<CardActionInfo>();
            var cardsCanMove = new List<CardActionInfo>();
            
            // 评估每张卡牌的行动能力
            foreach (Card card in actionableCards)
            {
                // 检查卡牌的能力
                List<AbilityConfiguration> abilities = _abilityManager.GetCardAbilities(card);
                
                // 尝试查找可使用的能力
                var abilityInfo = FindUsableAbility(card, abilities);
                if (abilityInfo != null)
                {
                    cardsWithAbilities.Add(abilityInfo);
                    Debug.Log($"卡牌 {card.Data.Name} 可以使用能力: {abilityInfo.Ability.abilityName}");
                    continue; // 已找到高优先级行动，不再考虑后续行动
                }
                
                // 尝试查找可攻击的目标
                var attackInfo = FindAttackAction(card);
                if (attackInfo != null)
                {
                    cardsCanAttack.Add(attackInfo);
                    Debug.Log($"卡牌 {card.Data.Name} 可以执行攻击");
                    continue; // 已找到次优先级行动，不再考虑移动
                }
                
                // 尝试查找可移动的位置
                var moveInfo = FindMoveAction(card);
                if (moveInfo != null)
                {
                    cardsCanMove.Add(moveInfo);
                    Debug.Log($"卡牌 {card.Data.Name} 可以执行移动");
                }
            }
            
            Debug.Log($"分组结果 - 可用能力: {cardsWithAbilities.Count}, 可攻击: {cardsCanAttack.Count}, 可移动: {cardsCanMove.Count}");
            
            // 按优先级选择行动
            bool actionExecuted = false;
            
            // 1. 优先使用能力
            if (cardsWithAbilities.Count > 0)
            {
                // 随机选择一个能力使用
                int index = UnityEngine.Random.Range(0, cardsWithAbilities.Count);
                var selectedAction = cardsWithAbilities[index];
                
                Debug.Log($"AI决定使用卡牌 {selectedAction.Card.Data.Name} 的能力: {selectedAction.Ability.abilityName}");
                yield return ExecuteAbilityAction(selectedAction);
                actionExecuted = true;
            }
            // 2. 其次执行攻击
            else if (cardsCanAttack.Count > 0)
            {
                // 随机选择一个攻击执行
                int index = UnityEngine.Random.Range(0, cardsCanAttack.Count);
                var selectedAction = cardsCanAttack[index];
                
                Debug.Log($"AI决定使用卡牌 {selectedAction.Card.Data.Name} 执行攻击");
                yield return ExecuteAbilityAction(selectedAction);
                actionExecuted = true;
            }
            // 3. 最后考虑移动
            else if (cardsCanMove.Count > 0)
            {
                // 随机选择一个移动执行
                int index = UnityEngine.Random.Range(0, cardsCanMove.Count);
                var selectedAction = cardsCanMove[index];
                
                Debug.Log($"AI决定使用卡牌 {selectedAction.Card.Data.Name} 执行移动");
                yield return ExecuteAbilityAction(selectedAction);
                actionExecuted = true;
            }
            
            // 如果没有执行任何行动，结束回合
            if (!actionExecuted)
            {
                Debug.LogWarning("AI无法执行任何行动，手动结束回合");
                _turnManager.EndEnemyTurn();
            }
            
            // 完成AI回合
            _isExecutingTurn = false;
        }
        
        /// <summary>
        /// 卡牌行动信息 - 存储卡牌可执行的行动
        /// </summary>
        private class CardActionInfo
        {
            public Card Card;
            public AbilityConfiguration Ability;
            public Vector2Int TargetPosition;
            
            public CardActionInfo(Card card, AbilityConfiguration ability, Vector2Int targetPosition)
            {
                Card = card;
                Ability = ability;
                TargetPosition = targetPosition;
            }
        }
        
        /// <summary>
        /// 查找卡牌可用的能力
        /// </summary>
        private CardActionInfo FindUsableAbility(Card card, List<AbilityConfiguration> abilities)
        {
            if (abilities.Count == 0) return null;
            
            // 创建一个能力可用性列表
            List<KeyValuePair<AbilityConfiguration, List<Vector2Int>>> usableAbilities = new List<KeyValuePair<AbilityConfiguration, List<Vector2Int>>>();
            
            // 检查每个能力的可用性
            foreach (AbilityConfiguration ability in abilities)
            {
                // 获取能力可用的目标位置
                List<Vector2Int> targetPositions = _abilityManager.GetAbilityRange(ability, card);
                
                if (targetPositions.Count > 0)
                {
                    bool isAttackOrMoveAbility = false;
                    
                    // 检查是否是攻击或移动能力
                    foreach (var action in ability.actionSequence)
                    {
                        if (action.actionType == AbilityActionConfig.ActionType.Attack || 
                            action.actionType == AbilityActionConfig.ActionType.Move)
                        {
                            isAttackOrMoveAbility = true;
                            break;
                        }
                    }
                    
                    // 跳过攻击和移动能力，它们将在后续检查中处理
                    if (!isAttackOrMoveAbility)
                    {
                        usableAbilities.Add(new KeyValuePair<AbilityConfiguration, List<Vector2Int>>(ability, targetPositions));
                    }
                }
            }
            
            // 如果有可用能力，随机选择一个
            if (usableAbilities.Count > 0)
            {
                int abilityIndex = UnityEngine.Random.Range(0, usableAbilities.Count);
                var selectedAbility = usableAbilities[abilityIndex];
                AbilityConfiguration ability = selectedAbility.Key;
                List<Vector2Int> targetPositions = selectedAbility.Value;
                
                // 随机选择一个目标
                Vector2Int targetPosition = targetPositions[UnityEngine.Random.Range(0, targetPositions.Count)];
                
                // 最终确认能力是否可以触发
                if (_abilityManager.CanTriggerAbility(ability, card, targetPosition))
                {
                    return new CardActionInfo(card, ability, targetPosition);
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 查找卡牌可执行的攻击行动
        /// </summary>
        private CardActionInfo FindAttackAction(Card card)
        {
            // 获取可攻击的位置
            List<Vector2Int> attackablePositions = card.GetAttackablePositions(_cardManager.BoardWidth, _cardManager.BoardHeight, _cardManager.GetAllCards());
            
            if (attackablePositions.Count == 0)
            {
                return null;
            }
            
            // 获取卡牌的攻击能力
            List<AbilityConfiguration> abilities = _abilityManager.GetCardAbilities(card);
            
            // 筛选出攻击类型的能力
            List<KeyValuePair<AbilityConfiguration, Vector2Int>> usableAttacks = new List<KeyValuePair<AbilityConfiguration, Vector2Int>>();
            
            // 检查每个位置和每个能力
            foreach (Vector2Int targetPosition in attackablePositions)
            {
                foreach (var ability in abilities)
                {
                    // 检查能力是否包含攻击动作
                    bool hasAttackAction = false;
                    foreach (var action in ability.actionSequence)
                    {
                        if (action.actionType == AbilityActionConfig.ActionType.Attack)
                        {
                            hasAttackAction = true;
                            break;
                        }
                    }
                    
                    if (hasAttackAction && _abilityManager.CanTriggerAbility(ability, card, targetPosition))
                    {
                        usableAttacks.Add(new KeyValuePair<AbilityConfiguration, Vector2Int>(ability, targetPosition));
                    }
                }
            }
            
            // 如果有可用的攻击能力，随机选择一个
            if (usableAttacks.Count > 0)
            {
                int attackIndex = UnityEngine.Random.Range(0, usableAttacks.Count);
                var selectedAttack = usableAttacks[attackIndex];
                
                return new CardActionInfo(card, selectedAttack.Key, selectedAttack.Value);
            }
            
            return null;
        }
        
        /// <summary>
        /// 查找卡牌可执行的移动行动
        /// </summary>
        private CardActionInfo FindMoveAction(Card card)
        {
            // 获取可移动的位置
            List<Vector2Int> movePositions = card.GetMovablePositions(_cardManager.BoardWidth, _cardManager.BoardHeight, _cardManager.GetAllCards());
            
            if (movePositions.Count == 0)
            {
                return null;
            }
            
            // 获取卡牌的移动能力
            List<AbilityConfiguration> abilities = _abilityManager.GetCardAbilities(card);
            
            // 筛选出移动类型的能力
            List<KeyValuePair<AbilityConfiguration, Vector2Int>> usableMoves = new List<KeyValuePair<AbilityConfiguration, Vector2Int>>();
            
            // 检查每个位置和每个能力
            foreach (Vector2Int targetPosition in movePositions)
            {
                foreach (var ability in abilities)
                {
                    // 检查能力是否包含移动动作
                    bool hasMoveAction = false;
                    foreach (var action in ability.actionSequence)
                    {
                        if (action.actionType == AbilityActionConfig.ActionType.Move)
                        {
                            hasMoveAction = true;
                            break;
                        }
                    }
                    
                    if (hasMoveAction && _abilityManager.CanTriggerAbility(ability, card, targetPosition))
                    {
                        usableMoves.Add(new KeyValuePair<AbilityConfiguration, Vector2Int>(ability, targetPosition));
                    }
                }
            }
            
            // 如果有可用的移动能力，随机选择一个
            if (usableMoves.Count > 0)
            {
                int moveIndex = UnityEngine.Random.Range(0, usableMoves.Count);
                var selectedMove = usableMoves[moveIndex];
                
                return new CardActionInfo(card, selectedMove.Key, selectedMove.Value);
            }
            
            return null;
        }
        
        /// <summary>
        /// 执行能力行动
        /// </summary>
        private IEnumerator ExecuteAbilityAction(CardActionInfo actionInfo)
        {
            Card card = actionInfo.Card;
            AbilityConfiguration ability = actionInfo.Ability;
            Vector2Int targetPosition = actionInfo.TargetPosition;
            
            // 设置目标位置，这样会显示高亮
            _cardManager.SetTargetPosition(targetPosition);
            
            // 等待一段时间，让玩家看清AI的选择
            yield return new WaitForSeconds(selectionDelay);
            
            // 执行能力
            yield return _abilityManager.ExecuteAbility(ability, card, targetPosition);
            
            // 标记卡牌已行动
            card.HasActed = true;
            
            // 与玩家回合结束逻辑保持一致
            Debug.Log($"AI完成行动: {ability.abilityName}，通知敌方行动完成");
            GameEventSystem.Instance.NotifyEnemyActionCompleted(card.OwnerId);
        }
        
        /// <summary>
        /// 获取可行动的敌方卡牌
        /// </summary>
        private List<Card> GetActionableEnemyCards()
        {
            // 获取所有敌方卡牌
            List<Card> enemyCards = GetEnemyCards();
            
            // 筛选出可行动的卡牌
            List<Card> actionableCards = new List<Card>();
            foreach (Card card in enemyCards)
            {
                if (!card.HasActed)
                {
                    actionableCards.Add(card);
                }
            }
            
            return actionableCards;
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
                int k = UnityEngine.Random.Range(0, n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
} 