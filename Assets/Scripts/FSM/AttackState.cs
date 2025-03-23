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
            
            // 执行攻击
            bool success = StateMachine.CardManager.ExecuteAttack();
            
            Debug.Log($"攻击执行结果: {(success ? "成功" : "失败")}");
            
            // 检查是否需要结束回合
            CheckEndTurn();
            
            // 完成状态，返回空闲状态
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
            // 获取回合管理器
            TurnManager turnManager = StateMachine.CardManager.GetTurnManager();
            if (turnManager == null) return;
            
            // 检查是否所有玩家卡牌都已行动
            bool allCardsActed = true;
            Dictionary<Vector2Int, Card> allCards = StateMachine.CardManager.GetAllCards();
            
            Debug.Log("检查是否所有玩家卡牌都已行动:");
            foreach (var cardPair in allCards)
            {
                Card card = cardPair.Value;
                if (card.OwnerId == 0 && !card.HasActed && !card.IsFaceDown)
                {
                    Debug.Log($"发现未行动的玩家卡牌: 位置 {card.Position}, 名称 {card.Data.Name}");
                    allCardsActed = false;
                    break;
                }
            }
            
            // 如果所有卡牌都已行动，结束回合
            if (allCardsActed && turnManager.IsPlayerTurn())
            {
                Debug.Log("所有玩家卡牌都已行动，自动结束回合");
                turnManager.EndPlayerTurn();
            }
            else
            {
                Debug.Log($"不结束回合: allCardsActed={allCardsActed}, IsPlayerTurn={turnManager.IsPlayerTurn()}");
            }
        }
    }
} 