using System.Collections;
using UnityEngine;
using ChessGame.FSM.TurnState;

namespace ChessGame.Cards
{
    /// <summary>
    /// 能力执行系统 - 负责执行能力
    /// </summary>
    public class AbilityExecutionSystem : IAbilityExecutionSystem
    {
        private AbilityExecutor _abilityExecutor;
        private AbilityConditionResolver _conditionResolver;
        private TurnManager _turnManager;
        private GameEventSystem _gameEventSystem;
        private CardManager _cardManager;
        
        // 标志是否正在执行能力
        private bool _isExecutingAbility = false;
        
        /// <summary>
        /// 获取当前是否正在执行能力
        /// </summary>
        public bool IsExecutingAbility => _isExecutingAbility;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public AbilityExecutionSystem(
            AbilityExecutor abilityExecutor,
            AbilityConditionResolver conditionResolver,
            TurnManager turnManager,
            GameEventSystem gameEventSystem,
            CardManager cardManager)
        {
            _abilityExecutor = abilityExecutor;
            _conditionResolver = conditionResolver;
            _turnManager = turnManager;
            _gameEventSystem = gameEventSystem;
            _cardManager = cardManager;
        }
        
        /// <summary>
        /// 检查能力是否可以执行
        /// </summary>
        public bool CanExecuteAbility(AbilityConfiguration ability, Card card, Vector2Int position)
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
            bool conditionMet = _conditionResolver.ResolveCondition(ability.triggerCondition, card, position, _cardManager);
            Debug.Log($"[冷却系统] 能力 {ability.abilityName} 条件检查结果: {conditionMet}");
            
            return conditionMet;
        }
        
        /// <summary>
        /// 执行能力
        /// </summary>
        public IEnumerator ExecuteAbility(AbilityConfiguration ability, Card card, Vector2Int targetPosition, bool isAutomatic = false)
        {
            // 设置标志为true
            _isExecutingAbility = true;
            
            Debug.Log($"[冷却系统] 开始执行能力: {ability.abilityName}, 是否自动触发: {isAutomatic}");
            
            // 如果是自动触发能力，触发开始事件
            if (isAutomatic && _gameEventSystem != null)
            {
                _gameEventSystem.NotifyAutomaticAbilityStart(targetPosition);
            }
            
            // 执行能力
            yield return _abilityExecutor.ExecuteAbility(ability, card, targetPosition);
            
            // 如果是自动触发能力，触发结束事件
            if (isAutomatic && _gameEventSystem != null)
            {
                _gameEventSystem.NotifyAutomaticAbilityEnd(targetPosition);
            }
            
            // 执行能力后重置冷却
            if (ability.cooldown > 0)
            {
                string cooldownCounterId = ability.GetCooldownCounterId();
                card.SetTurnCounter(cooldownCounterId, ability.cooldown);
                Debug.Log($"[冷却系统] 能力 {ability.abilityName} 执行完毕，设置冷却为 {ability.cooldown} 回合");
            }
            
            // 如果不是自动触发的能力，且在主要阶段，则标记卡牌已行动
            if (!isAutomatic && _turnManager.GetTurnStateMachine().GetCurrentPhase() == TurnPhase.PlayerMainPhase)
            {
                card.HasActed = true;
                Debug.Log($"卡牌 {card.Data.Name} 使用了主动能力，标记为已行动");
            }
            else
            {
                Debug.Log($"卡牌 {card.Data.Name} 触发了自动能力，不标记为已行动");
            }
            
            // 能力执行完毕，重置标志
            _isExecutingAbility = false;
            
            Debug.Log($"[冷却系统] 能力 {ability.abilityName} 执行完成");
        }
    }
} 