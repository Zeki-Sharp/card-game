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
            
            // 清除所有选中状态
            StateMachine.CardManager.ClearSelectedPosition(); // 触发 OnCardDeselected
        }
        
        public override void Exit()
        {
            Debug.Log("退出空闲状态");
        }
        
        public override void HandleCellClick(Vector2Int position)
        {
            Debug.Log($"IdleState.HandleCellClick: 位置 {position}");
            // 空闲状态下点击空白格子不做任何操作
        }
        
        public override void HandleCardClick(Vector2Int position)
        {
            Debug.Log($"IdleState.HandleCardClick: 位置 {position}");
            
            // 检查是否是玩家的卡牌
            Card card = StateMachine.CardManager.GetCard(position);
            if (card != null)
            {
                Debug.Log($"点击的卡牌: {card.Data.Name}, OwnerId: {card.OwnerId}, HasActed: {card.HasActed}, IsFaceDown: {card.IsFaceDown}");
            }
            
            // 获取TurnManager
            TurnManager turnManager = StateMachine.CardManager.GetTurnManager();
            
            // 检查是否是玩家回合
            if (turnManager != null && !turnManager.IsPlayerTurn())
            {
                Debug.Log("现在是敌方回合，玩家不能行动");
                return;
            }
            
            // 只有在玩家回合才能选择玩家的正面卡牌
            if (card != null && card.OwnerId == 0 && !card.HasActed && !card.IsFaceDown)
            {
                Debug.Log($"选中卡牌: {card.Data.Name}");
                // 选中卡牌
                StateMachine.CardManager.SelectCard(position);
                // 通知状态机切换到选中状态
                CompleteState(CardState.Selected);
            }
            else if (card != null && card.OwnerId == 0 && card.IsFaceDown)
            {
                Debug.Log("卡牌是背面状态，不能选择");
            }
            else if (card != null && card.OwnerId == 0 && card.HasActed)
            {
                Debug.Log("卡牌已经行动过，不能选择");
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