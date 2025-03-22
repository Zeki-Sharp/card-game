using UnityEngine;
using System;

namespace ChessGame.FSM
{
    // 卡牌状态基类
    public abstract class CardStateBase : ICardState
    {
        protected CardStateMachine StateMachine;
        
        public event Action<CardState> StateCompleted;
        
        public CardStateBase(CardStateMachine stateMachine)
        {
            StateMachine = stateMachine;
        }
        
        public abstract void Enter();
        public abstract void Exit();
        public abstract void HandleCellClick(Vector2Int position);
        public abstract void HandleCardClick(Vector2Int position);
        public abstract void Update();
        
        // 通知状态机状态已完成
        protected void CompleteState(CardState nextState)
        {
            StateCompleted?.Invoke(nextState);
        }
    }
} 