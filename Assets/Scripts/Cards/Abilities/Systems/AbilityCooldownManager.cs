using UnityEngine;
using System.Collections.Generic;

namespace ChessGame.Cards
{
    /// <summary>
    /// 能力冷却管理器 - 负责管理能力冷却
    /// </summary>
    public class AbilityCooldownManager : IAbilityCooldownManager
    {
        private IAbilityRegistry _abilityRegistry;
        private CardManager _cardManager;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public AbilityCooldownManager(
            IAbilityRegistry abilityRegistry,
            CardManager cardManager)
        {
            _abilityRegistry = abilityRegistry;
            _cardManager = cardManager;
        }
        
        /// <summary>
        /// 设置能力冷却
        /// </summary>
        public void SetCooldown(Card card, AbilityConfiguration ability, int cooldown)
        {
            if (card == null || ability == null)
                return;
                
            string cooldownCounterId = ability.GetCooldownCounterId();
            card.SetTurnCounter(cooldownCounterId, cooldown);
            
            Debug.Log($"设置卡牌 {card.Data.Name} 的能力 {ability.abilityName} 冷却为 {cooldown}");
        }
        
        /// <summary>
        /// 获取当前冷却
        /// </summary>
        public int GetCurrentCooldown(Card card, AbilityConfiguration ability)
        {
            if (card == null || ability == null)
                return 0;
                
            string cooldownCounterId = ability.GetCooldownCounterId();
            return card.GetTurnCounter(cooldownCounterId);
        }
        
        /// <summary>
        /// 初始化卡牌能力冷却
        /// </summary>
        public void InitializeCardAbilityCooldowns(Card card)
        {
            if (card == null || card.Data == null)
                return;
                
            Debug.Log($"[冷却系统] 初始化卡牌 {card.Data.Name} 的能力冷却");
            
            List<AbilityConfiguration> abilities = _abilityRegistry.GetCardAbilities(card);
            
            foreach (var ability in abilities)
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
        public void InitializeAllCardsCooldowns()
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