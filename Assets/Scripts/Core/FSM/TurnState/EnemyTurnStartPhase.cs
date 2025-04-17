using UnityEngine;
using System.Collections;
using ChessGame.Utils;

namespace ChessGame.FSM.TurnState
{
    /// <summary>
    /// 敌方回合开始阶段
    /// </summary>
    public class EnemyTurnStartPhase : TurnStateBase
    {
        private Coroutine _phaseCoroutine;
        
        public EnemyTurnStartPhase(TurnStateMachine stateMachine) : base(stateMachine) { }
        
        public override void Enter()
        {
            Debug.Log("进入敌方回合开始阶段");
            
            // 通知游戏事件系统
            GameEventSystem.Instance.NotifyTurnStarted(1);
            
            // 重置所有敌方卡牌的行动状态
            StateMachine.GetCardManager().ResetEnemyCardActions();
            
            // 触发"回合开始"能力
            TriggerTurnStartAbilities();
            
            // 启动协程，延迟进入主要阶段
            _phaseCoroutine = CoroutineManager.Instance.StartCoroutineEx(DelayedCompletePhase());
        }
        
        public override void Exit()
        {
            Debug.Log("退出敌方回合开始阶段");
            
            // 如果协程还在运行，停止它
            if (_phaseCoroutine != null)
            {
                CoroutineManager.Instance.StopCoroutineEx(_phaseCoroutine);
                _phaseCoroutine = null;
            }
        }
        
        public override void Update()
        {
            // 回合开始阶段不需要Update逻辑
        }
        
        // 触发回合开始能力
        private void TriggerTurnStartAbilities()
        {
            Debug.Log("触发敌方回合开始能力");
            
            // 这里可以添加触发回合开始能力的代码
            // 例如遍历所有敌方卡牌，检查是否有回合开始触发的能力
        }
        
        // 延迟完成阶段的协程
        private IEnumerator DelayedCompletePhase()
        {
            // 等待一小段时间，让玩家看到回合开始的提示
            yield return new WaitForSeconds(0.5f);
            
            // 完成当前阶段，进入主要阶段
            CompletePhase(TurnPhase.EnemyMainPhase);
        }
    }
} 