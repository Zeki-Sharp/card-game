using System.Collections.Generic;
using UnityEngine;

namespace ChessGame.Cards
{
    /// <summary>
    /// 能力注册表 - 负责管理能力配置
    /// </summary>
    public class AbilityRegistry : IAbilityRegistry
    {
        // 存储卡牌类型与能力配置的映射
        private Dictionary<int, List<AbilityConfiguration>> _cardTypeAbilities = new Dictionary<int, List<AbilityConfiguration>>();
        
        /// <summary>
        /// 注册能力
        /// </summary>
        public void RegisterAbility(int cardTypeId, AbilityConfiguration ability)
        {
            if (!_cardTypeAbilities.ContainsKey(cardTypeId))
            {
                _cardTypeAbilities[cardTypeId] = new List<AbilityConfiguration>();
            }
            
            _cardTypeAbilities[cardTypeId].Add(ability);
            Debug.Log($"为卡牌类型 {cardTypeId} 注册能力: {ability.abilityName}");
        }
        
        /// <summary>
        /// 获取卡牌类型的所有能力
        /// </summary>
        public List<AbilityConfiguration> GetCardAbilities(int cardTypeId)
        {
            if (_cardTypeAbilities.ContainsKey(cardTypeId))
            {
                return _cardTypeAbilities[cardTypeId];
            }
            
            return new List<AbilityConfiguration>();
        }
        
        /// <summary>
        /// 获取卡牌的所有能力
        /// </summary>
        public List<AbilityConfiguration> GetCardAbilities(Card card)
        {
            if (card == null || card.Data == null)
            {
                return new List<AbilityConfiguration>();
            }
            
            return GetCardAbilities(card.Data.Id);
        }
        
        /// <summary>
        /// 从CardDataSO加载能力
        /// </summary>
        public void LoadAbilitiesFromCardDataSO()
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
    }
} 