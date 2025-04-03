using UnityEngine;
using System.Collections;
using ChessGame.Utils;

namespace ChessGame.FSM.TurnState
{
    /// <summary>
    /// 敌方主要行动阶段
    /// </summary>
    public class EnemyMainPhase : TurnStateBase
    {
        private Coroutine _aiCoroutine;
        
        public EnemyMainPhase(TurnStateMachine stateMachine) : base(stateMachine) { }
        
        public override void Enter()
        {
            Debug.Log("进入敌方主要行动阶段");
            
            // 启动AI行动协程
            _aiCoroutine = CoroutineManager.Instance.StartCoroutineEx(ExecuteAITurn());
        }
        
        public override void Exit()
        {
            Debug.Log("退出敌方主要行动阶段");
            
            // 如果AI协程还在运行，停止它
            if (_aiCoroutine != null)
            {
                CoroutineManager.Instance.StopCoroutineEx(_aiCoroutine);
                _aiCoroutine = null;
            }
        }
        
        public override void Update()
        {
            // 敌方主要阶段不需要Update逻辑，AI行动由协程控制
        }
        
        // 执行AI回合
        private IEnumerator ExecuteAITurn()
        {
            Debug.Log("AI开始执行回合");
            
            // 获取AI控制器
            AIController aiController = GameObject.FindObjectOfType<AIController>();
            
            if (aiController != null)
            {
                // 执行AI回合
                yield return aiController.ExecuteAITurn();
            }
            else
            {
                Debug.LogError("找不到AIController，无法执行AI回合");
                yield return new WaitForSeconds(1.0f);
            }
            
            Debug.Log("AI回合执行完毕");
            
            // 完成当前阶段，进入回合结束阶段
            CompletePhase(TurnPhase.EnemyTurnEnd);
        }
    }
} 