using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ChessGame;
using ChessGame.FSM.TurnState;
using System;

namespace ChessGame.Cards
{
    /// <summary>
    /// 能力管理器 - 负责管理所有卡牌的能力
    /// </summary>
    public class AbilityManager : MonoBehaviour
    {
        private static AbilityManager _instance;
        public static AbilityManager Instance => _instance;
        public CardManager _cardManager { get; private set; }
        
        // 存储卡牌类型与能力配置的映射
        private Dictionary<int, List<AbilityConfiguration>> _cardTypeAbilities = new Dictionary<int, List<AbilityConfiguration>>();
        
        private AbilityExecutor _abilityExecutor;
        private AbilityConditionResolver _conditionResolver;
        private AbilityRangeCalculator _rangeCalculator;
        
        private GameEventSystem _gameEventSystem;
        
        // 添加一个静态标志来指示是否正在执行能力
        private static bool _isExecutingAbility = false;
        
        // 添加公共属性来访问这个标志
        public static bool IsExecutingAbility
        {
            get { return _isExecutingAbility; }
        }

        public enum HighlightType
        {
            Move,
            Attack,
            Ability
        }

        private TurnManager _turnManager;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            // 获取CardManager
            _cardManager = FindObjectOfType<CardManager>();

            _abilityExecutor = new AbilityExecutor(_cardManager);
            _conditionResolver = new AbilityConditionResolver();
            _rangeCalculator = new AbilityRangeCalculator(_cardManager, _conditionResolver);
            
            _gameEventSystem = GameEventSystem.Instance;
            
            // 获取TurnManager
            _turnManager = FindObjectOfType<TurnManager>();
            
            // 设置脚本执行顺序
            DontDestroyOnLoad(gameObject);
            
            Debug.Log("AbilityManager初始化完成，时间: " + Time.time);
        }
        
        private void Start()
        {
            // 订阅回合开始事件
            _gameEventSystem.OnTurnStarted += HandleTurnStarted;
            
            // 订阅回合结束事件
            _gameEventSystem.OnTurnEnded += HandleTurnEnded;
            
            // 订阅卡牌翻面事件
            _gameEventSystem.OnCardFlipped += HandleCardFlipped;
            
            // 订阅卡牌添加事件
            _gameEventSystem.OnCardAdded += HandleCardAdded;
            
            // 加载能力配置
            LoadAbilityConfigurations();
            
            // 从CardDataSO加载能力
            LoadAbilitiesFromCardDataSO();
            
            // 初始化所有已存在卡牌的能力冷却
            InitializeAllCardsCooldowns();
        }
        
        private void OnDestroy()
        {
            // 取消订阅事件
            if (_gameEventSystem != null)
            {
                _gameEventSystem.OnTurnStarted -= HandleTurnStarted;
                _gameEventSystem.OnTurnEnded -= HandleTurnEnded;
                _gameEventSystem.OnCardFlipped -= HandleCardFlipped;
                _gameEventSystem.OnCardAdded -= HandleCardAdded;
            }
        }
        
        /// <summary>
        /// 处理回合开始事件
        /// </summary>
        private void HandleTurnStarted(int playerId)
        {
            Debug.Log($"AbilityManager: 处理回合开始事件，玩家ID: {playerId}");
            
            // 减少所有卡牌的冷却计数
            ReduceAllCooldowns(playerId);
            
            // 触发回合开始时的自动能力
            FSM.TurnState.TurnPhase triggerPhase = playerId == 0 ? 
                FSM.TurnState.TurnPhase.PlayerTurnStart : 
                FSM.TurnState.TurnPhase.EnemyTurnStart;
                
            TriggerAutomaticAbilitiesAtPhase(playerId, triggerPhase);
        }
        
        /// <summary>
        /// 减少所有卡牌的冷却计数
        /// </summary>
        private void ReduceAllCooldowns(int playerId)
        {
            Debug.Log($"[冷却系统] 开始减少玩家 {playerId} 所有卡牌的冷却计数");
            
            Dictionary<Vector2Int, Card> allCards = _cardManager.GetAllCards();
            foreach (var kvp in allCards)
            {
                Card card = kvp.Value;
                if (card.OwnerId == playerId && !card.IsFaceDown)
                {
                    Debug.Log($"[冷却系统] 处理卡牌 {card.Data.Name} 的冷却");
                    
                    foreach (var ability in GetCardAbilities(card))
                    {
                        if (ability.cooldown > 0)
                        {
                            string cooldownCounterId = ability.GetCooldownCounterId();
                            int currentCooldown = card.GetTurnCounter(cooldownCounterId);
                            
                            Debug.Log($"[冷却系统] 卡牌 {card.Data.Name} 能力 {ability.abilityName} 当前冷却: {currentCooldown}");
                            
                            if (currentCooldown > 0)
                            {
                                card.SetTurnCounter(cooldownCounterId, currentCooldown - 1);
                                Debug.Log($"[冷却系统] 减少卡牌 {card.Data.Name} 能力 {ability.abilityName} 的冷却，从 {currentCooldown} 到 {currentCooldown - 1}");
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 处理回合结束事件
        /// </summary>
        private void HandleTurnEnded(int playerId)
        {
            // 获取当前回合阶段
            FSM.TurnState.TurnPhase currentPhase = FSM.TurnState.TurnPhase.PlayerTurnEnd;
            if (_turnManager != null && _turnManager.GetTurnStateMachine() != null)
            {
                currentPhase = _turnManager.GetTurnStateMachine().GetCurrentPhase();
            }
            
            Debug.Log($"AbilityManager: 处理回合结束事件，玩家ID: {playerId}，当前回合阶段: {currentPhase}");
            
            // 触发回合结束时的自动能力
            bool isPlayerTurn = playerId == 0; // 假设玩家ID为0
            FSM.TurnState.TurnPhase triggerPhase = isPlayerTurn ? 
                FSM.TurnState.TurnPhase.PlayerTurnEnd : 
                FSM.TurnState.TurnPhase.EnemyTurnEnd;
                
            TriggerAutomaticAbilitiesAtPhase(playerId, triggerPhase);
        }
        
        /// <summary>
        /// 处理卡牌添加事件
        /// </summary>
        private void HandleCardAdded(Vector2Int position, int ownerId, bool isFaceDown)
        {
            if (!isFaceDown) // 只处理正面卡牌
            {
                Card card = _cardManager.GetCard(position);
                if (card != null)
                {
                    Debug.Log($"AbilityManager: 处理卡牌添加事件，卡牌: {card.Data.Name}");
                    
                    // 初始化卡牌的能力冷却
                    foreach (var ability in GetCardAbilities(card))
                    {
                        if (ability.cooldown > 0)
                        {
                            string cooldownCounterId = ability.GetCooldownCounterId();
                            
                            // 设置初始冷却值为配置的冷却值
                            card.SetTurnCounter(cooldownCounterId, ability.cooldown);
                            
                            Debug.Log($"[冷却系统] 卡牌 {card.Data.Name} 添加，初始化能力 {ability.abilityName} 的冷却为 {ability.cooldown}");
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 处理卡牌翻面事件
        /// </summary>
        private void HandleCardFlipped(Vector2Int position, bool isFaceDown)
        {
            if (!isFaceDown) // 只处理从背面翻到正面的情况
            {
                Card card = _cardManager.GetCard(position);
                if (card != null)
                {
                    Debug.Log($"AbilityManager: 处理卡牌翻面事件，卡牌: {card.Data.Name}");
                    
                    // 初始化卡牌的能力冷却
                    foreach (var ability in GetCardAbilities(card))
                    {
                        if (ability.cooldown > 0)
                        {
                            string cooldownCounterId = ability.GetCooldownCounterId();
                            
                            // 设置初始冷却值为配置的冷却值
                            card.SetTurnCounter(cooldownCounterId, ability.cooldown);
                            
                            Debug.Log($"[冷却系统] 卡牌 {card.Data.Name} 翻面，初始化能力 {ability.abilityName} 的冷却为 {ability.cooldown}");
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 加载所有能力配置
        /// </summary>
        private void LoadAbilityConfigurations()
        {
            AbilityConfiguration[] configs = Resources.LoadAll<AbilityConfiguration>("Abilities");
            if (configs.Length == 0)
            {
                Debug.LogWarning("未找到任何能力配置");
                return;
            }
            
            Debug.Log($"加载了 {configs.Length} 个能力配置");
            
            // 这里简化处理，假设每个能力配置文件名包含卡牌类型ID
            foreach (var config in configs)
            {
                string fileName = config.name;
                if (fileName.StartsWith("Ability_"))
                {
                    string[] parts = fileName.Split('_');
                    if (parts.Length >= 2 && int.TryParse(parts[1], out int cardTypeId))
                    {
                        RegisterAbility(cardTypeId, config);
                        Debug.Log($"注册能力: {config.abilityName} 到卡牌类型 {cardTypeId}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 注册卡牌类型的能力
        /// </summary>
        public void RegisterAbility(int cardTypeId, AbilityConfiguration ability)
        {
            if (ability == null)
            {
                Debug.LogError($"尝试为卡牌类型 {cardTypeId} 注册空能力");
                return;
            }
            
            if (!_cardTypeAbilities.ContainsKey(cardTypeId))
            {
                _cardTypeAbilities[cardTypeId] = new List<AbilityConfiguration>();
            }
            
            // 避免重复添加相同能力
            if (!_cardTypeAbilities[cardTypeId].Contains(ability))
            {
                _cardTypeAbilities[cardTypeId].Add(ability);
                Debug.Log($"为卡牌类型 {cardTypeId} 注册能力: {ability.abilityName}");
            }
        }
        
        /// <summary>
        /// 获取卡牌可用的能力
        /// </summary>
        public List<AbilityConfiguration> GetCardAbilities(Card card)
        {
            if (card == null)
            {
                Debug.LogError("尝试获取空卡牌的能力");
                return new List<AbilityConfiguration>();
            }
            
            int cardTypeId = card.Data.Id;
            if (_cardTypeAbilities.TryGetValue(cardTypeId, out List<AbilityConfiguration> abilities))
            {
                Debug.Log($"获取卡牌 {card.Data.Name}(ID:{cardTypeId}) 的能力，数量: {abilities.Count}");
                return abilities;
            }
            
            Debug.Log($"卡牌 {card.Data.Name}(ID:{cardTypeId}) 没有注册能力");
            return new List<AbilityConfiguration>();
        }
        
        /// <summary>
        /// 检查能力是否可以触发
        /// </summary>
        public bool CanTriggerAbility(AbilityConfiguration ability, Card card, Vector2Int position, CardManager cardManager)
        {
            Debug.Log($"[冷却系统] 检查能力 {ability.abilityName} 是否可以触发");
            
            // 检查冷却
            if (ability.cooldown > 0)
            {
                string cooldownCounterId = ability.GetCooldownCounterId();
                int currentCooldown = card.GetTurnCounter(cooldownCounterId);
                
                Debug.Log($"[冷却系统] 能力 {ability.abilityName} 当前冷却: {currentCooldown}");
                
                if (currentCooldown > 0)
                {
                    Debug.Log($"[冷却系统] 能力 {ability.abilityName} 冷却中，无法触发");
                    return false;
                }
            }
            
            // 检查条件
            bool conditionMet = _conditionResolver.ResolveCondition(ability.triggerCondition, card, position, cardManager);
            Debug.Log($"[冷却系统] 能力 {ability.abilityName} 条件检查结果: {conditionMet}");
            
            return conditionMet;
        }
        
        /// <summary>
        /// 执行能力
        /// </summary>
        public IEnumerator ExecuteAbility(AbilityConfiguration ability, Card card, Vector2Int targetPosition)
        {
            // 设置标志为true
            _isExecutingAbility = true;
            
            Debug.Log($"[冷却系统] 开始执行能力: {ability.abilityName}");
            
            // 执行能力
            yield return _abilityExecutor.ExecuteAbility(ability, card, targetPosition);
            
            // 执行能力后重置冷却
            if (ability.cooldown > 0)
            {
                string cooldownCounterId = ability.GetCooldownCounterId();
                card.SetTurnCounter(cooldownCounterId, ability.cooldown);
                Debug.Log($"[冷却系统] 能力 {ability.abilityName} 执行完毕，设置冷却为 {ability.cooldown} 回合");
            }
            
            // 能力执行完毕，重置标志
            _isExecutingAbility = false;
            
            Debug.Log($"[冷却系统] 能力 {ability.abilityName} 执行完成");
        }
        
        // 添加从CardDataSO加载能力的方法
        private void LoadAbilitiesFromCardDataSO()
        {
            CardDataSO[] cardDataSOs = Resources.LoadAll<CardDataSO>("CardData");
            Debug.Log($"从Resources/CardData加载了{cardDataSOs.Length}个卡牌数据");
            
            foreach (var cardDataSO in cardDataSOs)
            {
                if (cardDataSO.abilities != null && cardDataSO.abilities.Count > 0)
                {
                    Debug.Log($"卡牌 {cardDataSO.cardName}(ID:{cardDataSO.id}) 有 {cardDataSO.abilities.Count} 个能力");
                    foreach (var ability in cardDataSO.abilities)
                    {
                        RegisterAbility(cardDataSO.id, ability);
                    }
                }
            }
        }
        
        /// <summary>
        /// 获取条件解析器
        /// </summary>
        public AbilityConditionResolver GetConditionResolver()
        {
            return _conditionResolver;
        }
        
        /// <summary>
        /// 获取能力可作用的范围
        /// </summary>
        public List<Vector2Int> GetAbilityRange(AbilityConfiguration ability, Card card, CardManager cardManager)
        {
            // 使用范围计算器计算范围
            return _rangeCalculator.GetAbilityRange(ability, card);
        }
        
        /// <summary>
        /// 获取能力的高亮类型
        /// </summary>
        public HighlightType GetAbilityHighlightType(AbilityConfiguration ability)
        {
            if (ability == null || ability.actionSequence == null || ability.actionSequence.Count == 0)
            {
                return HighlightType.Move;
            }
            
            // 检查能力的动作序列
            foreach (var action in ability.actionSequence)
            {
                // 如果包含攻击动作，使用攻击高亮
                if (action.actionType == AbilityActionConfig.ActionType.Attack)
                {
                    return HighlightType.Attack;
                }
            }
            
            // 默认使用移动高亮
            return HighlightType.Move;
        }

        /// <summary>
        /// 在指定回合阶段触发自动能力
        /// </summary>
        private void TriggerAutomaticAbilitiesAtPhase(int playerId, FSM.TurnState.TurnPhase phase)
        {
            Debug.Log($"触发回合阶段 {phase} 的自动能力，玩家ID: {playerId}");
            
            // 获取所有卡牌
            Dictionary<Vector2Int, Card> allCards = _cardManager.GetAllCards();
            
            // 遍历所有卡牌
            foreach (var kvp in allCards)
            {
                Card card = kvp.Value;
                
                // 只处理当前回合玩家的卡牌，并且只处理正面的卡牌
                if (card.OwnerId != playerId || card.IsFaceDown)
                    continue;
                    
                // 获取卡牌的所有能力
                List<AbilityConfiguration> abilities = GetCardAbilities(card);
                
                foreach (var ability in abilities)
                {
                    // 检查能力是否为自动触发，且触发阶段匹配
                    if (ability.IsAutomatic() && ability.triggerPhase == phase)
                    {
                        // 检查能力是否可以触发
                        if (CanTriggerAbility(ability, card, card.Position, _cardManager))
                        {
                            Debug.Log($"自动触发卡牌 {card.Data.Name} 的能力 {ability.abilityName}");
                            
                            // 执行能力
                            StartCoroutine(ExecuteAbility(ability, card, card.Position));
                        }
                        else
                        {
                            Debug.Log($"卡牌 {card.Data.Name} 的能力 {ability.abilityName} 不满足触发条件");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 初始化卡牌的能力冷却
        /// </summary>
        public void InitializeCardAbilityCooldowns(Card card)
        {
            if (card == null || card.IsFaceDown)
                return;
            
            Debug.Log($"[冷却系统] 初始化卡牌 {card.Data.Name} 的能力冷却");
            
            foreach (var ability in GetCardAbilities(card))
            {
                if (ability.cooldown > 0)
                {
                    string cooldownCounterId = ability.GetCooldownCounterId();
                    
                    // 设置初始冷却值为配置的冷却值
                    card.SetTurnCounter(cooldownCounterId, ability.cooldown);
                    
                    Debug.Log($"[冷却系统] 卡牌 {card.Data.Name} 初始化能力 {ability.abilityName} 的冷却为 {ability.cooldown}");
                }
            }
        }

        /// <summary>
        /// 初始化所有卡牌的能力冷却
        /// </summary>
        private void InitializeAllCardsCooldowns()
        {
            Debug.Log("[冷却系统] 初始化所有卡牌的能力冷却");
            
            Dictionary<Vector2Int, Card> allCards = _cardManager.GetAllCards();
            foreach (var kvp in allCards)
            {
                Card card = kvp.Value;
                if (!card.IsFaceDown)
                {
                    InitializeCardAbilityCooldowns(card);
                }
            }
        }
    }
} 