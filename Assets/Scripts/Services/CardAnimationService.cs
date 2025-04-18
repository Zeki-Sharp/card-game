using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChessGame
{
    /// <summary>
    /// 卡牌动画服务 - 负责处理卡牌的动画效果
    /// </summary>
    public class CardAnimationService : MonoBehaviour
    {
        private static CardAnimationService _instance;
        public static CardAnimationService Instance => _instance;

        [SerializeField] private CardManager cardManager;
        [SerializeField] private float moveAnimationDuration = 0.5f;
        [SerializeField] private float attackAnimationDuration = 0.3f;
        [SerializeField] private float removeAnimationDuration = 0.5f;
        [SerializeField] private float flipAnimationDuration = 0.5f;
        
        // 添加动画队列系统
        private Queue<System.Func<IEnumerator>> _animationQueue = new Queue<System.Func<IEnumerator>>();
        private bool _isProcessingQueue = false;
        
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
            }
        }
        
        /// <summary>
        /// 添加动画到队列
        /// </summary>
        /// <param name="animationFunc">返回动画协程的函数</param>
        private void EnqueueAnimation(System.Func<IEnumerator> animationFunc)
        {
            _animationQueue.Enqueue(animationFunc);
            
            // 如果队列没在处理中，开始处理
            if (!_isProcessingQueue)
            {
                StartCoroutine(ProcessAnimationQueue());
            }
        }
        
        /// <summary>
        /// 处理动画队列，确保动画按顺序执行
        /// </summary>
        private IEnumerator ProcessAnimationQueue()
        {
            _isProcessingQueue = true;
            
            while (_animationQueue.Count > 0)
            {
                var nextAnimation = _animationQueue.Dequeue();
                yield return StartCoroutine(nextAnimation());
            }
            
            _isProcessingQueue = false;
        }
        
        // 播放移动动画
        private void PlayMoveAnimation(Vector2Int fromPosition, Vector2Int toPosition)
        {
            Debug.Log($"播放移动动画: 从 {fromPosition} 到 {toPosition}");
            
            // 将动画添加到队列
            EnqueueAnimation(() => MoveAnimationCoroutine(
                cardManager.GetCardView(toPosition),
                GetWorldPosition(toPosition)));
        }
        
        // 移动动画协程
        private IEnumerator MoveAnimationCoroutine(CardView cardView, Vector3 targetPosition)
        {
            float duration = 0.3f;
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
                
                // 将动画添加到队列
                EnqueueAnimation(() => AttackAnimationCoroutine(attackerView, targetWorldPos));
            }
        }
        
        // 攻击动画协程
        private IEnumerator AttackAnimationCoroutine(CardView attackerView, Vector3 targetPosition)
        {
            if (attackerView == null) yield break;
            
            Vector3 originalPosition = attackerView.transform.position;
            Vector3 midPosition = Vector3.Lerp(originalPosition, targetPosition, 0.5f);
            float duration = 0.3f;
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
            
            // 更新卡牌视图
            attackerView.UpdateVisuals();
        }
        
        // 播放受伤动画
        private void PlayDamageAnimation(Vector2Int position)
        {
            Debug.Log($"播放受伤动画: {position}");
            
            CardView cardView = cardManager.GetCardView(position);
            if (cardView != null)
            {
                // 将动画添加到队列
                EnqueueAnimation(() => DamageAnimationCoroutine(cardView, position));
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
            float shrinkDuration = 0.15f;
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
            float recoverDuration = 0.25f;
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
            
            // 更新卡牌视觉效果
            cardView.UpdateVisuals();
            
            // 检查是否需要死亡动画
            Card card = cardManager.GetCard(position);
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
                // 将动画添加到队列
                EnqueueAnimation(() => DeathAnimationCoroutine(cardView, position));
            }
        }
        
        // 死亡动画协程
        private IEnumerator DeathAnimationCoroutine(CardView cardView, Vector2Int position)
        {
            if (cardView == null) yield break;
            
            // 缩小并淡出
            float duration = removeAnimationDuration;
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
                // 将动画添加到队列
                EnqueueAnimation(() => FlipAnimationCoroutine(cardView, isFaceDown));
            }
        }
        
        // 使用缩放而非旋转的翻转动画
        private IEnumerator FlipAnimationCoroutine(CardView cardView, bool isFaceDown)
        {
            float duration = 0.5f;
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
        }

        // 卡牌属性修改事件响应
        private void OnCardStatModified(Vector2Int position)
        {
            Debug.Log($"卡牌属性被修改: {position}");
            // 将动画添加到队列
            EnqueueAnimation(() => PlayGrowAnimation(position));
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
            // 将放置动画添加到队列
            EnqueueAnimation(() => PlaceAnimationCoroutine(position, startPosition));
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
            float squishDuration = 0.15f;
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