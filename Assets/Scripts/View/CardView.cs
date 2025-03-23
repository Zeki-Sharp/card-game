using UnityEngine;
using TMPro;
using System.Collections;

namespace ChessGame
{
    public class CardView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer cardRenderer;
        [SerializeField] private SpriteRenderer frameRenderer;
        [SerializeField] private SpriteRenderer attackBackRenderer;
        [SerializeField] private SpriteRenderer healthBackRenderer;
        [SerializeField] private TextMeshPro attackText;
        [SerializeField] private TextMeshPro healthText;
        [SerializeField] private Color playerColor = Color.blue;
        [SerializeField] private Color enemyColor = Color.red;
        [SerializeField] private Color actedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        [SerializeField] private Sprite cardBackSprite; // 卡牌背面图片
        
        private Card _card;
        private bool _isSelected = false;
        private Sprite _frontSprite; // 保存正面图片
        
        private void Awake()
        {
            // 获取碰撞体组件
            BoxCollider collider = GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<BoxCollider>();
                Debug.Log("为CardView添加了BoxCollider组件");
            }
            
            // 调整碰撞体大小
            collider.size = new Vector3(0.9f, 0.2f, 0.9f); // 调整大小使其适合卡牌
            collider.center = new Vector3(0f, 0.1f, 0f); // 稍微抬高中心点
            
            // 如果没有设置卡背图片，尝试从Resources加载
            if (cardBackSprite == null)
            {
                cardBackSprite = Resources.Load<Sprite>("CardBack");
                if (cardBackSprite == null)
                {
                    Debug.LogWarning("未找到卡背图片，请确保Resources文件夹中有CardBack图片");
                }
            }
        }
        
        public void Initialize(Card card)
        {
            _card = card;
            
            // 保存正面图片
            if (card.Data.Image != null)
            {
                _frontSprite = card.Data.Image;
            }
            
            UpdateVisuals();
        }
        
        public void UpdateVisuals()
        {
            if (_card == null) return;
            
            // 更新卡牌图片
            if (cardRenderer != null)
            {
                // 根据卡牌状态显示正面或背面
                if (_card.IsFaceDown && cardBackSprite != null)
                {
                    cardRenderer.sprite = cardBackSprite;
                    
                    // 隐藏攻击力和生命值
                    if (attackText != null) attackText.gameObject.SetActive(false);
                    if (healthText != null) healthText.gameObject.SetActive(false);
                    if (attackBackRenderer != null) attackBackRenderer.gameObject.SetActive(false);
                    if (healthBackRenderer != null) healthBackRenderer.gameObject.SetActive(false);
                }
                else
                {
                    cardRenderer.sprite = _frontSprite;
                    
                    // 显示攻击力和生命值
                    if (attackText != null)
                    {
                        attackText.gameObject.SetActive(true);
                        attackText.text = _card.Data.Attack.ToString();
                    }
                    
                    if (healthText != null)
                    {
                        healthText.gameObject.SetActive(true);
                        healthText.text = _card.Data.Health.ToString();
                    }
                    
                    if (attackBackRenderer != null) attackBackRenderer.gameObject.SetActive(true);
                    if (healthBackRenderer != null) healthBackRenderer.gameObject.SetActive(true);
                }
            }
            
            // 根据所属玩家设置边框颜色
            if (frameRenderer != null)
            {
                frameRenderer.color = _card.OwnerId == 0 ? playerColor : enemyColor;
                
                // 如果已行动或是背面状态，降低亮度
                if (_card.HasActed || _card.IsFaceDown)
                {
                    frameRenderer.color = actedColor;
                }
            }

            cardRenderer.sortingOrder = 100;
            frameRenderer.sortingOrder = 101;
            if (attackBackRenderer != null) attackBackRenderer.sortingOrder = 102;
            if (healthBackRenderer != null) healthBackRenderer.sortingOrder = 102;
            if (attackText != null) attackText.sortingOrder = 103;
            if (healthText != null) healthText.sortingOrder = 103;
        }   
        
        public Card GetCard()
        {
            return _card;
        }
        
        // 播放翻转动画
        public void PlayFlipAnimation()
        {
            StartCoroutine(FlipAnimationCoroutine());
        }
        
        private IEnumerator FlipAnimationCoroutine()
        {
            float duration = 0.5f;
            float elapsed = 0f;
            
            // 第一阶段：缩小X轴直到看不见
            Vector3 originalScale = transform.localScale;
            Vector3 flatScale = new Vector3(0.01f, originalScale.y, originalScale.z);
            
            while (elapsed < duration / 2)
            {
                float t = elapsed / (duration / 2);
                transform.localScale = Vector3.Lerp(originalScale, flatScale, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 更新卡牌视觉效果
            UpdateVisuals();
            
            // 第二阶段：恢复X轴大小
            elapsed = 0f;
            while (elapsed < duration / 2)
            {
                float t = elapsed / (duration / 2);
                transform.localScale = Vector3.Lerp(flatScale, originalScale, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            transform.localScale = originalScale;
        }
        
        // 播放攻击动画
        public void PlayAttackAnimation(Vector3 targetPosition)
        {
            StartCoroutine(AttackAnimationCoroutine(targetPosition));
        }
        
        private System.Collections.IEnumerator AttackAnimationCoroutine(Vector3 targetPosition)
        {
            Vector3 originalPosition = transform.position;
            Vector3 midPosition = Vector3.Lerp(originalPosition, targetPosition, 0.6f);
            
            // 移向目标
            float duration = 0.2f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                transform.position = Vector3.Lerp(originalPosition, midPosition, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 返回原位
            elapsed = 0f;
            while (elapsed < duration)
            {
                transform.position = Vector3.Lerp(midPosition, originalPosition, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            transform.position = originalPosition;
        }
        
        // 播放受伤动画
        public void PlayDamageAnimation()
        {
            StartCoroutine(DamageAnimationCoroutine());
        }
        
        private System.Collections.IEnumerator DamageAnimationCoroutine()
        {
            // 闪烁效果
            for (int i = 0; i < 3; i++)
            {
                cardRenderer.color = Color.red;
                yield return new WaitForSeconds(0.1f);
                cardRenderer.color = Color.white;
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        // 播放死亡动画
        public void PlayDeathAnimation()
        {
            StartCoroutine(DeathAnimationCoroutine());
        }
        
        private System.Collections.IEnumerator DeathAnimationCoroutine()
        {
            // 缩小并淡出
            float duration = 0.5f;
            float elapsed = 0f;
            
            Vector3 originalScale = transform.localScale;
            Color originalColor = cardRenderer.color;
            Color targetColor = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
            
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
                cardRenderer.color = Color.Lerp(originalColor, targetColor, t);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            Destroy(gameObject);
        }
        
        // 设置选中状态
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            
            // 可以添加选中效果，例如改变边框颜色或添加发光效果
            if (frameRenderer != null)
            {
                if (_isSelected)
                {
                    // 选中效果，例如黄色边框
                    frameRenderer.color = Color.yellow;
                }
                else
                {
                    // 恢复正常颜色
                    UpdateVisuals();
                }
            }
        }
        
        // 设置可攻击状态
        public void SetAttackable(bool attackable)
        {
            // 可以添加可攻击效果，例如红色边框
            if (frameRenderer != null)
            {
                if (attackable)
                {
                    // 可攻击效果，例如红色边框
                    frameRenderer.color = Color.red;
                }
                else
                {
                    // 恢复正常颜色
                    UpdateVisuals();
                }
            }
        }
    }
} 