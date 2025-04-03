using UnityEngine;
using System.Collections;
using ChessGame.Utils;

namespace ChessGame.FSM.TurnState
{
    /// <summary>
    /// 玩家回合结束阶段
    /// </summary>
    public class PlayerTurnEndPhase : TurnStateBase
    {
        private Coroutine _phaseCoroutine;
        
        public PlayerTurnEndPhase(TurnStateMachine stateMachine) : base(stateMachine) { }
        
        public override void Enter()
        {
            Debug.Log("进入玩家回合结束阶段");
            
            // 通知游戏事件系统
            GameEventSystem.Instance.NotifyTurnEnded(0);
            
            // 触发"回合结束"能力
            TriggerTurnEndAbilities();
            
            // 启动协程，延迟切换到敌方回合
            _phaseCoroutine = CoroutineManager.Instance.StartCoroutineEx(DelayedCompletePhase());
        }
        
        public override void Exit()
        {
            Debug.Log("退出玩家回合结束阶段");
            
            // 如果协程还在运行，停止它
            if (_phaseCoroutine != null)
            {
                CoroutineManager.Instance.StopCoroutineEx(_phaseCoroutine);
                _phaseCoroutine = null;
            }
        }
        
        public override void Update()
        {
            // 回合结束阶段不需要Update逻辑
        }
        
        // 触发回合结束能力
        private void TriggerTurnEndAbilities()
        {
            Debug.Log("触发玩家回合结束能力");
            
            // 这里可以添加触发回合结束能力的代码
            // 例如遍历所有玩家卡牌，检查是否有回合结束触发的能力
        }
        
        // 延迟完成阶段的协程
        private IEnumerator DelayedCompletePhase()
        {
            // 等待一小段时间，让玩家看到回合结束的提示
            yield return new WaitForSeconds(0.5f);
            
            // 切换到敌方回合
            StateMachine.SwitchPlayer();
        }
    }
} 