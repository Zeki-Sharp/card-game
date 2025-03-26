using UnityEngine;
using System.Collections.Generic;
using ChessGame;

namespace ChessGame.FSM
{
    // 移动状态
    public class MoveState : CardStateBase
    {
        public MoveState(CardStateMachine stateMachine) : base(stateMachine) { }
        
        public override void Enter()
        {
            Debug.Log("进入移动状态");
            
            // 获取选中的卡牌和目标位置
            Vector2Int? selectedPosition = StateMachine.CardManager.GetSelectedPosition();
            Vector2Int? targetPosition = StateMachine.CardManager.GetTargetPosition();
            
            if (!selectedPosition.HasValue || !targetPosition.HasValue)
            {
                Debug.LogError("移动状态：没有选中的卡牌或目标位置");
                CompleteState(CardState.Idle);
                return;
            }
            
            // 直接使用CardManager的MoveCard方法
            bool success = StateMachine.CardManager.MoveCard(selectedPosition.Value, targetPosition.Value);
            
            if (success)
            {
                Debug.Log("移动成功");
            }
            else
            {
                Debug.LogWarning("移动失败");
            }
            
            // 移动完成后，转换到空闲状态
            CompleteState(CardState.Idle);
        }
        
        public override void Exit()
        {
            Debug.Log("退出移动状态");
            
            // 清除选中和目标位置
            StateMachine.CardManager.DeselectCard();
            StateMachine.CardManager.ClearTargetPosition();
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
            // 移动状态不需要Update逻辑，因为移动是立即完成的
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