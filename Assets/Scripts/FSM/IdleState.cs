using UnityEngine;
using System;

namespace ChessGame.FSM
{
    // 空闲状态
    public class IdleState : CardStateBase
    {
        public IdleState(CardStateMachine stateMachine) : base(stateMachine) { }
        
        public override void Enter()
        {
            Debug.Log("进入空闲状态");
            // 清除所有高亮
            StateMachine.CardManager.ClearAllHighlights();
        }
        
        public override void Exit()
        {
            Debug.Log("退出空闲状态");
        }
        
        public override void HandleCellClick(Vector2Int position)
        {
            Debug.Log($"IdleState.HandleCellClick: 位置 {position}");
            // 空闲状态下点击空格子，不做任何操作
        }
        
        public override void HandleCardClick(Vector2Int position)
        {
            Debug.Log($"IdleState.HandleCardClick: 位置 {position}");
            
            // 检查是否是玩家的卡牌
            Card card = StateMachine.CardManager.GetCard(position);
            if (card != null)
            {
                Debug.Log($"点击的卡牌: {card.Data.Name}, OwnerId: {card.OwnerId}, HasActed: {card.HasActed}");
            }
            
            if (card != null && card.OwnerId == 0 && !card.HasActed)
            {
                Debug.Log($"选中卡牌: {card.Data.Name}");
                // 选中卡牌
                StateMachine.CardManager.SelectCard(position);
                // 通知状态机切换到选中状态
                CompleteState(CardState.Selected);
            }
            else
            {
                Debug.Log("卡牌不可选择");
            }
        }
        
        public override void Update()
        {
            // 空闲状态下不需要特殊更新
        }
    }
} 