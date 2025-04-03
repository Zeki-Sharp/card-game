using UnityEngine;
using System;
using System.Collections.Generic;

namespace ChessGame.FSM
{
    // 攻击状态
    public class AttackState : CardStateBase
    {
        public AttackState(CardStateMachine stateMachine) : base(stateMachine) { }
        
        public override void Enter()
        {
            Debug.Log("进入攻击状态");
            
            // 获取选中的卡牌和目标位置
            Vector2Int? selectedPosition = StateMachine.CardManager.GetSelectedPosition();
            Vector2Int? targetPosition = StateMachine.CardManager.GetTargetPosition();
            
            if (!selectedPosition.HasValue || !targetPosition.HasValue)
            {
                Debug.LogError("攻击状态：没有选中的卡牌或目标位置");
                CompleteState(CardState.Idle);
                return;
            }
            
            // 直接使用CardManager的AttackCard方法
            bool success = StateMachine.CardManager.AttackCard(selectedPosition.Value, targetPosition.Value);
            
            if (success)
            {
                Debug.Log("攻击成功");
                
                // 检查是否应该结束回合
                CheckEndTurn();
            }
            else
            {
                Debug.LogWarning("攻击失败");
            }
            
            // 攻击完成后，转换到空闲状态
            CompleteState(CardState.Idle);
        }
        
        public override void Exit()
        {
            Debug.Log("退出攻击状态");
        }
        
        public override void HandleCellClick(Vector2Int position)
        {
            Debug.Log("攻击状态下不处理点击");
        }
        
        public override void HandleCardClick(Vector2Int position)
        {
            Debug.Log("攻击状态下不处理点击");
        }
        
        public override void Update()
        {
            // 如果有攻击动画，可以在这里检查动画是否完成
            // 完成后调用 CompleteState(CardState.Idle)
        }
        
        // 添加检查结束回合的方法
        private void CheckEndTurn()
        {
            TurnManager turnManager = StateMachine.CardManager.GetTurnManager();
            if (turnManager == null) return;

            if (turnManager.IsPlayerTurn())
            {
                Debug.Log("任意玩家卡牌行动后立即结束回合");
                turnManager.EndPlayerTurn();
            }
        }

    }
} 