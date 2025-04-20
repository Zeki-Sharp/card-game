/*using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ChessGame.Cards;
using System; // 添加System命名空间

namespace ChessGame
{
    /// <summary>
    /// 卡牌反击行动 - 处理卡牌被攻击后的反击
    /// </summary>
    public class CounterAttackAction : CardAction
    {
        private Vector2Int _defenderPosition;
        private Vector2Int _attackerPosition;
        private bool _isAnimationCompleted = false; // 标记动画是否完成
        
        // 添加反击完成事件
        public event Action OnCounterAttackCompleted;
        
        public CounterAttackAction(CardManager cardManager, Vector2Int defenderPosition, Vector2Int attackerPosition) 
            : base(cardManager)
        {
            _defenderPosition = defenderPosition;
            _attackerPosition = attackerPosition;
            
            // 确保CardManager不为空
            if (CardManager == null)
            {
                Debug.LogError("CounterAttackAction: CardManager 为空");
                // 尝试再次从场景中获取
                CardManager = GameObject.FindObjectOfType<CardManager>();
                if (CardManager == null)
                {
                    Debug.LogError("CounterAttackAction: 无法从场景中找到 CardManager");
                }
            }
        }
        
        public override bool CanExecute()
        {
            // 检查defender和attacker是否存在
            Card defenderCard = CardManager.GetCard(_defenderPosition);
            if (defenderCard == null)
            {
                Debug.LogWarning($"反击失败：防御者位置 {_defenderPosition} 没有卡牌");
                return false;
            }
            
            Card attackerCard = CardManager.GetCard(_attackerPosition);
            if (attackerCard == null)
            {
                Debug.LogWarning($"反击失败：攻击者位置 {_attackerPosition} 没有卡牌");
                return false;
            }
            
            return true;
        }
        
        public override bool Execute()
        {
            // 修正：不再发出警告，而是实际执行反击操作
            Debug.Log("开始执行CounterAttackAction.Execute()");
            
            // 确保CanExecute检查通过
            if (!CanExecute())
                return false;
                
            // 手动启动协程
            if (CardManager != null)
            {
                Debug.Log("通过CardManager启动反击协程");
                CardManager.StartCoroutine(ExecuteCoroutine());
                return true;
            }
            else
            {
                Debug.LogError("CounterAttackAction.Execute: CardManager为空，无法启动协程");
                return false;
            }
        }
        
        // 注册动画完成的回调
        private void RegisterAnimationCompletionCallback()
        {
            if (GameEventSystem.Instance != null)
            {
                Debug.Log("注册攻击动画完成回调");
                GameEventSystem.Instance.OnAttackAnimFinished += AnimationCompleted;
            }
            else
            {
                Debug.LogError("找不到GameEventSystem实例，无法注册动画完成回调");
                // 如果没有动画服务，直接标记为完成
                _isAnimationCompleted = true;
            }
        }
        
        // 动画完成的回调方法
        private void AnimationCompleted(Vector2Int attackerPosition, int attackerId, int targetId)
        {
            Debug.Log($"反击动画完成回调被触发: {attackerPosition}, {attackerId}, {targetId}");
            _isAnimationCompleted = true;
            // 取消订阅，避免多次触发
            if (GameEventSystem.Instance != null)
            {
                GameEventSystem.Instance.OnAttackAnimFinished -= AnimationCompleted;
            }
        }
        
        /// <summary>
        /// 执行反击动作的协程
        /// </summary>
        public IEnumerator ExecuteCoroutine()
        {
            Debug.Log("开始执行CounterAttackAction.ExecuteCoroutine()");
            
            if (!CanExecute())
            {
                Debug.LogWarning("反击条件检查失败，终止反击协程");
                // 即使失败也需要通知完成
                OnCounterAttackCompleted?.Invoke();
                yield break;
            }
                
            // 获取defender和attacker卡牌
            Card defenderCard = CardManager.GetCard(_defenderPosition);
            Card attackerCard = CardManager.GetCard(_attackerPosition);
            
            // 记录攻击前的生命值
            int attackerHpBefore = attackerCard.Data.Health;
            
            Debug.Log($"开始执行反击：{defenderCard.Data.Name} 对 {attackerCard.Data.Name} 进行反击");
            
            // 触发defender反击attacker的攻击动画
            _isAnimationCompleted = false; // 重置动画完成标志
            RegisterAnimationCompletionCallback();
            
            // 触发攻击动画
            Debug.Log($"触发反击攻击动画：从 {_defenderPosition} 到 {_attackerPosition}");
            GameEventSystem.Instance.NotifyCardAttacked(_defenderPosition, _attackerPosition);
            
            // 等待攻击动画完成
            float waitTime = 0f;
            float maxWaitTime = 2f; // 最大等待时间
            while (!_isAnimationCompleted && waitTime < maxWaitTime)
            {
                yield return null;
                waitTime += Time.deltaTime;
            }
            
            Debug.Log($"反击动画完成或超时，waitTime: {waitTime}");
            
            // 应用defender.Attack的伤害到attacker的生命值
            int damage = defenderCard.Data.Attack;
            attackerCard.Data.Health -= damage;
            
            Debug.Log($"反击造成 {damage} 点伤害，{attackerCard.Data.Name} 生命值从 {attackerHpBefore} 减少到 {attackerCard.Data.Health}");
            
            // 触发attacker的受伤动画
            Debug.Log($"触发受伤动画：位置 {_attackerPosition}");
            GameEventSystem.Instance.NotifyCardDamaged(_attackerPosition);
            
            // 等待受伤动画完成
            // 注册受伤动画完成回调
            bool damageAnimCompleted = false;
            Action<Vector2Int, int> damageAnimCallback = null;
            
            damageAnimCallback = (pos, dmg) => {
                if (pos == _attackerPosition) {
                    Debug.Log($"受伤动画完成: 位置 {pos}, 伤害 {dmg}");
                    damageAnimCompleted = true;
                    // 取消订阅
                    GameEventSystem.Instance.OnDamageAnimFinished -= damageAnimCallback;
                }
            };
            
            // 订阅受伤动画完成事件
            GameEventSystem.Instance.OnDamageAnimFinished += damageAnimCallback;
            
            // 等待受伤动画完成
            float damageWaitTime = 0f;
            float maxDamageWaitTime = 1.5f;
            while (!damageAnimCompleted && damageWaitTime < maxDamageWaitTime)
            {
                yield return null;
                damageWaitTime += Time.deltaTime;
            }
            
            // 如果超时但没收到回调，手动取消订阅
            if (!damageAnimCompleted) {
                GameEventSystem.Instance.OnDamageAnimFinished -= damageAnimCallback;
                Debug.LogWarning("受伤动画完成事件超时");
            }
            
            // 检查attacker是否死亡，若死亡则触发移除
            if (attackerCard.Data.Health <= 0)
            {
                Debug.Log($"{attackerCard.Data.Name} 被反击致死，生命值: {attackerCard.Data.Health}");
                GameEventSystem.Instance.NotifyCardRemoved(_attackerPosition);
                CardManager.RemoveCard(_attackerPosition);
                
                // 等待死亡动画完成
                yield return new WaitForSeconds(0.5f);
            }
            
            Debug.Log("反击动作完成");
            
            // 通知反击完成
            OnCounterAttackCompleted?.Invoke();
        }
    }
} */