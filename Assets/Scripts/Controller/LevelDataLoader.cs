using System.Collections.Generic;
using UnityEngine;

namespace ChessGame
{
    public class LevelDataLoader : MonoBehaviour
    {
        [SerializeField] private CardDataProvider cardDataProvider;
        [SerializeField] private CardInitializer cardInitializer;
        [SerializeField] private LevelCardConfiguration levelConfig;
        
        private void Awake()
        {
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
            if (levelConfig == null)
            {
                Debug.LogError("未设置关卡配置");
                return;
            }
            
            // 清空当前棋盘
            CardManager cardManager = FindObjectOfType<CardManager>();
            if (cardManager != null)
            {
                cardManager.ClearAllCards();
            }
            
            // 加载玩家卡牌
            LoadPlayerCards();
            
            // 加载敌方卡牌
            LoadEnemyCards();
            
            Debug.Log("关卡加载完成");
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
            bool isFaceDown = entry.isFaceDown;
            
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
                    cardInitializer.SpawnCardAt(position, entry.cardId, entry.ownerId, entry.isFaceDown);
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
                        cardInitializer.SpawnCardAt(position, entry.cardId, entry.ownerId, entry.isFaceDown);
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