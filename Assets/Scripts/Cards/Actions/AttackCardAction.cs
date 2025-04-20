using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace ChessGame
{
    /// <summary>
    /// 卡牌攻击行动 - 包含完整的攻击逻辑，但不负责攻击范围检查
    /// </summary>
    public class AttackCardAction : CardAction
    {
        private Vector2Int _attackerPosition;
        private Vector2Int _targetPosition;
        private int _damageDealt; // 记录造成的伤害
        private bool _isAnimationCompleted = false; // 标记动画是否完成
        
        public AttackCardAction(CardManager cardManager, Vector2Int attackerPosition, Vector2Int targetPosition) 
            : base(cardManager)
        {
            _attackerPosition = attackerPosition;
            _targetPosition = targetPosition;
            
            // 确保CardManager不为空
            if (CardManager == null)
            {
                Debug.LogError("AttackCardAction: CardManager 为空");
                // 尝试再次从场景中获取
                CardManager = GameObject.FindObjectOfType<CardManager>();
                if (CardManager == null)
                {
                    Debug.LogError("AttackCardAction: 无法从场景中找到 CardManager");
                }
            }
        }
        
        // 获取造成的伤害
        public int GetDamageDealt()
        {
            return _damageDealt;
        }
        
        public override bool CanExecute()
        {
            // 检查CardManager是否为空
            if (CardManager == null)
            {
                Debug.LogError("AttackCardAction.CanExecute: CardManager 为空");
                // 最后一次尝试获取
                CardManager = GameObject.FindObjectOfType<CardManager>();
                if (CardManager == null)
                {
                    return false;
                }
            }
            
            // 获取攻击者卡牌
            Card attackerCard = CardManager.GetCard(_attackerPosition);
            if (attackerCard == null)
            {
                Debug.LogWarning($"攻击失败：攻击者位置 {_attackerPosition} 没有卡牌");
                return false;
            }
            
            // 获取目标卡牌
            Card targetCard = CardManager.GetCard(_targetPosition);
            if (targetCard == null)
            {
                Debug.LogWarning($"攻击失败：目标位置 {_targetPosition} 没有卡牌");
                return false;
            }
            
            // 移除攻击范围检查，由能力系统负责
            // 只检查基本条件
            
            return true;
        }
        
        // 注册动画完成的回调
        private void RegisterAnimationCompletionCallback()
        {
            if (GameEventSystem.Instance != null)
            {
                GameEventSystem.Instance.OnAttackAnimFinished += AnimationCompleted;
            }
            else
            {
                Debug.LogError("找不到CardAnimationService实例，无法注册动画完成回调");
                // 如果没有动画服务，直接标记为完成
                _isAnimationCompleted = true;
            }
        }
        
        // 动画完成的回调方法
        private void AnimationCompleted(Vector2Int attackerPosition, int attackerId, int targetId)
        {
            _isAnimationCompleted = true;
            // 取消订阅，避免多次触发
            if (GameEventSystem.Instance != null)
            {
                GameEventSystem.Instance.OnAttackAnimFinished -= AnimationCompleted;
            }
        }
        
        // 执行背面卡牌攻击的方法
        private IEnumerator ExecuteFaceDownAttack(Card attackerCard, Card targetCard, int targetHpBefore)
        {
            // 翻面
            targetCard.FlipToFaceUp();
            
            // 触发翻面事件 - 播放翻面动画
            GameEventSystem.Instance.NotifyCardFlipped(_targetPosition, false);
            
            // 等待一小段时间让翻面动画完成
            yield return new WaitForSeconds(0.5f);
            
            // 触发攻击事件 - 播放攻击动画
            RegisterAnimationCompletionCallback();
            GameEventSystem.Instance.NotifyCardAttacked(_attackerPosition, _targetPosition);
            
            // 等待攻击动画完成
            float waitTime = 0f;
            float maxWaitTime = 2f; // 最大等待时间，避免无限等待
            while (!_isAnimationCompleted && waitTime < maxWaitTime)
            {
                yield return null;
                waitTime += Time.deltaTime;
            }
            
            // 计算并应用伤害 - 只在动画完成后执行
            int damage = attackerCard.Data.Attack;
            targetCard.Data.Health -= damage;
            
            // 计算造成的伤害
            _damageDealt = targetHpBefore - targetCard.Data.Health;
            
            // 标记攻击者已行动
            attackerCard.HasActed = true;
            
            // 触发受伤事件 - 播放受伤动画
            GameEventSystem.Instance.NotifyCardDamaged(_targetPosition);
            
            // 检查目标卡牌是否死亡
            CheckAndRemoveIfDead(targetCard, _targetPosition);
            
            Debug.Log($"卡牌 {attackerCard.Data.Name} 攻击背面卡牌 {targetCard.Data.Name} 成功，造成 {_damageDealt} 点伤害");
            
            // 如果是玩家卡牌，通知玩家行动完成
            if (attackerCard.OwnerId == 0) // 是玩家卡牌
            {
                Debug.Log("玩家卡牌攻击完成，通知玩家行动完成");
                GameEventSystem.Instance.NotifyPlayerActionCompleted(attackerCard.OwnerId);
            }
            // 如果是敌方卡牌，通知敌方行动完成
            else if (attackerCard.OwnerId == 1) // 是敌方卡牌
            {
                Debug.Log("敌方卡牌攻击完成，通知敌方行动完成");
                GameEventSystem.Instance.NotifyEnemyActionCompleted(attackerCard.OwnerId);
            }
        }
        
        // 执行正面卡牌攻击的方法
        private IEnumerator ExecuteFaceUpAttack(Card attackerCard, Card targetCard, int attackerHpBefore, int targetHpBefore)
        {
            // 触发攻击事件 - 播放攻击动画
            RegisterAnimationCompletionCallback();
            GameEventSystem.Instance.NotifyCardAttacked(_attackerPosition, _targetPosition);
            
            // 等待攻击动画完成
            float waitTime = 0f;
            float maxWaitTime = 2f; // 最大等待时间
            while (!_isAnimationCompleted && waitTime < maxWaitTime)
            {
                yield return null;
                waitTime += Time.deltaTime;
            }
            
            // 计算并应用伤害 - 只在动画完成后执行
            int damage = attackerCard.Data.Attack;
            targetCard.Data.Health -= damage;
            
            // 计算造成的伤害
            _damageDealt = damage;
            
            // 触发受伤事件 - 播放受伤动画
            GameEventSystem.Instance.NotifyCardDamaged(_targetPosition);
            
            // 等待一小段时间让受伤动画完成
            yield return new WaitForSeconds(0.3f);
            
            // 判断是否应该受到反伤
            bool canCounter = targetCard.ShouldReceiveCounterAttack(attackerCard, CardManager.GetAllCards());
            
            if (canCounter)
            {
                Debug.Log($"卡牌 {targetCard.Data.Name} 对 {attackerCard.Data.Name} 进行反击");
                
                // 重置动画完成标志
                _isAnimationCompleted = false;
                
                // 触发反击攻击事件 - 播放反击动画
                RegisterAnimationCompletionCallback();
                // 注意：这里反向触发攻击事件，表示目标卡牌攻击攻击者卡牌
                GameEventSystem.Instance.NotifyCardAttacked(_targetPosition, _attackerPosition);
                
                // 等待反击动画完成
                waitTime = 0f;
                while (!_isAnimationCompleted && waitTime < maxWaitTime)
                {
                    yield return null;
                    waitTime += Time.deltaTime;
                }
                
                // 计算并应用反伤 - 只在动画完成后执行
                int counterDamage = targetCard.Data.Attack;
                attackerCard.Data.Health -= counterDamage;
                
                // 触发攻击者受伤事件 - 播放受伤动画
                GameEventSystem.Instance.NotifyCardDamaged(_attackerPosition);
            }
            else
            {
                Debug.Log($"卡牌 {attackerCard.Data.Name} 不在 {targetCard.Data.Name} 的攻击范围内，不受反伤");
            }
            
            // 标记攻击者已行动
            attackerCard.HasActed = true;
            
            // 记录攻击后的生命值
            int attackerHealthAfter = attackerCard.Data.Health;
            int targetHealthAfter = targetCard.Data.Health;
            
            Debug.Log($"攻击前生命值 - 攻击者: {attackerHpBefore}, 目标: {targetHpBefore}");
            Debug.Log($"攻击后生命值 - 攻击者: {attackerHealthAfter}, 目标: {targetHealthAfter}");
            Debug.Log($"造成伤害: {_damageDealt}");
            
            // 检查卡牌是否死亡
            CheckAndRemoveIfDead(attackerCard, _attackerPosition);
            CheckAndRemoveIfDead(targetCard, _targetPosition);
            
            // 如果是玩家卡牌，通知玩家行动完成
            if (attackerCard.OwnerId == 0) // 是玩家卡牌
            {
                Debug.Log("玩家卡牌攻击完成，通知玩家行动完成");
                GameEventSystem.Instance.NotifyPlayerActionCompleted(attackerCard.OwnerId);
            }
            // 如果是敌方卡牌，通知敌方行动完成
            else if (attackerCard.OwnerId == 1) // 是敌方卡牌
            {
                Debug.Log("敌方卡牌攻击完成，通知敌方行动完成");
                GameEventSystem.Instance.NotifyEnemyActionCompleted(attackerCard.OwnerId);
            }
            
            yield break; // 使用yield break代替return语句
        }
        
        public override bool Execute()
        {    
            if (!CanExecute())
                return false;
                
            // 获取攻击者和目标卡牌
            Card attackerCard = CardManager.GetCard(_attackerPosition);
            Card targetCard = CardManager.GetCard(_targetPosition);
            
            // 记录攻击前的生命值
            int attackerHpBefore = attackerCard.Data.Health;
            int targetHpBefore = targetCard.Data.Health;
            
            // 找到MonoBehaviour对象来启动协程
            CardManager monoBehaviour = CardManager;
            if (monoBehaviour == null)
            {
                monoBehaviour = GameObject.FindObjectOfType<CardManager>();
                if (monoBehaviour == null)
                {
                    Debug.LogError("无法找到MonoBehaviour对象来启动协程");
                    return false;
                }
            }
            
            // 根据卡牌是否背面选择不同的处理方式
            if (targetCard.IsFaceDown)
            {
                Debug.Log("目标是背面卡牌，先翻面");
                monoBehaviour.StartCoroutine(ExecuteFaceDownAttack(attackerCard, targetCard, targetHpBefore));
            }
            else
            {
                Debug.Log("目标是正面卡牌，直接执行攻击");
                monoBehaviour.StartCoroutine(ExecuteFaceUpAttack(attackerCard, targetCard, attackerHpBefore, targetHpBefore));
            }
            
            return true;
        }

        private void CheckAndRemoveIfDead(Card card, Vector2Int position)
        {
            if (card != null && card.Data.Health <= 0)
            {
                // 移除卡牌
                GameEventSystem.Instance.NotifyCardRemoved(position);
                Debug.Log($"卡牌 {card.Data.Name} 生命值为 {card.Data.Health}，将被移除");
                CardManager.RemoveCard(position);
            }
        }
    }
} 