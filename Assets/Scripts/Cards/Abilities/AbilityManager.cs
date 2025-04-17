using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ChessGame.FSM.TurnState;

namespace ChessGame.Cards
{
    /// <summary>
    /// 能力管理器 - 负责协调各个能力子系统
    /// </summary>
    public class AbilityManager : MonoBehaviour
    {
        private static AbilityManager _instance;
        public static AbilityManager Instance => _instance;
        
        // 子系统
        private IAbilityRegistry _abilityRegistry;
        private IAbilityTriggerSystem _triggerSystem;
        private IAbilityRangeService _rangeService;
        private IAbilityExecutionSystem _executionSystem;
        private IAbilityCooldownManager _cooldownManager;
        
        // 依赖的其他系统
        private CardManager _cardManager;
        private GameEventSystem _gameEventSystem;
        private TurnManager _turnManager;
        
        // 公共属性，提供对子系统的访问
        public IAbilityRegistry AbilityRegistry => _abilityRegistry;
        public IAbilityRangeService RangeService => _rangeService;
        public IAbilityExecutionSystem ExecutionSystem => _executionSystem;
        public IAbilityCooldownManager CooldownManager => _cooldownManager;
        
        // 提供对执行状态的访问 - 修改为静态属性
        public static bool IsExecutingAbility => Instance != null && Instance._executionSystem != null && Instance._executionSystem.IsExecutingAbility;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            // 获取依赖的系统
            _cardManager = FindObjectOfType<CardManager>();
            _gameEventSystem = GameEventSystem.Instance;
            _turnManager = FindObjectOfType<TurnManager>();
            
            // 初始化辅助类
            AbilityExecutor abilityExecutor = new AbilityExecutor(_cardManager);
            AbilityConditionResolver conditionResolver = new AbilityConditionResolver();
            AbilityRangeCalculator rangeCalculator = new AbilityRangeCalculator(_cardManager, conditionResolver);
            
            // 初始化子系统
            _abilityRegistry = new AbilityRegistry();
            _rangeService = new AbilityRangeService(rangeCalculator);
            _executionSystem = new AbilityExecutionSystem(
                abilityExecutor, 
                conditionResolver, 
                _turnManager, 
                _gameEventSystem, 
                _cardManager);
            _cooldownManager = new AbilityCooldownManager(_abilityRegistry, _cardManager);
            _triggerSystem = new AbilityTriggerSystem(_abilityRegistry, _executionSystem, _cardManager);
            
            DontDestroyOnLoad(gameObject);
            
            Debug.Log("AbilityManager初始化完成，时间: " + Time.time);
        }
        
        private void Start()
        {
            // 订阅事件
            _gameEventSystem.OnTurnStarted += HandleTurnStarted;
            _gameEventSystem.OnTurnEnded += HandleTurnEnded;
            _gameEventSystem.OnCardFlipped += HandleCardFlipped;
            _gameEventSystem.OnCardAdded += HandleCardAdded;
            
            // 加载能力配置
            (_abilityRegistry as AbilityRegistry).LoadAbilitiesFromCardDataSO();
            
            // 初始化所有已存在卡牌的能力冷却
            _cooldownManager.InitializeAllCardsCooldowns();
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
            
            // 委托给触发系统处理
            _triggerSystem.HandleTurnStarted(playerId);
        }
        
        /// <summary>
        /// 处理回合结束事件
        /// </summary>
        private void HandleTurnEnded(int playerId)
        {
            Debug.Log($"AbilityManager: 处理回合结束事件，玩家ID: {playerId}");
            
            // 委托给触发系统处理
            _triggerSystem.HandleTurnEnded(playerId);
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
                    _cooldownManager.InitializeCardAbilityCooldowns(card);
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
                    _cooldownManager.InitializeCardAbilityCooldowns(card);
                }
            }
        }
        
        /// <summary>
        /// 获取卡牌可用的能力
        /// </summary>
        public List<AbilityConfiguration> GetCardAbilities(Card card)
        {
            return _abilityRegistry.GetCardAbilities(card);
        }
        
        /// <summary>
        /// 检查能力是否可以触发
        /// </summary>
        public bool CanTriggerAbility(AbilityConfiguration ability, Card card, Vector2Int position)
        {
            return _executionSystem.CanExecuteAbility(ability, card, position);
        }
        
        /// <summary>
        /// 检查能力是否可以触发 - 重载方法，适配现有调用
        /// </summary>
        public bool CanTriggerAbility(AbilityConfiguration ability, Card card, Vector2Int position, CardManager cardManager)
        {
            // 调用原有方法
            return CanTriggerAbility(ability, card, position);
        }
        
        /// <summary>
        /// 执行能力
        /// </summary>
        public IEnumerator ExecuteAbility(AbilityConfiguration ability, Card card, Vector2Int targetPosition, bool isAutomatic = false)
        {
            return _executionSystem.ExecuteAbility(ability, card, targetPosition, isAutomatic);
        }
        
        /// <summary>
        /// 获取能力可作用的范围
        /// </summary>
        public List<Vector2Int> GetAbilityRange(AbilityConfiguration ability, Card card)
        {
            // 使用默认的目标位置（卡牌自身位置）
            return _rangeService.GetAbilityRange(ability, card, card.Position);
        }
        
        /// <summary>
        /// 获取能力可作用的范围 - 重载方法，适配现有调用
        /// </summary>
        public List<Vector2Int> GetAbilityRange(AbilityConfiguration ability, Card card, CardManager cardManager)
        {
            // 使用卡牌管理器中选中的位置作为目标位置
            Vector2Int targetPosition = cardManager.GetSelectedPosition();
            return _rangeService.GetAbilityRange(ability, card, targetPosition);
        }
        
        /// <summary>
        /// 获取能力可作用的范围 - 重载方法，适配现有调用
        /// </summary>
        public List<Vector2Int> GetAbilityRange(AbilityConfiguration ability, Card card, Vector2Int targetPosition)
        {
            return _rangeService.GetAbilityRange(ability, card, targetPosition);
        }
    }
} 