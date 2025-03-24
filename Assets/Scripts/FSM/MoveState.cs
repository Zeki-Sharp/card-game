using UnityEngine;
using System.Collections.Generic;

namespace ChessGame.FSM
{
    // 移动状态
    public class MoveState : CardStateBase
    {
        public MoveState(CardStateMachine stateMachine) : base(stateMachine) { }
        
        public override void Enter()
        {
            Debug.Log("进入移动状态");

            // 获取选中的移动卡牌
            Card mover = StateMachine.CardManager.GetSelectedCard();
            if (mover == null)
            {
                Debug.LogError("没有选中的移动卡牌");
                return;
            }

            // 获取目标位置
            Vector2Int? targetPosOpt = StateMachine.CardManager.GetTargetPosition();
            if (!targetPosOpt.HasValue)
            {
                Debug.LogError("移动失败：没有目标位置");
                CompleteState(CardState.Idle);
                return;
            }

            Vector2Int targetPos = targetPosOpt.Value;

            // 检查目标位置是否合法（可选，也可以假设SelectedState已经处理）
            if (!mover.CanMoveTo(targetPos, StateMachine.CardManager.GetAllCards()))
            {
                Debug.LogWarning("目标位置不在合法移动范围内，回到Idle");
                CompleteState(CardState.Idle);
                return;
            }

            Debug.Log($"准备移动卡牌 {mover.Data.Name} 到 {targetPos}");

            Vector2Int fromPos = mover.Position;

            // 更新模型数据
            mover.Position = targetPos;
            mover.HasActed = true;

            // 直接使用移动方法
            StateMachine.CardManager.MoveCard(fromPos, targetPos);

            // 触发事件
            StateMachine.CardManager.NotifyCardMoved(fromPos, targetPos);

            Debug.Log($"卡牌 {mover.Data.Name} 移动完成：{fromPos} -> {targetPos}");

            // 调用通用回合结束检查
            CheckEndTurn();

            // 状态完成，切换回 Idle
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