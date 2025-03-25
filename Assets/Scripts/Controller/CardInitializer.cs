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
        [SerializeField] private int initialPlayerCards = 3;
        [SerializeField] private int initialEnemyCards = 3;
        [SerializeField] private int initialPlayerFaceUpCards = 1; // 初始正面朝上的玩家卡牌数量
        [SerializeField] private int initialEnemyFaceUpCards = 1; // 初始正面朝上的敌方卡牌数量
        
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
        
        // 或者添加一个标志防止重复初始化
        private bool _hasInitialized = false;

        private void Start()
        {
            // 只有在未初始化时才执行
            if (!_hasInitialized)
            {
                StartCoroutine(InitializeCardsDelayed());
            }
        }
        
        // 修改InitializeCardsDelayed方法
        private IEnumerator InitializeCardsDelayed()
        {
            yield return new WaitForSeconds(0.5f);
            
            if (!_hasInitialized)
            {
                // 生成初始卡牌
                SpawnInitialCards();
                _hasInitialized = true;
            }
        }
        
        // 修改SpawnInitialCards方法
        public void SpawnInitialCards()
        {
            Debug.Log($"SpawnInitialCards被调用，调用堆栈: {new System.Diagnostics.StackTrace()}");
            
            // 防止重复初始化
            if (_hasInitialized)
            {
                Debug.LogWarning("卡牌已经初始化过，忽略重复调用");
                return;
            }
            
            Debug.Log("生成初始卡牌");
            
            // 生成玩家卡牌（第一张正面朝上）
            SpawnPlayerCards(initialPlayerCards);
            
            // 生成敌方卡牌（第一张正面朝上）
            SpawnEnemyCards(initialEnemyCards);
            
            _hasInitialized = true;
        }
        
        // 生成玩家卡牌
        private void SpawnPlayerCards(int count)
        {
            if (count <= 0) return;
            
            // 获取所有可用卡牌数据
            List<CardData> availableCards = cardDataProvider.GetAllCardData();
            if (availableCards.Count == 0)
            {
                Debug.LogError("没有可用的卡牌数据");
                return;
            }
            
            // 过滤出玩家卡牌
            List<CardData> playerCards = availableCards.FindAll(card => card.Faction == 0);
            if (playerCards.Count == 0)
            {
                Debug.LogError("没有玩家阵营的卡牌数据");
                return;
            }
            
            // 计算生成区域
            int boardWidth = cardManager.BoardWidth;
            int boardHeight = cardManager.BoardHeight;
            int startY = 0;
            int endY = boardHeight / 2 - 1;
            
            // 尝试生成第一张正面朝上的卡牌
            bool firstCardSpawned = false;
            int attempts = 0;
            int maxAttempts = 50;
            
            while (!firstCardSpawned && attempts < maxAttempts)
            {
                attempts++;
                
                // 随机选择一个位置
                int x = Random.Range(0, boardWidth);
                int y = Random.Range(startY, endY + 1);
                Vector2Int position = new Vector2Int(x, y);
                
                // 检查位置是否已被占用
                if (cardManager.GetCard(position) != null)
                    continue;
                    
                // 随机选择一张卡牌
                CardData cardData = playerCards[Random.Range(0, playerCards.Count)];
                
                // 生成正面朝上的卡牌
                SpawnCard(cardData, position, false); // false表示正面朝上
                
                firstCardSpawned = true;
                Debug.Log($"生成玩家正面卡牌: {cardData.Name}, 位置: {position}");
            }
            
            // 生成剩余的背面朝上的卡牌
            SpawnRandomCards(count - 1, 0); // 0表示玩家阵营
        }
        
        // 生成敌方卡牌
        private void SpawnEnemyCards(int count)
        {
            if (count <= 0) return;
            
            // 获取所有可用卡牌数据
            List<CardData> availableCards = cardDataProvider.GetAllCardData();
            if (availableCards.Count == 0)
            {
                Debug.LogError("没有可用的卡牌数据");
                return;
            }
            
            // 过滤出敌方卡牌
            List<CardData> enemyCards = availableCards.FindAll(card => card.Faction == 1);
            if (enemyCards.Count == 0)
            {
                Debug.LogError("没有敌方阵营的卡牌数据");
                return;
            }
            
            // 计算生成区域
            int boardWidth = cardManager.BoardWidth;
            int boardHeight = cardManager.BoardHeight;
            int startY = boardHeight - 1;
            int endY = boardHeight - 1;
            
            // 尝试生成第一张正面朝上的卡牌
            bool firstCardSpawned = false;
            int attempts = 0;
            int maxAttempts = 50;
            
            while (!firstCardSpawned && attempts < maxAttempts)
            {
                attempts++;
                
                // 随机选择一个位置
                int x = Random.Range(0, boardWidth);
                int y = startY; // 敌方卡牌在顶部
                Vector2Int position = new Vector2Int(x, y);
                
                // 检查位置是否已被占用
                if (cardManager.GetCard(position) != null)
                    continue;
                    
                // 随机选择一张卡牌
                CardData cardData = enemyCards[Random.Range(0, enemyCards.Count)];
                
                // 生成正面朝上的卡牌
                SpawnCard(cardData, position, false); // false表示正面朝上
                
                firstCardSpawned = true;
                Debug.Log($"生成敌方正面卡牌: {cardData.Name}, 位置: {position}");
            }
            
            // 生成剩余的背面朝上的卡牌
            SpawnRandomCards(count - 1, 1); // 1表示敌方阵营
        }
        
        // 生成随机卡牌
        public void SpawnRandomCards(int count, int ownerId = 0)
        {
            if (cardManager == null || cardDataProvider == null)
            {
                Debug.LogError("CardManager或CardDataProvider未设置");
                return;
            }
            
            Debug.Log($"生成 {count} 张随机卡牌，所有者ID: {ownerId}");
            
            // 获取所有可用卡牌数据
            List<CardData> availableCards = cardDataProvider.GetAllCardData();
            if (availableCards.Count == 0)
            {
                Debug.LogError("没有可用的卡牌数据");
                return;
            }
            
            // 计算生成区域
            int boardWidth = cardManager.BoardWidth;
            int boardHeight = cardManager.BoardHeight;
            
            int startY = (ownerId == 0) ? 0 : boardHeight - 1;
            int endY = (ownerId == 0) ? boardHeight / 2 - 1 : boardHeight - 1;
            
            // 尝试生成指定数量的卡牌
            int cardsSpawned = 0;
            int maxAttempts = count * 10; // 防止无限循环
            int attempts = 0;
            
            while (cardsSpawned < count && attempts < maxAttempts)
            {
                attempts++;
                
                // 随机选择一个位置
                int x = Random.Range(0, boardWidth);
                int y = Random.Range(startY, endY + 1);
                Vector2Int position = new Vector2Int(x, y);
                
                // 检查位置是否已被占用
                if (cardManager.GetCard(position) != null)
                    continue;
                    
                // 随机选择一张卡牌
                CardData cardData = availableCards[Random.Range(0, availableCards.Count)];
                
                // 生成卡牌
                SpawnCard(cardData, position, true);
                
                cardsSpawned++;
            }
            
            Debug.Log($"成功生成 {cardsSpawned} 张卡牌，尝试次数: {attempts}");
        }
        
        // 生成卡牌
        public void SpawnCard(CardData cardData, Vector2Int position, bool isFaceDown = true)
        {
            if (cardManager == null)
            {
                Debug.LogError("CardManager未设置");
                return;
            }
            
            // 检查位置是否已被占用
            if (cardManager.GetCard(position) != null)
            {
                Debug.LogWarning($"位置 {position} 已被占用，无法生成卡牌");
                return;
            }
            
            // 确定卡牌所有者（根据卡牌数据的阵营）
            int ownerId = cardData.Faction;
            
            // 创建卡牌
            Card card = new Card(cardData, position, ownerId, isFaceDown);
            
            // 添加到卡牌管理器
            cardManager.AddCard(card, position);
            
            Debug.Log($"生成卡牌: {cardData.Name}, 位置: {position}, 所有者: {ownerId}, 背面: {isFaceDown}");
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
            
            // 获取所有可用卡牌数据
            List<CardData> availableCards = cardDataProvider.GetAllCardData();
            
            // 查找指定ID的卡牌数据
            CardData cardData = availableCards.Find(card => card.Id == cardId);
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