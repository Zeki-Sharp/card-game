using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

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
        [SerializeField] private Sprite cardFrontSprite; // 卡牌正面图片
        [SerializeField] private TextMeshPro nameText;
        
        private Card _card;
        private bool _isSelected = false;
        private Sprite _frontSprite; // 保存正面图片
        private List<Coroutine> _activeCoroutines = new List<Coroutine>();
        
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
        
        // 对象销毁时清理引用
        private void OnDestroy()
        {
            Debug.Log($"CardView被销毁: {gameObject.name}");
            
            // 停止所有协程
            StopAllCoroutines();
            
            // 清理协程列表
            _activeCoroutines.Clear();
            
            // 清除卡牌引用
            _card = null;
            
            // 清除渲染器和文本引用
            cardRenderer = null;
            frameRenderer = null;
            attackBackRenderer = null;
            healthBackRenderer = null;
            attackText = null;
            healthText = null;
            nameText = null;
        }
        
        public void Initialize(Card card)
        {
            _card = card;
            
            // 调试信息
            Debug.Log($"初始化卡牌视图: {card.Data.Name}, 图片: {(card.Data.Image != null ? "有图片" : "无图片")}");
            
            // 保存正面图片
            _frontSprite = card.Data.Image;
            
            UpdateVisuals();
        }
        
        public void UpdateVisuals()
        {
            // 添加安全检查，防止访问已销毁的对象
            if (this == null || !gameObject || !gameObject.activeInHierarchy)
            {
                Debug.LogWarning("尝试更新已销毁或未激活的CardView");
                return;
            }
            
            if (_card == null) return;
            
            // 更新卡牌名称
            if (nameText != null)
            {
                nameText.text = _card.Data.Name;
            }
            
            // 更新卡牌图片 - 使用卡牌数据中的图片
            if (cardRenderer != null)
            {
                if (_card.IsFaceDown)
                {
                    // 背面状态 - 使用卡背图片
                    cardRenderer.sprite = cardBackSprite;
                    
                    // 隐藏属性文本
                    if (nameText != null) nameText.gameObject.SetActive(false);
                    if (attackText != null) attackText.gameObject.SetActive(false);
                    if (healthText != null) healthText.gameObject.SetActive(false);
                    if (attackBackRenderer != null) attackBackRenderer.gameObject.SetActive(false);
                    if (healthBackRenderer != null) healthBackRenderer.gameObject.SetActive(false);
                }
                else
                {
                    // 正面状态 - 使用卡牌数据中的图片
                    if (_card.Data.Image != null)
                    {
                        cardRenderer.sprite = _card.Data.Image;
                        Debug.Log($"使用卡牌数据图片: {_card.Data.Name}");
                    }
                    else
                    {
                        // 如果卡牌数据中没有图片，使用默认正面图片
                        cardRenderer.sprite = cardFrontSprite;
                        Debug.LogWarning($"卡牌 {_card.Data.Name} 没有图片，使用默认正面图片");
                    }
                    
                    // 显示属性文本
                    if (nameText != null) nameText.gameObject.SetActive(true);
                    if (attackText != null) attackText.gameObject.SetActive(true);
                    if (healthText != null) healthText.gameObject.SetActive(true);
                    if (attackBackRenderer != null) attackBackRenderer.gameObject.SetActive(true);
                    if (healthBackRenderer != null) healthBackRenderer.gameObject.SetActive(true);
                }
            }
            
            // 更新攻击力和生命值
            if (attackText != null)
            {
                attackText.text = _card.Data.Attack.ToString();
            }
            
            if (healthText != null)
            {
                // 设置血量文字内容
                healthText.text = _card.Data.Health.ToString();
                
                // 根据生命值设置颜色
                /*if (_card.Data.Health <= 0)
                {
                    healthText.color = Color.red;
                }
                else if (_card.Data.Health <= 2)
                {
                    healthText.color = new Color(1.0f, 0.5f, 0.0f); // 橙色
                }
                else
                {
                    healthText.color = Color.green;
                }*/
            }
            
            // 根据所有者设置边框颜色
            if (frameRenderer != null)
            {
                frameRenderer.color = _card.OwnerId == 0 ? playerColor : enemyColor;
                
                // 如果已行动，降低亮度
                if (_card.HasActed && !_card.IsFaceDown)
                {
                    frameRenderer.color = actedColor;
                }
            }
            
            // 设置排序顺序
            if (cardRenderer != null) cardRenderer.sortingOrder = 100;
            if (frameRenderer != null) frameRenderer.sortingOrder = 101;
            if (attackBackRenderer != null) attackBackRenderer.sortingOrder = 102;
            if (healthBackRenderer != null) healthBackRenderer.sortingOrder = 102;
            if (attackText != null) attackText.sortingOrder = 103;
            if (healthText != null) healthText.sortingOrder = 103;
        }   
        
        public Card GetCard()
        {
            return _card;
        }
     
        // 播放简单视觉反馈 - 这些方法仅包含简单的视觉效果，不涉及位置移动
        public void PlayDamageEffect()
        {
            // 添加安全检查
            if (this == null || !gameObject || !gameObject.activeInHierarchy)
            {
                Debug.LogWarning("尝试在已销毁或未激活的CardView上播放特效");
                return;
            }
            
            StartCoroutine(DamageEffectCoroutine());
        }
        
        private System.Collections.IEnumerator DamageEffectCoroutine()
        {
            // 改为只标红一次
            cardRenderer.color = Color.red;
            yield return new WaitForSeconds(0.2f);
            cardRenderer.color = Color.white;
        }
        
        public void PlayBuffEffect()
        {
            StartCoroutine(BuffEffectCoroutine());
        }
        
        private System.Collections.IEnumerator BuffEffectCoroutine()
        {
            // 改为只闪烁一次绿色
            cardRenderer.color = Color.green;
            yield return new WaitForSeconds(0.2f);
            cardRenderer.color = Color.white;
        }
        
        // 添加治疗效果
        public void PlayHealEffect()
        {
            StartCoroutine(HealEffectCoroutine());
        }
        
        private System.Collections.IEnumerator HealEffectCoroutine()
        {
            // 治疗使用淡绿色/青色
            cardRenderer.color = new Color(0.3f, 1f, 0.7f);
            yield return new WaitForSeconds(0.2f);
            cardRenderer.color = Color.white;
        }
        
        // 高亮显示卡牌
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            
            // 如果有边框渲染器，可以改变其颜色
            if (frameRenderer != null)
            {
                if (_isSelected)
                {
                    // 存储原始颜色并修改为高亮颜色
                    frameRenderer.color = Color.yellow;
                }
                else
                {
                    // 恢复原始颜色
                    frameRenderer.color = _card.OwnerId == 0 ? playerColor : enemyColor;
                    
                    // 如果已行动，降低亮度
                    if (_card.HasActed && !_card.IsFaceDown)
                    {
                        frameRenderer.color = actedColor;
                    }
                }
            }
        }
    }
} 