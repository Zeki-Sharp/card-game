using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace ChessGame
{
    /// <summary>
    /// 卡牌动画服务 - 负责处理卡牌的动画效果
    /// </summary>
    public class AnimationManager : MonoBehaviour
    {
        private static AnimationManager _instance;
        public static AnimationManager Instance => _instance;

        [SerializeField] private CardManager cardManager;
        [SerializeField] private float moveAnimationDuration = 0.5f;
        [SerializeField] private float attackAnimationDuration = 0.3f;
        [SerializeField] private float removeAnimationDuration = 0.5f;
        [SerializeField] private float flipAnimationDuration = 0.5f;
        
        // 添加动画速度控制
        [SerializeField, Range(0.5f, 2.0f)] private float animationSpeedMultiplier = 1.0f;
        
        /// <summary>
        /// 设置动画速度倍率
        /// </summary>
        /// <param name="speedMultiplier">速度倍率（0.5-2.0）</param>
        public void SetAnimationSpeed(float speedMultiplier)
        {
            animationSpeedMultiplier = Mathf.Clamp(speedMultiplier, 0.5f, 2.0f);
        }
        
        /// <summary>
        /// 获取考虑了速度因素的动画持续时间
        /// </summary>
        /// <param name="baseDuration">基础持续时间</param>
        /// <returns>调整后的持续时间</returns>
        private float GetAdjustedDuration(float baseDuration)
        {
            return baseDuration / animationSpeedMultiplier;
        }
        
        // 添加动画组类型枚举
        public enum AnimationGroupType
        {
            Action,      // 主动行为（攻击、移动）
            Reaction,    // 反应行为（受击、翻面）
            Result,      // 结果展示（数值变化）
            Secondary    // 次要效果（高亮、提示）
        }
        
        // 动画项结构，包含动画函数和组类型
        private struct AnimationItem
        {
            public System.Func<IEnumerator> AnimationFunc;
            public AnimationGroupType GroupType;
            
            public AnimationItem(System.Func<IEnumerator> animationFunc, AnimationGroupType groupType)
            {
                AnimationFunc = animationFunc;
                GroupType = groupType;
            }
        }
        
        // 更新原有队列为新的动画项队列
        private Queue<AnimationItem> _animationQueue = new Queue<AnimationItem>();
        private bool _isProcessingQueue = false;
        
        // 添加重叠执行控制参数
        [SerializeField, Range(0.5f, 1.0f)] private float actionCompletionThreshold = 0.8f; // Action动画完成多少比例后开始下一个动画
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            if (cardManager == null)
                cardManager = FindObjectOfType<CardManager>();
        }
        
        private void Start()
        {
            // 订阅GameEventSystem的事件
            if (GameEventSystem.Instance != null)
            {
                Debug.Log("CardAnimationService: 订阅GameEventSystem事件");
                
                GameEventSystem.Instance.OnCardMoved += PlayMoveAnimation;
                GameEventSystem.Instance.OnCardAttacked += PlayAttackAnimation;
                GameEventSystem.Instance.OnCardRemoved += PlayDestroyAnimation;
                GameEventSystem.Instance.OnCardFlipped += PlayFlipAnimation;
                GameEventSystem.Instance.OnCardDamaged += PlayDamageAnimation;
                GameEventSystem.Instance.OnCardStatModified += OnCardStatModified;
                GameEventSystem.Instance.OnCardHealed += OnCardHealed;
            }
            else
            {
                Debug.LogError("找不到GameEventSystem实例");
            }
        }
        
        private void OnDestroy()
        {
            // 取消订阅GameEventSystem的事件
            if (GameEventSystem.Instance != null)
            {
                GameEventSystem.Instance.OnCardMoved -= PlayMoveAnimation;
                GameEventSystem.Instance.OnCardAttacked -= PlayAttackAnimation;
                GameEventSystem.Instance.OnCardRemoved -= PlayDestroyAnimation;
                GameEventSystem.Instance.OnCardFlipped -= PlayFlipAnimation;
                GameEventSystem.Instance.OnCardDamaged -= PlayDamageAnimation;
                GameEventSystem.Instance.OnCardStatModified -= OnCardStatModified;
                GameEventSystem.Instance.OnCardHealed -= OnCardHealed;
            }
        }
        
        /// <summary>
        /// 添加动画到队列
        /// </summary>
        /// <param name="animationFunc">返回动画协程的函数</param>
        /// <param name="groupType">动画组类型</param>
        private void EnqueueAnimation(System.Func<IEnumerator> animationFunc, AnimationGroupType groupType = AnimationGroupType.Action)
        {
            _animationQueue.Enqueue(new AnimationItem(animationFunc, groupType));
            
            // 如果队列没在处理中，开始处理
            if (!_isProcessingQueue)
            {
                StartCoroutine(ProcessAnimationQueue());
            }
        }
        
        // 保留旧的方法以保持向后兼容性
        private void EnqueueAnimation(System.Func<IEnumerator> animationFunc)
        {
            EnqueueAnimation(animationFunc, AnimationGroupType.Action);
        }
        
        /// <summary>
        /// 处理动画队列，确保动画按顺序执行
        /// </summary>
        private IEnumerator ProcessAnimationQueue()
        {
            _isProcessingQueue = true;
            
            while (_animationQueue.Count > 0)
            {
                var currentAnimation = _animationQueue.Dequeue();
                
                // 获取当前动画协程
                IEnumerator currentAnimCoroutine = currentAnimation.AnimationFunc();
                
                // 如果是Action类型动画并且有后续动画
                if (currentAnimation.GroupType == AnimationGroupType.Action && _animationQueue.Count > 0)
                {
                    // 获取下一个动画的信息但不从队列中移除
                    AnimationItem nextAnimation = _animationQueue.Peek();
                    
                    // 如果下一个是Reaction类型，实现重叠执行
                    if (nextAnimation.GroupType == AnimationGroupType.Reaction)
                    {
                        // 运行当前动画到指定进度
                        float startTime = Time.time;
                        float estimatedDuration = EstimateAnimationDuration(currentAnimation);
                        float targetProgress = estimatedDuration * actionCompletionThreshold;
                        
                        // 运行当前动画直到达到目标进度
                        while (currentAnimCoroutine.MoveNext())
                        {
                            yield return currentAnimCoroutine.Current;
                            
                            // 检查是否达到了目标进度
                            if (Time.time - startTime >= targetProgress)
                                break;
                        }
                        
                        // 当主动动画达到阈值后，提前启动反应动画
                        _animationQueue.Dequeue(); // 现在可以移除下一个动画
                        StartCoroutine(nextAnimation.AnimationFunc());
                        
                        // 继续完成当前动画的剩余部分
                        while (currentAnimCoroutine.MoveNext())
                        {
                            yield return currentAnimCoroutine.Current;
                        }
                    }
                    else
                    {
                        // 正常执行当前动画
                        while (currentAnimCoroutine.MoveNext())
                        {
                            yield return currentAnimCoroutine.Current;
                        }
                    }
                }
                else
                {
                    // 其他动画类型正常执行
                    while (currentAnimCoroutine.MoveNext())
                    {
                        yield return currentAnimCoroutine.Current;
                    }
                }
            }
            
            _isProcessingQueue = false;
        }
        
        /// <summary>
        /// 估算动画持续时间
        /// </summary>
        private float EstimateAnimationDuration(AnimationItem animation)
        {
            // 基于动画类型返回估计持续时间，并应用速度调整
            float baseDuration = animation.GroupType switch
            {
                AnimationGroupType.Action => attackAnimationDuration,
                AnimationGroupType.Reaction => 0.3f,
                AnimationGroupType.Result => 0.2f,
                AnimationGroupType.Secondary => 0.2f,
                _ => 0.5f
            };
            
            return GetAdjustedDuration(baseDuration);
        }
        
        // 播放移动动画
        private void PlayMoveAnimation(Vector2Int fromPosition, Vector2Int toPosition)
        {
            Debug.Log($"播放移动动画: 从 {fromPosition} 到 {toPosition}");
            
            // 将动画添加到队列，指定为Action类型
            EnqueueAnimation(() => MoveAnimationCoroutine(
                cardManager.GetCardView(toPosition),
                GetWorldPosition(toPosition)), AnimationGroupType.Action);
        }
        
        // 移动动画协程
        private IEnumerator MoveAnimationCoroutine(CardView cardView, Vector3 targetPosition)
        {
            float duration = GetAdjustedDuration(moveAnimationDuration);
            float elapsed = 0f;
            
            // 保存原始位置
            Vector3 originalPosition = cardView.transform.position;
            
            Debug.Log($"开始移动动画: 从 {originalPosition} 到 {targetPosition}");
            
            // 执行移动动画
            while (elapsed < duration)
            {
                cardView.transform.position = Vector3.Lerp(originalPosition, targetPosition, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 确保最终位置正确
            cardView.transform.position = targetPosition;
            
            Debug.Log($"移动动画完成: {cardView.name} 现在位于 {cardView.transform.position}");
        }
        
        // 播放攻击动画
        private void PlayAttackAnimation(Vector2Int attackerPosition, Vector2Int targetPosition)
        {
            Debug.Log($"播放攻击动画: 从 {attackerPosition} 到 {targetPosition}");
            
            CardView attackerView = cardManager.GetCardView(attackerPosition);
            if (attackerView != null)
            {
                // 获取目标位置的世界坐标
                Vector3 targetWorldPos = GetWorldPosition(targetPosition);
                
                // 将动画添加到队列，指定为Action类型
                EnqueueAnimation(() => AttackAnimationCoroutine(attackerView, targetWorldPos, attackerPosition), AnimationGroupType.Action);
            }
        }
        
        // 攻击动画协程
        private IEnumerator AttackAnimationCoroutine(CardView attackerView, Vector3 targetPosition, Vector2Int attackerPosition)
        {
            if (attackerView == null) yield break;
            
            Vector3 originalPosition = attackerView.transform.position;
            Vector3 midPosition = Vector3.Lerp(originalPosition, targetPosition, 0.5f);
            float duration = GetAdjustedDuration(attackAnimationDuration);
            float elapsed = 0f;
            
            // 向目标移动一半
            while (elapsed < duration)
            {
                if (attackerView == null) yield break; // 检查对象是否仍然存在
                
                attackerView.transform.position = Vector3.Lerp(originalPosition, midPosition, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 回到原位
            elapsed = 0f;
            while (elapsed < duration)
            {
                if (attackerView == null) yield break; // 检查对象是否仍然存在
                
                attackerView.transform.position = Vector3.Lerp(midPosition, originalPosition, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            if (attackerView == null) yield break;
            
            attackerView.transform.position = originalPosition;

            
            // 获取目标单元格位置
            Card attackerCard = attackerView.GetCard();
            int attackerId = attackerCard.OwnerId;
            
            // 从targetPosition向量中获取目标位置的x和y坐标
            Vector2Int targetGridPosition = new Vector2Int(
                Mathf.RoundToInt(targetPosition.x), 
                Mathf.RoundToInt(targetPosition.y)
            );
            
            // 触发攻击动画完成事件 - 使用GameEventSystem
            if (GameEventSystem.Instance != null)
            {
                // 使用FindCardAtPosition获取目标ID
                Card targetCard = cardManager.GetCard(targetGridPosition);
                int targetId = targetCard != null ? targetCard.OwnerId : -1;
                
                GameEventSystem.Instance.NotifyAttackAnimFinished(attackerPosition, attackerId, targetId);
            }
        }
        
        // 播放受伤动画
        private void PlayDamageAnimation(Vector2Int position)
        {
            Debug.Log($"播放受伤动画: {position}");
            
            CardView cardView = cardManager.GetCardView(position);
            if (cardView != null)
            {
                // 将动画添加到队列，指定为Reaction类型
                EnqueueAnimation(() => DamageAnimationCoroutine(cardView, position), AnimationGroupType.Reaction);
            }
        }
        
        // 受伤动画协程
        private IEnumerator DamageAnimationCoroutine(CardView cardView, Vector2Int position)
        {
            // 闪烁效果
            cardView.PlayDamageEffect(); // 调用简化版的视觉效果，已修改为只标红一次
            
            // 保存原始缩放和位置
            Vector3 originalScale = cardView.transform.localScale;
            Vector3 originalPosition = cardView.transform.position;
            
            // 受伤时先缩小
            float scaleFactor = 0.85f; // 缩小到原来的85%
            Vector3 smallerScale = originalScale * scaleFactor;
            
            // 缩小阶段
            float shrinkDuration = GetAdjustedDuration(0.15f);
            float elapsed = 0f;
            
            while (elapsed < shrinkDuration)
            {
                float t = elapsed / shrinkDuration;
                cardView.transform.localScale = Vector3.Lerp(originalScale, smallerScale, t);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 确保达到最小缩放
            cardView.transform.localScale = smallerScale;
            
            // 恢复阶段
            float recoverDuration = GetAdjustedDuration(0.25f);
            elapsed = 0f;
            
            while (elapsed < recoverDuration)
            {
                float t = elapsed / recoverDuration;
                cardView.transform.localScale = Vector3.Lerp(smallerScale, originalScale, t);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 确保恢复原始大小
            cardView.transform.localScale = originalScale;
            cardView.transform.position = originalPosition;
            
            
            // 获取卡牌的当前生命值
            Card card = cardManager.GetCard(position);
            int damage = card != null ? card.Data.Health : 0;
            
            // 触发受伤动画完成事件 - 使用GameEventSystem
            if (GameEventSystem.Instance != null)
            {
                // 使用position.x * boardWidth + position.y作为位置的唯一标识符
                int positionId = position.x * cardManager.BoardWidth + position.y;
                GameEventSystem.Instance.NotifyDamageAnimFinished(position, damage);
            }
            
            // 检查是否需要死亡动画
            if (card != null && card.Data.Health <= 0)
            {
                yield return StartCoroutine(DeathAnimationCoroutine(cardView, position));
            }
        }
        
        // 播放销毁动画
        private void PlayDestroyAnimation(Vector2Int position)
        {
            Debug.Log($"播放销毁动画: {position}");
            
            CardView cardView = cardManager.GetCardView(position);
            if (cardView != null)
            {
                // 将动画添加到队列，销毁动画属于Result类型
                EnqueueAnimation(() => DeathAnimationCoroutine(cardView, position), AnimationGroupType.Result);
            }
        }
        
        // 死亡动画协程
        private IEnumerator DeathAnimationCoroutine(CardView cardView, Vector2Int position)
        {
            if (cardView == null) yield break;
            
            // 获取卡牌的当前生命值
            Card card = cardManager.GetCard(position);
            int damage = card != null ? card.Data.Health : 0;
            
            // 缩小并淡出
            float duration = GetAdjustedDuration(removeAnimationDuration);
            float elapsed = 0f;
            
            Vector3 originalScale = cardView.transform.localScale;
            SpriteRenderer[] renderers = cardView.GetComponentsInChildren<SpriteRenderer>();
            List<Color> originalColors = new List<Color>();
            
            // 保存所有渲染器的原始颜色
            foreach (SpriteRenderer renderer in renderers)
            {
                originalColors.Add(renderer.color);
            }
            
            // 执行缩小和淡出动画
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                
                // 缩小
                cardView.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
                
                // 淡出所有渲染器
                for (int i = 0; i < renderers.Length; i++)
                {
                    Color color = renderers[i].color;
                    Color targetColor = new Color(color.r, color.g, color.b, 0f);
                    renderers[i].color = Color.Lerp(originalColors[i], targetColor, t);
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 确保缩小到0
            cardView.transform.localScale = Vector3.zero;
            
            // 隐藏游戏对象
            cardView.gameObject.SetActive(false);
            
            // 触发死亡动画完成事件 - 使用GameEventSystem
            if (GameEventSystem.Instance != null)
            {
                // 使用position.x * boardWidth + position.y作为位置的唯一标识符
                int positionId = position.x * cardManager.BoardWidth + position.y;
                GameEventSystem.Instance.NotifyDeathAnimFinished(positionId, damage);
            }
            
            // 通知游戏系统可以安全销毁卡牌
            GameEventSystem.Instance?.NotifyCardDestroyed(position);
        }
        
        // 播放翻面动画
        private void PlayFlipAnimation(Vector2Int position, bool isFaceDown)
        {
            Debug.Log($"播放翻面动画: 位置 {position}, 是否背面: {isFaceDown}");
            
            CardView cardView = cardManager.GetCardView(position);
            if (cardView != null)
            {
                // 翻面动画通常是反应类型
                EnqueueAnimation(() => FlipAnimationCoroutine(cardView, isFaceDown, position), AnimationGroupType.Reaction);
            }
        }
        
        // 使用缩放而非旋转的翻转动画
        private IEnumerator FlipAnimationCoroutine(CardView cardView, bool isFaceDown, Vector2Int position)
        {
            float duration = GetAdjustedDuration(flipAnimationDuration);
            float elapsed = 0f;
            
            // 保存原始缩放
            Vector3 originalScale = cardView.transform.localScale;
            
            // 第一阶段：缩小X轴直到看不见
            while (elapsed < duration / 2)
            {
                float t = elapsed / (duration / 2);
                Vector3 scale = cardView.transform.localScale;
                scale.x = Mathf.Lerp(originalScale.x, 0, t);
                cardView.transform.localScale = scale;
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 在中间点更新卡牌视觉效果
            cardView.UpdateVisuals();
            
            // 第二阶段：放大X轴直到正常大小
            elapsed = 0f;
            while (elapsed < duration / 2)
            {
                float t = elapsed / (duration / 2);
                Vector3 scale = cardView.transform.localScale;
                scale.x = Mathf.Lerp(0, originalScale.x, t);
                cardView.transform.localScale = scale;
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 确保恢复到正常大小
            cardView.transform.localScale = originalScale;
            
            // 触发翻面动画完成事件 - 使用GameEventSystem
            if (GameEventSystem.Instance != null)
            {
                // 使用position.x * boardWidth + position.y作为位置的唯一标识符
                int positionId = position.x * cardManager.BoardWidth + position.y;
                GameEventSystem.Instance.NotifyFlipAnimFinished(positionId, isFaceDown);
            }
        }
        
        // 获取世界坐标
        private Vector3 GetWorldPosition(Vector2Int gridPosition)
        {
            // 这里需要根据你的游戏实现来计算世界坐标
            // 简单示例：假设每个格子大小为1单位
            float offsetX = -((cardManager.BoardWidth - 1) * 1f) / 2f;
            float offsetY = -((cardManager.BoardHeight - 1) * 1f) / 2f;
            
            return new Vector3(
                gridPosition.x * 1f + offsetX,
                gridPosition.y * 1f + offsetY,
                0f
            );
        }
        
        /// <summary>
        /// 播放卡牌成长动画
        /// </summary>
        /// <param name="position">卡牌位置</param>
        /// <param name="duration">动画持续时间</param>
        /// <param name="scaleMultiplier">最大缩放倍数</param>
        /// <returns>协程</returns>
        public IEnumerator PlayGrowAnimation(Vector2Int position, float duration = 0.5f, float scaleMultiplier = 1.2f)
        {
            CardView cardView = cardManager.GetCardView(position);
            if (cardView == null) yield break;
            
            // 应用速度调整
            duration = GetAdjustedDuration(duration);
            
            // 保存原始缩放
            Vector3 originalScale = cardView.transform.localScale;
            Vector3 targetScale = originalScale * scaleMultiplier;
            
            // 第一阶段：放大
            float elapsed = 0f;
            while (elapsed < duration / 2)
            {
                if (cardView == null) yield break; // 安全检查
                
                float t = elapsed / (duration / 2);
                cardView.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 播放增益效果
            cardView.PlayBuffEffect(); // 播放视觉效果
            
            // 第二阶段：恢复原大小
            elapsed = 0f;
            while (elapsed < duration / 2)
            {
                if (cardView == null) yield break; // 安全检查
                
                float t = elapsed / (duration / 2);
                cardView.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 确保恢复原始大小
            if (cardView != null)
            {
                cardView.transform.localScale = originalScale;
            }

            // 通知成长动画完成
            GameEventSystem.Instance.NotifyGrowAnimFinished(position, 0);
        }

        // 卡牌属性修改事件响应
        private void OnCardStatModified(Vector2Int position)
        {
            Debug.Log($"卡牌属性被修改: {position}");
            // 属性修改属于结果类型
            EnqueueAnimation(() => PlayGrowAnimation(position), AnimationGroupType.Result);
        }

        // 卡牌治疗事件响应
        private void OnCardHealed(Vector2Int position)
        {
            Debug.Log($"卡牌被治疗: {position}");
            // 治疗属于结果类型
            EnqueueAnimation(() => PlayHealAnimation(position), AnimationGroupType.Result);
        }

        // 播放卡牌治疗动画
        public IEnumerator PlayHealAnimation(Vector2Int position, float duration = 0.5f, float scaleMultiplier = 1.2f)
        {
            CardView cardView = cardManager.GetCardView(position);
            if (cardView == null) yield break;
            
            // 应用速度调整
            duration = GetAdjustedDuration(duration);
            
            // 保存原始缩放
            Vector3 originalScale = cardView.transform.localScale;
            Vector3 targetScale = originalScale * scaleMultiplier;
            
            // 第一阶段：放大
            float elapsed = 0f;
            while (elapsed < duration / 2)
            {
                if (cardView == null) yield break; // 安全检查
                
                float t = elapsed / (duration / 2);
                cardView.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 播放增益效果 - 治疗效果也使用绿色效果
            cardView.PlayHealEffect(); // 使用治疗专用的视觉效果
            
            // 第二阶段：恢复原大小
            elapsed = 0f;
            while (elapsed < duration / 2)
            {
                if (cardView == null) yield break; // 安全检查
                
                float t = elapsed / (duration / 2);
                cardView.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 确保恢复原始大小
            if (cardView != null)
            {
                cardView.transform.localScale = originalScale;
            }
            
            // 通知治疗动画完成
            GameEventSystem.Instance.NotifyHealAnimFinished(position, 0);
        }

        // 播放卡牌放置动画
        public IEnumerator PlayPlaceAnimation(Vector2Int position, Vector3 startPosition)
        {
            CardView cardView = cardManager.GetCardView(position);
            if (cardView == null) yield break;
            
            // 这个方法已经返回协程，可以直接在外部使用StartCoroutine
            // 或者将其添加到队列 - 根据调用方式选择合适的处理
            
            // 设置初始位置
            cardView.transform.position = startPosition;
            
            // 目标位置
            Vector3 targetPosition = GetWorldPosition(position);
            
            // 计算一个高于起点和终点的弧线中点
            Vector3 midPosition = (startPosition + targetPosition) / 2f;
            midPosition.y += 1.5f; // 增加高度形成弧线
            
            // 执行弧线运动
            float duration = 0.5f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                
                // 使用贝塞尔曲线计算路径点
                Vector3 m1 = Vector3.Lerp(startPosition, midPosition, t);
                Vector3 m2 = Vector3.Lerp(midPosition, targetPosition, t);
                cardView.transform.position = Vector3.Lerp(m1, m2, t);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 确保最终位置正确
            cardView.transform.position = targetPosition;
            
            // 播放着陆效果
            yield return StartCoroutine(LandingEffectCoroutine(cardView));
        }

        // 播放卡牌放置动画（公共接口）
        public void PlayPlaceAnimationQueued(Vector2Int position, Vector3 startPosition)
        {
            // 将放置动画添加到队列，放置是主动行为
            EnqueueAnimation(() => PlaceAnimationCoroutine(position, startPosition), AnimationGroupType.Action);
        }
        
        // 内部协程实现放置动画
        private IEnumerator PlaceAnimationCoroutine(Vector2Int position, Vector3 startPosition)
        {
            CardView cardView = cardManager.GetCardView(position);
            if (cardView == null) yield break;
            
            // 设置初始位置
            cardView.transform.position = startPosition;
            
            // 目标位置
            Vector3 targetPosition = GetWorldPosition(position);
            
            // 计算一个高于起点和终点的弧线中点
            Vector3 midPosition = (startPosition + targetPosition) / 2f;
            midPosition.y += 1.5f; // 增加高度形成弧线
            
            // 执行弧线运动
            float duration = GetAdjustedDuration(0.5f);
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                
                // 使用贝塞尔曲线计算路径点
                Vector3 m1 = Vector3.Lerp(startPosition, midPosition, t);
                Vector3 m2 = Vector3.Lerp(midPosition, targetPosition, t);
                cardView.transform.position = Vector3.Lerp(m1, m2, t);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 确保最终位置正确
            cardView.transform.position = targetPosition;
            
            // 播放着陆效果
            yield return LandingEffectCoroutine(cardView);
        }

        // 卡牌着陆效果
        private IEnumerator LandingEffectCoroutine(CardView cardView)
        {
            if (cardView == null) yield break;
            
            // 轻微缩放效果
            Vector3 originalScale = cardView.transform.localScale;
            Vector3 squishScale = new Vector3(originalScale.x * 1.1f, originalScale.y * 0.9f, originalScale.z);
            
            // 1. 轻微扁平化
            float squishDuration = GetAdjustedDuration(0.15f);
            float elapsed = 0f;
            
            while (elapsed < squishDuration)
            {
                float t = elapsed / squishDuration;
                cardView.transform.localScale = Vector3.Lerp(originalScale, squishScale, t);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 2. 恢复原始形状
            elapsed = 0f;
            while (elapsed < squishDuration)
            {
                float t = elapsed / squishDuration;
                cardView.transform.localScale = Vector3.Lerp(squishScale, originalScale, t);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 确保恢复原始大小
            cardView.transform.localScale = originalScale;
        }
    }
} 