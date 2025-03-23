using UnityEngine;
using System;

namespace ChessGame.FSM
{
    // 选中状态
    public class SelectedState : CardStateBase
    {
        public SelectedState(CardStateMachine stateMachine) : base(stateMachine) { }
        
        public override void Enter()
        {
            Debug.Log("进入选中状态");
            
            // 获取选中的卡牌
            Card selectedCard = StateMachine.CardManager.GetSelectedCard();
            if (selectedCard == null)
            {
                Debug.LogError("进入选中状态，但没有选中的卡牌");
                CompleteState(CardState.Idle);
                return;
            }
            
            Debug.Log($"选中的卡牌: {selectedCard.Data.Name}, 位置: {selectedCard.Position}");
            
            // 获取卡牌视图并高亮
            CardView cardView = StateMachine.CardManager.GetCardView(selectedCard.Position);
            if (cardView != null)
            {
                cardView.SetSelected(true);
                Debug.Log("卡牌视图已高亮");
            }
            else
            {
                Debug.LogWarning("找不到卡牌视图");
            }
            
            // 高亮选中棋子的位置
            StateMachine.CardManager.HighlightSelectedPosition(selectedCard);
            
            // 高亮可移动的格子
            StateMachine.CardManager.HighlightMovablePositions(selectedCard);
            
            // 高亮可攻击的敌方棋子
            StateMachine.CardManager.HighlightAttackableCards(selectedCard);
        }
        
        public override void Exit()
        {
            Debug.Log("退出选中状态");
            // 清除高亮
            StateMachine.CardManager.ClearAllHighlights();
        }
        
        public override void HandleCellClick(Vector2Int position)
        {
            Debug.Log($"SelectedState.HandleCellClick: 位置 {position}");
            
            // 检查是否是可移动的位置
            if (StateMachine.CardManager.CanMoveTo(position))
            {
                Debug.Log($"移动到位置: {position}");
                // 设置目标位置
                StateMachine.CardManager.SetTargetPosition(position);
                // 切换到移动状态
                Debug.Log("切换到移动状态");
                CompleteState(CardState.Moving);
            }
            else
            {
                Debug.Log("取消选择");
                // 取消选择
                CompleteState(CardState.Idle);
            }
        }
        
        public override void HandleCardClick(Vector2Int position)
        {
            Debug.Log($"SelectedState.HandleCardClick: 位置 {position}");
            
            Card selectedCard = StateMachine.CardManager.GetSelectedCard();
            Card targetCard = StateMachine.CardManager.GetCard(position);
            
            if (selectedCard == null)
            {
                Debug.LogError("选中状态下，但没有选中的卡牌");
                CompleteState(CardState.Idle);
                return;
            }
            
            // 如果点击的是已选中的卡牌，取消选择
            if (selectedCard.Position == position)
            {
                Debug.Log("点击了已选中的卡牌，取消选择");
                CompleteState(CardState.Idle);
                return;
            }
            
            // 如果点击的是背面卡牌，且在攻击范围内
            if (targetCard != null && targetCard.IsFaceDown && StateMachine.CardManager.CanAttack(position))
            {
                Debug.Log($"攻击背面卡牌");
                StateMachine.CardManager.SetTargetPosition(position);
                Debug.Log("切换到攻击状态");
                CompleteState(CardState.Attacking);
            }
            // 如果点击的是敌方卡牌，且在攻击范围内
            else if (targetCard != null && !targetCard.IsFaceDown && targetCard.OwnerId != selectedCard.OwnerId && StateMachine.CardManager.CanAttack(position))
            {
                Debug.Log($"攻击敌方卡牌: {targetCard.Data.Name}");
                StateMachine.CardManager.SetTargetPosition(position);
                Debug.Log("切换到攻击状态");
                CompleteState(CardState.Attacking);
            }
            // 如果点击的是己方其他卡牌
            else if (targetCard != null && !targetCard.IsFaceDown && targetCard.OwnerId == selectedCard.OwnerId && !targetCard.HasActed)
            {
                Debug.Log($"选择新卡牌: {targetCard.Data.Name}");
                // 选择新卡牌
                StateMachine.CardManager.SelectCard(position);
                CompleteState(CardState.Selected);
            }
            else
            {
                Debug.Log("取消选择");
                // 取消选择
                CompleteState(CardState.Idle);
            }
        }
        
        public override void Update()
        {
            // 选中状态下不需要特殊更新
        }
    }
} 