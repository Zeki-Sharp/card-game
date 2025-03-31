using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace ChessGame.Cards
{
    /// <summary>
    /// 能力管理器 - 负责管理所有卡牌的能力
    /// </summary>
    public class AbilityManager : MonoBehaviour
    {
        private static AbilityManager _instance;
        public static AbilityManager Instance => _instance;
        
        // 存储卡牌类型与能力配置的映射
        private Dictionary<int, List<AbilityConfiguration>> _cardTypeAbilities = new Dictionary<int, List<AbilityConfiguration>>();
        
        private AbilityExecutor _abilityExecutor;
        private AbilityConditionResolver _conditionResolver;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            _conditionResolver = new AbilityConditionResolver();
            
            // 在Start中初始化，确保CardManager已经存在
        }
        
        private void Start()
        {
            CardManager cardManager = FindObjectOfType<CardManager>();
            if (cardManager == null)
            {
                Debug.LogError("无法找到CardManager");
                return;
            }
            
            _abilityExecutor = new AbilityExecutor(cardManager);
            
            // 加载能力配置
            LoadAbilityConfigurations();
            
            // 订阅回合开始事件
            TurnManager turnManager = FindObjectOfType<TurnManager>();
            if (turnManager != null)
            {
                turnManager.OnTurnStarted += HandleTurnStarted;
            }
        }
        
        private void OnDestroy()
        {
            // 取消订阅事件
            TurnManager turnManager = FindObjectOfType<TurnManager>();
            if (turnManager != null)
            {
                turnManager.OnTurnStarted -= HandleTurnStarted;
            }
        }
        
        /// <summary>
        /// 处理回合开始事件
        /// </summary>
        private void HandleTurnStarted(int playerId)
        {
            _conditionResolver.ReduceCooldowns(playerId);
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
            // 例如: "Ability_1_Charge.asset" 表示ID为1的卡牌的冲锋能力
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
            if (!_cardTypeAbilities.ContainsKey(cardTypeId))
            {
                _cardTypeAbilities[cardTypeId] = new List<AbilityConfiguration>();
            }
            _cardTypeAbilities[cardTypeId].Add(ability);
        }
        
        /// <summary>
        /// 获取卡牌可用的能力
        /// </summary>
        public List<AbilityConfiguration> GetCardAbilities(Card card)
        {
            int cardTypeId = card.Data.Id;
            if (_cardTypeAbilities.TryGetValue(cardTypeId, out var abilities))
            {
                return abilities;
            }
            return new List<AbilityConfiguration>();
        }
        
        /// <summary>
        /// 检查能力是否可以触发
        /// </summary>
        public bool CanTriggerAbility(AbilityConfiguration ability, Card card, Vector2Int targetPosition, CardManager cardManager)
        {
            return _conditionResolver.CheckCondition(ability.triggerCondition, card, targetPosition, cardManager);
        }
        
        /// <summary>
        /// 执行能力
        /// </summary>
        public IEnumerator ExecuteAbility(AbilityConfiguration ability, Card card, Vector2Int targetPosition)
        {
            yield return _abilityExecutor.ExecuteAbility(ability, card, targetPosition);
            
            // 设置冷却
            _conditionResolver.SetAbilityCooldown(card.Data.Id, ability.abilityName, ability.cooldown);
        }
    }
} 