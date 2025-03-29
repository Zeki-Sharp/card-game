using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace ChessGame
{
    /// <summary>
    /// 负责卡牌的初始化和生成
    /// </summary>
    public class CardInitializer : MonoBehaviour
    {
        private static CardInitializer _instance;
        
        public static CardInitializer Instance
        {
            get { return _instance; }
        }
        
        [SerializeField] private CardManager cardManager;
        [SerializeField] private CardDataProvider cardDataProvider;
        
        private void Awake()
        {
            // 如果已经存在实例，销毁当前对象
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"场景中存在多个CardInitializer实例，销毁重复的: {gameObject.name}");
                Destroy(gameObject);
                return;
            }
            
            // 设置单例实例
            _instance = this;
            
            // 初始化引用
            if (cardManager == null)
                cardManager = FindObjectOfType<CardManager>();
                
            if (cardDataProvider == null)
                cardDataProvider = FindObjectOfType<CardDataProvider>();
        }
        
        // 在指定位置生成卡牌
        public void SpawnCardAt(Vector2Int position, int cardId, int ownerId = 0, bool isFaceDown = true)
        {
            if (cardManager == null || cardDataProvider == null)
            {
                Debug.LogError("CardManager或CardDataProvider未设置");
                return;
            }
            
            // 检查位置是否已被占用
            if (cardManager.GetCard(position) != null)
            {
                Debug.LogWarning($"位置 {position} 已被占用，无法生成卡牌");
                return;
            }
            
            // 获取卡牌数据
            CardData cardData = cardDataProvider.GetCardDataById(cardId);
            if (cardData == null)
            {
                Debug.LogError($"找不到ID为 {cardId} 的卡牌数据");
                return;
            }
            
            // 创建卡牌
            Card card = new Card(cardData, position, ownerId, isFaceDown);
            
            // 添加到卡牌管理器
            cardManager.AddCard(card, position);
            
            Debug.Log($"在位置 {position} 生成卡牌: {cardData.Name}, 所有者: {ownerId}, 背面: {isFaceDown}");
        }
    }
} 