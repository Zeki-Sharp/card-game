namespace ChessGame.Cards
{
    /// <summary>
    /// 能力冷却管理器接口 - 负责管理能力冷却
    /// </summary>
    public interface IAbilityCooldownManager
    {
        /// <summary>
        /// 设置能力冷却
        /// </summary>
        void SetCooldown(Card card, AbilityConfiguration ability, int cooldown);
        
        /// <summary>
        /// 获取当前冷却
        /// </summary>
        int GetCurrentCooldown(Card card, AbilityConfiguration ability);
        
        /// <summary>
        /// 初始化卡牌能力冷却
        /// </summary>
        void InitializeCardAbilityCooldowns(Card card);
        
        /// <summary>
        /// 初始化所有卡牌的能力冷却
        /// </summary>
        void InitializeAllCardsCooldowns();
    }
} 