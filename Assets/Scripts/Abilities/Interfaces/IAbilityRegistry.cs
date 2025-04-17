using System.Collections.Generic;
using UnityEngine;

namespace ChessGame.Cards
{
    /// <summary>
    /// 能力注册表接口 - 负责管理能力配置
    /// </summary>
    public interface IAbilityRegistry
    {
        /// <summary>
        /// 注册能力
        /// </summary>
        void RegisterAbility(int cardTypeId, AbilityConfiguration ability);
        
        /// <summary>
        /// 获取卡牌类型的所有能力
        /// </summary>
        List<AbilityConfiguration> GetCardAbilities(int cardTypeId);
        
        /// <summary>
        /// 获取卡牌的所有能力
        /// </summary>
        List<AbilityConfiguration> GetCardAbilities(Card card);
        
        /// <summary>
        /// 从CardDataSO加载能力
        /// </summary>
        void LoadAbilitiesFromCardDataSO();
    }
} 