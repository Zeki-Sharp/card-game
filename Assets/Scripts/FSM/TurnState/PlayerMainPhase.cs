using UnityEngine;

namespace ChessGame.FSM.TurnState
{
    /// <summary>
    /// 玩家主要行动阶段
    /// </summary>
    public class PlayerMainPhase : TurnStateBase
    {
        public PlayerMainPhase(TurnStateMachine stateMachine) : base(stateMachine) { }
        
        public override void Enter()
        {
            Debug.Log("进入玩家主要行动阶段");
            
            // 启用玩家输入
            EnablePlayerInput();
        }
        
        public override void Exit()
        {
            Debug.Log("退出玩家主要行动阶段");
            
            // 禁用玩家输入
            DisablePlayerInput();
        }
        
        public override void Update()
        {
            // 检查是否应该结束回合
            // 例如，如果所有卡牌都已行动，或者玩家点击了结束回合按钮
        }
        
        // 启用玩家输入
        private void EnablePlayerInput()
        {
            Debug.Log("启用玩家输入");
            
            // 这里可以添加启用玩家输入的代码
            // 例如启用卡牌状态机的输入处理
        }
        
        // 禁用玩家输入
        private void DisablePlayerInput()
        {
            Debug.Log("禁用玩家输入");
            
            // 这里可以添加禁用玩家输入的代码
            // 例如禁用卡牌状态机的输入处理
        }
        
        // 结束玩家回合
        public void EndTurn()
        {
            Debug.Log("玩家结束回合");
            
            // 完成当前阶段，进入回合结束阶段
            CompletePhase(TurnPhase.PlayerTurnEnd);
        }
    }
} 