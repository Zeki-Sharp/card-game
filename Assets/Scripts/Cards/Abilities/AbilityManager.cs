using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ChessGame;

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
        
        public enum HighlightType
        {
            Move,
            Attack,
            Ability
        }

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
            
            // 设置脚本执行顺序
            DontDestroyOnLoad(gameObject);
            
            Debug.Log("AbilityManager初始化完成，时间: " + Time.time);
        }
        
        private void Start()
        {
            // 查找并保存 CardManager 引用
            _cardManager = FindObjectOfType<CardManager>();
            if (_cardManager == null)
            {
                Debug.LogError("无法找到CardManager");
                return;
            }
            
            _abilityExecutor = new AbilityExecutor(_cardManager);
            
            // 加载能力配置
            LoadAbilityConfigurations();
            
            // 从CardDataSO加载能力
            LoadAbilitiesFromCardDataSO();
            
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
        public bool CanTriggerAbility(AbilityConfiguration ability, Card card, Vector2Int targetPosition, CardManager cardManager)
        {
            return _conditionResolver.CheckCondition(ability.triggerCondition, card, targetPosition, cardManager);
        }
        
        /// <summary>
        /// 执行能力
        /// </summary>
        public IEnumerator ExecuteAbility(AbilityConfiguration ability, Card card, Vector2Int targetPosition)
        {
            Debug.Log($"AbilityManager.ExecuteAbility: 开始执行能力 {ability.abilityName}");
            
            // 确保卡牌管理器存在
            if (_cardManager == null)
            {
                Debug.LogError("执行能力失败: 卡牌管理器为空");
                yield break;
            }
            
            // 执行能力前记录卡牌位置
            Vector2Int originalPosition = card.Position;
            
            // 执行能力
            yield return _abilityExecutor.ExecuteAbility(ability, card, targetPosition);
            
            // 设置冷却
            _conditionResolver.SetAbilityCooldown(card.Data.Id, ability.abilityName, ability.cooldown);
            
            // 标记卡牌已行动
            card.HasActed = true;
            
            Debug.Log($"AbilityManager.ExecuteAbility: 能力 {ability.abilityName} 执行完成");
            
            // 通知游戏事件系统能力执行完成
            if (GameEventSystem.Instance != null)
            {
                GameEventSystem.Instance.NotifyCardActed(card.Position);
            }
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
            List<Vector2Int> validPositions = new List<Vector2Int>();
            
            // 遍历棋盘上所有位置
            for (int x = 0; x < cardManager.BoardWidth; x++)
            {
                for (int y = 0; y < cardManager.BoardHeight; y++)
                {
                    Vector2Int targetPos = new Vector2Int(x, y);
                    
                    // 跳过源位置
                    if (targetPos == card.Position)
                        continue;
                    
                    // 检查能力是否可以在该位置触发
                    if (_conditionResolver.CheckCondition(ability.triggerCondition, card, targetPos, cardManager))
                    {
                        validPositions.Add(targetPos);
                    }
                }
            }
            
            return validPositions;
        }
        
        /// <summary>
        /// 获取能力的高亮类型
        /// </summary>
        public HighlightType GetAbilityHighlightType(AbilityConfiguration ability)
        {
            if (ability == null)
            {
                Debug.LogError("【能力管理器】传入的能力为空");
                return HighlightType.Move;
            }
            
            Debug.Log($"【能力管理器】获取能力 {ability.abilityName} 的高亮类型");
            
            // 检查能力的动作序列是否为空
            if (ability.actionSequence == null || ability.actionSequence.Count == 0)
            {
                Debug.LogWarning($"【能力管理器】能力 {ability.abilityName} 的动作序列为空");
                return HighlightType.Move;
            }
            
            // 检查能力的动作序列
            foreach (var action in ability.actionSequence)
            {
                Debug.Log($"【能力管理器】检查动作: {action.actionType}");
                
                // 如果包含攻击动作，使用攻击高亮
                if (action.actionType == AbilityActionConfig.ActionType.Attack)
                {
                    Debug.Log($"【能力管理器】能力 {ability.abilityName} 包含攻击动作，使用攻击高亮");
                    return HighlightType.Attack;
                }
            }
            
            Debug.Log($"【能力管理器】能力 {ability.abilityName} 不包含攻击动作，使用移动高亮");
            // 默认使用移动高亮
            return HighlightType.Move;
        }
    }
} 