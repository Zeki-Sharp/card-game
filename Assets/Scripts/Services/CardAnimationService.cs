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
        [SerializeField] private CardManager cardManager;
        
        private void Awake()
        {
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
            }
        
        }
        
        // 播放移动动画
        private void PlayMoveAnimation(Vector2Int fromPosition, Vector2Int toPosition)
        {
            Debug.Log($"播放移动动画: 从 {fromPosition} 到 {toPosition}");
            
            CardView cardView = cardManager.GetCardView(toPosition);
            if (cardView != null)
            {
                // 获取目标位置的世界坐标
                Vector3 targetWorldPos = GetWorldPosition(toPosition);
                
                // 播放移动动画
                StartCoroutine(MoveAnimationCoroutine(cardView, targetWorldPos));
            }
        }
        
        // 移动动画协程
        private IEnumerator MoveAnimationCoroutine(CardView cardView, Vector3 targetPosition)
        {
            Vector3 startPosition = cardView.transform.position;
            float duration = 0.3f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                cardView.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            cardView.transform.position = targetPosition;
            
            // 更新卡牌视图
            cardView.UpdateVisuals();
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
                
                // 播放攻击动画
                StartCoroutine(AttackAnimationCoroutine(attackerView, targetWorldPos));
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
                cardView.PlayDamageAnimation();
            }
        }
        
        // 播放销毁动画
        private void PlayDestroyAnimation(Vector2Int position)
        {
            Debug.Log($"播放销毁动画: {position}");
            
            CardView cardView = cardManager.GetCardView(position);
            if (cardView != null)
            {
                cardView.PlayDeathAnimation();
            }
        }
        
        // 播放翻面动画
        private void PlayFlipAnimation(Vector2Int position, bool isFaceDown)
        {
            Debug.Log($"播放翻面动画: 位置 {position}, 是否背面: {isFaceDown}");
            
            CardView cardView = cardManager.GetCardView(position);
            if (cardView != null)
            {
                // 播放翻转动画
                StartCoroutine(FlipAnimationCoroutine(cardView, isFaceDown));
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
    }
} 