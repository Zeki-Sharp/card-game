using System;
using UnityEngine;

namespace ChessGame.FSM.TurnState
{
    /// <summary>
    /// 回合状态基类 - 实现回合状态的基本功能
    /// </summary>
    public abstract class TurnStateBase : ITurnState
    {
        protected TurnStateMachine StateMachine { get; private set; }
        
        public event Action<TurnPhase> PhaseCompleted;
        
        public TurnStateBase(TurnStateMachine stateMachine)
        {
            StateMachine = stateMachine;
        }
        
        public abstract void Enter();
        public abstract void Exit();
        public abstract void Update();
        
        // 通知状态机阶段已完成
        protected void CompletePhase(TurnPhase nextPhase)
        {
            Debug.Log($"回合阶段完成，下一阶段: {nextPhase}");
            PhaseCompleted?.Invoke(nextPhase);
        }
    }
} 