using System;

namespace ChessGame.FSM.TurnState
{
    /// <summary>
    /// 回合状态接口 - 定义回合状态的基本行为
    /// </summary>
    public interface ITurnState
    {
        void Enter();
        void Exit();
        void Update();
        
        // 阶段完成事件
        event Action<TurnPhase> PhaseCompleted;
    }
} 