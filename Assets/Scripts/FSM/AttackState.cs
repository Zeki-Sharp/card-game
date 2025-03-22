using UnityEngine;
using System;
namespace ChessGame.FSM
{
    // 攻击状态
    public class AttackState : CardStateBase
    {
        public AttackState(CardStateMachine stateMachine) : base(stateMachine) { }
        
        public override void Enter()
        {
            Debug.Log("进入攻击状态");
            
            // 执行攻击
            bool success = StateMachine.CardManager.ExecuteAttack();
            
            Debug.Log($"攻击执行结果: {(success ? "成功" : "失败")}");
            
            // 无论成功与否，都切换回空闲状态
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
    }
} 