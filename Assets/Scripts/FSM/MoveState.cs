using UnityEngine;

namespace ChessGame.FSM
{
    // 移动状态
    public class MoveState : CardStateBase
    {
        public MoveState(CardStateMachine stateMachine) : base(stateMachine) { }
        
        public override void Enter()
        {
            Debug.Log("进入移动状态");
            
            // 执行移动
            bool success = StateMachine.CardManager.ExecuteMove();
            Debug.Log($"移动执行结果: {(success ? "成功" : "失败")}");
            
            // 通知状态机移动已完成，应该切换到空闲状态
            CompleteState(CardState.Idle);
        }
        
        public override void Exit()
        {
            Debug.Log("退出移动状态");
        }
        
        public override void HandleCellClick(Vector2Int position)
        {
            Debug.Log("移动状态下不处理点击");
        }
        
        public override void HandleCardClick(Vector2Int position)
        {
            Debug.Log("移动状态下不处理点击");
        }
        
        public override void Update()
        {
            // 如果有移动动画，可以在这里检查动画是否完成
            // 完成后调用 CompleteState(CardState.Idle)
        }
    }
} 