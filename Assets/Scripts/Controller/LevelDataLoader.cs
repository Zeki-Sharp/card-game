using System.Collections.Generic;
using UnityEngine;

namespace ChessGame
{
    public class LevelDataLoader : MonoBehaviour
    {
        [SerializeField] private CardDataProvider cardDataProvider;
        [SerializeField] private CardInitializer cardInitializer;
        [SerializeField] private string levelCSVPathFormat = "Levels/Level{0}"; // 关卡CSV路径格式，{0}将被替换为关卡索引
        [SerializeField] private int currentLevelIndex = 1; // 当前关卡索引
        
        private static LevelDataLoader _instance;
        private LevelCardConfiguration _currentLevelConfig;

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
            
            // 默认加载当前关卡
            LoadLevelByIndex(currentLevelIndex);
        }
        
        // 加载指定索引的关卡
        public void LoadLevelByIndex(int levelIndex)
        {
            Debug.Log($"加载关卡索引: {levelIndex}");
            currentLevelIndex = levelIndex;
            
            // 使用格式化的路径加载CSV
            string formattedPath = string.Format(levelCSVPathFormat, levelIndex);
            LoadLevelFromCSV(formattedPath);
        }

        // 加载下一个关卡
        public void LoadNextLevel()
        {
            LoadLevelByIndex(currentLevelIndex + 1);
        }

        // 从CSV加载关卡配置
        private void LoadLevelFromCSV(string csvPath)
        {
            Debug.Log($"从CSV加载关卡配置: {csvPath}");
            
            // 从CSV读取配置
            _currentLevelConfig = LevelCardConfigurationCSVReader.ReadFromCSV(csvPath);
            
            if (_currentLevelConfig != null)
            {
                // 加载关卡
                LoadLevel();
            }
            else
            {
                Debug.LogError($"从CSV加载配置失败: {csvPath}");
            }
        }
        
        // 加载当前关卡配置
        private void LoadLevel()
        {
            Debug.Log("LevelDataLoader.LoadLevel 开始执行");
            
            if (_currentLevelConfig == null)
            {
                Debug.LogError("未加载关卡配置");
                return;
            }
            
            // 获取 Board 并确保它已初始化
            Board board = FindObjectOfType<Board>();
            if (board == null)
            {
                Debug.LogError("找不到 Board 组件，无法加载关卡");
                return;
            }
            
            // 如果 Board 尚未初始化，等待初始化完成
            if (!board.IsInitialized)
            {
                Debug.Log("Board 尚未初始化，等待初始化完成后再加载关卡");
                board.OnBoardInitialized += () => {
                    Debug.Log("Board 初始化完成，现在开始加载关卡");
                    ContinueLoadLevel();
                };
                return;
            }
            
            // Board 已初始化，直接继续加载
            ContinueLoadLevel();
        }
        
        // 分离出实际的加载逻辑，以便在 Board 初始化后调用
        private void ContinueLoadLevel()
        {
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
                      _currentLevelConfig.PlayerCards.Count + " 种玩家卡牌和 " + 
                      _currentLevelConfig.EnemyCards.Count + " 种敌方卡牌");
        }
        
        private void LoadPlayerCards()
        {
            foreach (LevelCardEntry entry in _currentLevelConfig.PlayerCards)
            {
                SpawnCards(entry);
            }
        }
        
        private void LoadEnemyCards()
        {
            foreach (LevelCardEntry entry in _currentLevelConfig.EnemyCards)
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
            
            // 使用CardData的Faction作为ownerId
            int ownerId = cardData.Faction;
            
            // 获取棋盘尺寸
            Board board = FindObjectOfType<Board>();
            int boardWidth = board != null ? board.Width : 4;
            int boardHeight = board != null ? board.Height : 6;
            
            // 如果指定了位置，使用指定位置
            if (entry.positions != null && entry.positions.Length > 0)
            {
                int count = Mathf.Min(entry.count, entry.positions.Length);
                for (int i = 0; i < count; i++)
                {
                    Vector2Int position = entry.positions[i];
                    
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
                    
                    Debug.Log($"在指定位置生成卡牌 ID:{entry.cardId}, 位置:{position}, 所有者:{ownerId}, 背面朝上:{cardIsFaceDown}");
                    
                    cardInitializer.SpawnCardAt(position, entry.cardId, ownerId, cardIsFaceDown);
                }
            }
            // 否则随机放置
            else
            {
                for (int i = 0; i < entry.count; i++)
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
                        
                        Debug.Log($"随机生成卡牌 ID:{entry.cardId}, 位置:{position}, 所有者:{ownerId}, 背面朝上:{cardIsFaceDown}");
                        
                        cardInitializer.SpawnCardAt(position, entry.cardId, ownerId, cardIsFaceDown);
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