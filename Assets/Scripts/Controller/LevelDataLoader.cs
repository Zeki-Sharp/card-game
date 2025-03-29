using System.Collections.Generic;
using UnityEngine;

namespace ChessGame
{
    public class LevelDataLoader : MonoBehaviour
    {
        [SerializeField] private CardDataProvider cardDataProvider;
        [SerializeField] private CardInitializer cardInitializer;
        [SerializeField] private LevelCardConfiguration levelConfig;
        
        private static LevelDataLoader _instance;

        private void Awake()
        {
            // 单例模式检查
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("场景中存在多个LevelDataLoader实例，销毁重复的: " + gameObject.name);
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            if (cardDataProvider == null)
            {
                cardDataProvider = FindObjectOfType<CardDataProvider>();
                if (cardDataProvider == null)
                {
                    Debug.LogError("找不到CardDataProvider组件");
                    return;
                }
            }
            
            if (cardInitializer == null)
            {
                cardInitializer = FindObjectOfType<CardInitializer>();
                if (cardInitializer == null)
                {
                    Debug.LogError("找不到CardInitializer组件");
                    return;
                }
            }
        }
        
        public void LoadLevel()
        {
            Debug.Log("LevelDataLoader.LoadLevel 开始执行");
            
            if (levelConfig == null)
            {
                Debug.LogError("未设置关卡配置");
                return;
            }
            
            // 清空当前棋盘
            CardManager cardManager = FindObjectOfType<CardManager>();
            if (cardManager != null)
            {
                Debug.Log("清空所有现有卡牌");
                cardManager.ClearAllCards();
            }
            
            // 禁用CardInitializer的自动初始化
            if (cardInitializer != null)
            {
                Debug.Log("禁用CardInitializer的自动初始化");
                cardInitializer.enabled = false;
            }
            
            // 加载玩家卡牌
            Debug.Log("开始加载玩家卡牌");
            LoadPlayerCards();
            
            // 加载敌方卡牌
            Debug.Log("开始加载敌方卡牌");
            LoadEnemyCards();
            
            Debug.Log("关卡加载完成，总共加载了：" + 
                      levelConfig.PlayerCards.Count + " 种玩家卡牌和 " + 
                      levelConfig.EnemyCards.Count + " 种敌方卡牌");
        }
        
        private void LoadPlayerCards()
        {
            foreach (LevelCardEntry entry in levelConfig.PlayerCards)
            {
                SpawnCards(entry);
            }
        }
        
        private void LoadEnemyCards()
        {
            foreach (LevelCardEntry entry in levelConfig.EnemyCards)
            {
                SpawnCards(entry);
            }
        }
        
        private void SpawnCards(LevelCardEntry entry)
        {
            // 获取卡牌数据
            CardData cardData = cardDataProvider.GetCardDataById(entry.cardId);
            if (cardData == null)
            {
                Debug.LogError($"找不到ID为 {entry.cardId} 的卡牌数据");
                return;
            }
            
            // 获取棋盘尺寸
            CardManager cardManager = FindObjectOfType<CardManager>();
            if (cardManager == null)
            {
                Debug.LogError("找不到CardManager组件");
                return;
            }
            
            int boardWidth = cardManager.BoardWidth;
            int boardHeight = cardManager.BoardHeight;
            
            // 生成指定数量的卡牌
            int count = entry.count;
            int ownerId = entry.ownerId;
            
            // 修正这里的逻辑：
            // 如果isFaceDown为true，则所有卡牌都背面朝上
            // 如果isFaceDown为false，则根据faceUpCount决定有多少张卡牌正面朝上
            
            // 如果指定了位置，则在指定位置生成卡牌
            if (entry.positions != null && entry.positions.Length > 0)
            {
                int positionIndex = 0;
                for (int i = 0; i < count; i++)
                {
                    // 如果位置数组不够长，循环使用
                    if (positionIndex >= entry.positions.Length)
                    {
                        positionIndex = 0;
                    }
                    
                    Vector2Int position = entry.positions[positionIndex];
                    
                    // 决定这张卡是否正面朝上
                    bool cardIsFaceDown;
                    if (entry.isFaceDown)
                    {
                        // 如果配置为背面朝上，则所有卡牌都背面朝上
                        cardIsFaceDown = true;
                    }
                    else
                    {
                        // 如果配置为正面朝上，则根据faceUpCount决定
                        cardIsFaceDown = (i >= entry.faceUpCount);
                    }
                    
                    Debug.Log($"生成卡牌 ID:{entry.cardId}, 位置:{position}, 所有者:{ownerId}, 背面朝上:{cardIsFaceDown}, " +
                              $"配置:{entry.isFaceDown}, faceUpCount:{entry.faceUpCount}, 索引:{i}");
                    
                    cardInitializer.SpawnCardAt(position, entry.cardId, entry.ownerId, cardIsFaceDown);
                    positionIndex++;
                }
            }
            else
            {
                // 随机生成卡牌
                for (int i = 0; i < count; i++)
                {
                    Vector2Int position = GetRandomPosition(ownerId, boardWidth, boardHeight);
                    if (position.x >= 0) // 有效位置
                    {
                        // 决定这张卡是否正面朝上
                        bool cardIsFaceDown;
                        if (entry.isFaceDown)
                        {
                            // 如果配置为背面朝上，则所有卡牌都背面朝上
                            cardIsFaceDown = true;
                        }
                        else
                        {
                            // 如果配置为正面朝上，则根据faceUpCount决定
                            cardIsFaceDown = (i >= entry.faceUpCount);
                        }
                        
                        Debug.Log($"随机生成卡牌 ID:{entry.cardId}, 位置:{position}, 所有者:{ownerId}, 背面朝上:{cardIsFaceDown}, " +
                                  $"配置:{entry.isFaceDown}, faceUpCount:{entry.faceUpCount}, 索引:{i}");
                        
                        cardInitializer.SpawnCardAt(position, entry.cardId, entry.ownerId, cardIsFaceDown);
                    }
                }
            }
        }
        
        private Vector2Int GetRandomPosition(int ownerId, int boardWidth, int boardHeight)
        {
            CardManager cardManager = FindObjectOfType<CardManager>();
            if (cardManager == null)
            {
                Debug.LogError("找不到CardManager组件");
                return new Vector2Int(-1, -1);
            }
            
            // 确定位置范围
            int startY, endY;
            if (ownerId == 0) // 玩家
            {
                startY = 0;
                endY = boardHeight / 2 - 1;
            }
            else // 敌方
            {
                startY = boardHeight / 2;
                endY = boardHeight - 1;
            }
            
            // 随机选择位置
            int maxAttempts = 20;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                int x = Random.Range(0, boardWidth);
                int y = Random.Range(startY, endY + 1);
                Vector2Int position = new Vector2Int(x, y);
                
                // 检查位置是否已被占用
                if (cardManager.GetCard(position) == null)
                {
                    return position;
                }
            }
            
            Debug.LogWarning("无法找到空闲位置");
            return new Vector2Int(-1, -1);
        }
    }
} 