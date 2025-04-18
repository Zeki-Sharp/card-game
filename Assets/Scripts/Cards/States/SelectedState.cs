using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using ChessGame.Cards;
using ChessGame.Utils;

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
            
            // 高亮逻辑已移至CardHighlightService，通过事件触发
        }
        
        public override void Exit()
        {
            Debug.Log("退出选中状态");
            
            // 清除卡牌选中状态
            Card selectedCard = StateMachine.CardManager.GetSelectedCard();
            if (selectedCard != null)
            {
                CardView cardView = StateMachine.CardManager.GetCardView(selectedCard.Position);
                if (cardView != null)
                {
                    cardView.SetSelected(false);
                }
            }
            
            // 高亮清除逻辑已移至CardHighlightService，通过事件触发
        }
        
        public override void HandleCellClick(Vector2Int position)
        {
            // 获取选中的卡牌
            Vector2Int? selectedPosition = StateMachine.CardManager.GetSelectedPosition();
            if (!selectedPosition.HasValue) return;
            
            Card selectedCard = StateMachine.CardManager.GetCard(selectedPosition.Value);
            if (selectedCard == null) return;
            
            // 获取所有卡牌
            Dictionary<Vector2Int, Card> allCards = StateMachine.CardManager.GetAllCards();
            
            // 首先检查是否可以触发能力（提高能力触发的优先级）
            List<AbilityConfiguration> abilities = selectedCard.GetAbilities();
            foreach (var ability in abilities)
            {
                Debug.Log($"【状态机】检查能力: {ability.abilityName}");
                
                if (selectedCard.CanTriggerAbility(ability, position, StateMachine.CardManager))
                {
                    Debug.Log($"【状态机】能力可触发: {ability.abilityName}，切换到能力状态");
                    
                    // 设置目标位置
                    StateMachine.CardManager.SetTargetPosition(position);
                    
                    // 切换到能力状态
                    CompleteState(CardState.Ability);
                    return;
                }
            }
            
            // 然后检查是否可以攻击该位置
            if (selectedCard.CanAttack(position, allCards))
            {
                // 设置目标位置
                StateMachine.CardManager.SetTargetPosition(position);
                
                // 切换到攻击状态
                CompleteState(CardState.Attacking);
                return;
            }
            
            // 最后检查是否可以移动到该位置
            if (selectedCard.CanMoveTo(position, allCards))
            {
                // 设置目标位置
                StateMachine.CardManager.SetTargetPosition(position);
                
                // 切换到移动状态
                CompleteState(CardState.Moving);
                return;
            }
            
            // 如果点击的位置不可交互，取消选中
            StateMachine.CardManager.DeselectCard();
            CompleteState(CardState.Idle);
        }
        
        public override void HandleCardClick(Vector2Int position)
        {
            Debug.Log($"选中状态下点击卡牌: {position}");
            
            // 获取选中的卡牌
            Card selectedCard = StateMachine.CardManager.GetSelectedCard();
            if (selectedCard == null)
            {
                Debug.LogError("选中状态下没有选中的卡牌");
                CompleteState(CardState.Idle);
                return;
            }
            
            // 获取目标位置的卡牌
            Card targetCard = StateMachine.CardManager.GetCard(position);
            
            // 如果卡牌已经行动过，不能再次行动
            if (selectedCard.HasActed)
            {
                Debug.Log("卡牌已经行动过，不能再次行动");
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
            
            // 检查是否可以触发能力
            if (selectedCard.OwnerId == 0 && !selectedCard.IsFaceDown) // 只有玩家的正面卡牌可以使用能力
            {
                AbilityManager abilityManager = AbilityManager.Instance;
                if (abilityManager != null)
                {
                    List<AbilityConfiguration> abilities = abilityManager.GetCardAbilities(selectedCard);
                    foreach (var ability in abilities)
                    {
                        if (abilityManager.CanTriggerAbility(ability, selectedCard, position, StateMachine.CardManager))
                        {
                            Debug.Log($"触发能力: {ability.abilityName}");
                            StateMachine.CardManager.SetTargetPosition(position);
                            
                            // 执行能力
                            StateMachine.CardManager.StartCoroutine(abilityManager.ExecuteAbility(ability, selectedCard, position));
                            
                            // 标记卡牌已行动
                            selectedCard.HasActed = true;
                            
                            // 更新卡牌视图
                            CardView cardView = StateMachine.CardManager.GetCardView(selectedCard.Position);
                            if (cardView != null) cardView.UpdateVisuals();
                            
                            CompleteState(CardState.Idle);
                            return;
                        }
                    }
                }
            }
            
            // 如果点击的是背面卡牌，且在攻击范围内 - 直接使用Card类的方法
            if (targetCard != null && targetCard.IsFaceDown && 
                selectedCard.CanAttack(position, StateMachine.CardManager.GetAllCards()))
            {
                Debug.Log($"攻击背面卡牌");
                StateMachine.CardManager.SetTargetPosition(position);
                Debug.Log("切换到攻击状态");
                CompleteState(CardState.Attacking);
            }
            // 如果点击的是敌方卡牌，且在攻击范围内 - 直接使用Card类的方法
            else if (targetCard != null && !targetCard.IsFaceDown && targetCard.OwnerId != selectedCard.OwnerId && 
                     selectedCard.CanAttack(position, StateMachine.CardManager.GetAllCards()))
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