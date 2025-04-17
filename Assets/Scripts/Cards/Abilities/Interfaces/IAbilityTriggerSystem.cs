using ChessGame.FSM.TurnState;

namespace ChessGame.Cards
{
    /// <summary>
    /// 能力触发系统接口 - 负责处理能力的触发
    /// </summary>
    public interface IAbilityTriggerSystem
    {
        /// <summary>
        /// 在指定回合阶段触发自动能力
        /// </summary>
        void TriggerAutomaticAbilitiesAtPhase(int playerId, TurnPhase phase);
        
        /// <summary>
        /// 处理回合开始事件
        /// </summary>
        void HandleTurnStarted(int playerId);
        
        /// <summary>
        /// 处理回合结束事件
        /// </summary>
        void HandleTurnEnded(int playerId);
    }
} 