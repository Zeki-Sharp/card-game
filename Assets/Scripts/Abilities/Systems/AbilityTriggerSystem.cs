using UnityEngine;
using System.Collections.Generic;
using ChessGame.FSM.TurnState;

namespace ChessGame.Cards
{
    /// <summary>
    /// 能力触发系统 - 负责处理能力的触发
    /// </summary>
    public class AbilityTriggerSystem : IAbilityTriggerSystem
    {
        private IAbilityRegistry _abilityRegistry;
        private IAbilityExecutionSystem _executionSystem;
        private CardManager _cardManager;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public AbilityTriggerSystem(
            IAbilityRegistry abilityRegistry,
            IAbilityExecutionSystem executionSystem,
            CardManager cardManager)
        {
            _abilityRegistry = abilityRegistry;
            _executionSystem = executionSystem;
            _cardManager = cardManager;
        }
        
        /// <summary>
        /// 在指定回合阶段触发自动能力
        /// </summary>
        public void TriggerAutomaticAbilitiesAtPhase(int playerId, TurnPhase phase)
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
                List<AbilityConfiguration> abilities = _abilityRegistry.GetCardAbilities(card);
                
                foreach (var ability in abilities)
                {
                    // 检查能力是否为自动触发，且触发阶段匹配
                    if (ability.IsAutomatic() && ability.triggerPhase == phase)
                    {
                        // 检查能力是否可以触发
                        if (_executionSystem.CanExecuteAbility(ability, card, card.Position))
                        {
                            Debug.Log($"自动触发卡牌 {card.Data.Name} 的能力 {ability.abilityName}");
                            
                            // 执行能力，标记为自动触发
                            MonoBehaviour mono = _cardManager as MonoBehaviour;
                            if (mono != null)
                            {
                                mono.StartCoroutine(_executionSystem.ExecuteAbility(ability, card, card.Position, true));
                            }
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
        /// 处理回合开始事件
        /// </summary>
        public void HandleTurnStarted(int playerId)
        {
            Debug.Log($"处理回合开始事件，玩家ID: {playerId}");
            
            // 触发回合开始阶段的自动能力
            TriggerAutomaticAbilitiesAtPhase(playerId, TurnPhase.PlayerTurnStart);
        }
        
        /// <summary>
        /// 处理回合结束事件
        /// </summary>
        public void HandleTurnEnded(int playerId)
        {
            Debug.Log($"处理回合结束事件，玩家ID: {playerId}");
            
            // 触发回合结束阶段的自动能力
            TriggerAutomaticAbilitiesAtPhase(playerId, TurnPhase.PlayerTurnEnd);
        }
    }
} 