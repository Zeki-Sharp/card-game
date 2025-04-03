using UnityEngine;
using System;

namespace ChessGame.FSM
{
    // 卡牌状态接口
    public interface ICardState
    {
        void Enter();
        void Exit();
        void HandleCellClick(Vector2Int position);
        void HandleCardClick(Vector2Int position);
        void Update();
        
        // 添加事件
        event Action<CardState> StateCompleted;
    }   
} 