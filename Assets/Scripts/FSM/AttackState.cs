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

            Card attacker = StateMachine.CardManager.GetSelectedCard();
            Vector2Int attackerPos = attacker.Position;
            Vector2Int targetPos = StateMachine.CardManager.GetTargetPosition();
            Card target = StateMachine.CardManager.GetCard(targetPos);
            
            Debug.Log($"攻击准备就绪：攻击者 {attacker.Data.Name}，目标 {target.Data.Name}");

            if (!attacker.CanAttack(targetPos, StateMachine.CardManager.GetAllCards()))
            {
                Debug.LogWarning("进入AttackState时发现目标非法，说明前置逻辑出错");
                CompleteState(CardState.Idle); // 兜底防御
                return;
            }

            // 触发攻击动画事件
            StateMachine.CardManager.NotifyCardAttacked(attackerPos, targetPos);

            // 执行攻击
            if (target.IsFaceDown)
            {
                Debug.Log("目标是背面卡牌，翻面处理");
    
                // 翻面
                StateMachine.CardManager.FlipCard(targetPos);

                // 特殊规则：如果是友方卡牌，则翻面但不受伤
                if (target.OwnerId == attacker.OwnerId)
                {
                    Debug.Log("翻开的是我方卡牌，不执行攻击，只变为可选中");
                    CompleteState(CardState.Idle); // 回到Idle，不结束回合
                    return;
                }

                // 敌方背面卡牌翻面后，执行攻击但保证不会死亡
                Debug.Log("翻开的是敌方卡牌，执行攻击但保证不会死亡");
                
                // 记录攻击前的生命值
                int targetHpBefore = target.Data.Health;
                int attackerHpBefore = attacker.Data.Health;

                // 执行攻击（会直接修改双方血量）
                bool success = attacker.Attack(target);

                // 特殊规则：如果背面卡牌血量降至0或以下，保留1点血量
                if (target.Data.Health <= 0)
                {
                    Debug.Log($"背面卡牌 {target.Data.Name} 血量降至0或以下，保留1点血量");
                    target.Data.Health = 1;
                }

                // 触发受伤事件
                if (targetHpBefore > target.Data.Health)
                {
                    StateMachine.CardManager.NotifyCardDamaged(targetPos);
                    Debug.Log($"目标 {target.Data.Name} 受伤，当前血量: {target.Data.Health}");
                }

                // 攻击者是否死亡（通常是受到反击）
                bool attackerDead = attacker.Data.Health <= 0;
                if (attackerDead)
                {
                    Debug.Log($"攻击者 {attacker.Data.Name} 被反击致死，移除卡牌");
                    StateMachine.CardManager.RemoveCard(attacker.Position); 
                    StateMachine.CardManager.NotifyCardRemoved(attacker.Position);
                }
                else
                {
                    if (attackerHpBefore > attacker.Data.Health)
                    {
                        StateMachine.CardManager.NotifyCardDamaged(attacker.Position);
                        Debug.Log($"攻击者 {attacker.Data.Name} 受伤");
                    }
                    
                    // 标记卡牌为已行动
                    attacker.HasActed = true;
                    Debug.Log($"标记卡牌 {attacker.Data.Name} 为已行动");
                }

                // 检查是否结束回合
                CheckEndTurn();

                CompleteState(CardState.Idle);

            }
            else
            {
                // 处理正面卡牌的逻辑
                Debug.Log("目标是正面卡牌，直接执行攻击");
                
                // 记录攻击前的生命值（用于判断是否受伤）
                int targetHpBefore = target.Data.Health;
                int attackerHpBefore = attacker.Data.Health;

                // 执行攻击（会直接修改双方血量）
                bool success = attacker.Attack(target);

                bool targetDead = target.Data.Health <= 0;
                bool attackerDead = attacker.Data.Health <= 0;

                // 移除死亡的卡牌（目标优先）
                if (targetDead)
                {
                    Debug.Log($"目标 {target.Data.Name} 被击败，移除卡牌");
                    StateMachine.CardManager.RemoveCard(targetPos);
                    StateMachine.CardManager.NotifyCardRemoved(targetPos);
                }
                else
                {
                    // 没死就触发受伤事件
                    if (targetHpBefore > target.Data.Health)
                    {
                        StateMachine.CardManager.NotifyCardDamaged(targetPos);
                        Debug.Log($"目标 {target.Data.Name} 受伤");
                    }
                }

                // 攻击者是否死亡（通常是受到反击）
                if (attackerDead)
                {
                    Debug.Log($"攻击者 {attacker.Data.Name} 被反击致死，移除卡牌");
                    StateMachine.CardManager.RemoveCard(attacker.Position); 
                    StateMachine.CardManager.NotifyCardRemoved(attacker.Position);
                }
                else
                {
                    if (attackerHpBefore > attacker.Data.Health)
                    {
                        StateMachine.CardManager.NotifyCardDamaged(attacker.Position);
                        Debug.Log($"攻击者 {attacker.Data.Name} 受伤");
                    }
                    
                    // 标记卡牌为已行动
                    attacker.HasActed = true;
                    Debug.Log($"标记卡牌 {attacker.Data.Name} 为已行动");
                }

                // 检查是否结束回合
                CheckEndTurn();
            }
            
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