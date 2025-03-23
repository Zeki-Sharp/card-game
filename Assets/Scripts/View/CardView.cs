using UnityEngine;
using TMPro;

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
        
        private Card _card;
        private bool _isSelected = false;
        
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
        }
        
        public void Initialize(Card card)
        {
            _card = card;
            UpdateVisuals();
        }
        
        public void UpdateVisuals()
        {
            if (_card == null) return;
            
            // 更新卡牌图片
            if (cardRenderer != null && _card.Data.Image != null)
            {
                cardRenderer.sprite = _card.Data.Image;
            }
            
            // 更新攻击力和生命值文本
            if (attackText != null)
            {
                attackText.text = _card.Data.Attack.ToString();
            }
            
            if (healthText != null)
            {
                healthText.text = _card.Data.Health.ToString();
            }
            
            // 根据所属玩家设置边框颜色
            if (frameRenderer != null)
            {
                frameRenderer.color = _card.OwnerId == 0 ? playerColor : enemyColor;
                
                // 如果已行动，降低亮度
                if (_card.HasActed)
                {
                    frameRenderer.color = actedColor;
                }
            }

            cardRenderer.sortingOrder = 100;
            frameRenderer.sortingOrder = 101;
            attackBackRenderer.sortingOrder = 102;
            healthBackRenderer.sortingOrder = 102;
            attackText.sortingOrder = 103;
            healthText.sortingOrder = 103;
        }   
        
        public Card GetCard()
        {
            return _card;
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