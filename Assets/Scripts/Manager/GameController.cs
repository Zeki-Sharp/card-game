using UnityEngine;
using System.Collections;

namespace ChessGame
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] private Board board;
        [SerializeField] private CardManager cardManager;
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private AIController aiController;
        [SerializeField] private GameStateManager gameStateManager;
        [SerializeField] private GameEndChecker gameEndChecker;
        
        // 游戏状态
        private bool _isGameInitialized = false;
        private bool _isGameOver = false;
        private bool _cardsInitialized = false;
        
        private void Awake()
        {
            // 确保GameEventSystem实例存在
            if (GameEventSystem.Instance == null)
            {
                Debug.LogError("GameEventSystem实例不存在，将自动创建");
            }
            
            // ... 其他初始化代码 ...
        }
        
        private void Start()
        {
            Debug.Log("GameController.Start: 开始初始化游戏");
            
            // 初始化组件
            InitializeComponents();
            
            // 使用LevelDataLoader加载关卡
            LevelDataLoader levelLoader = FindObjectOfType<LevelDataLoader>();
            if (levelLoader != null)
            {
                Debug.Log("使用LevelDataLoader加载关卡");
                //levelLoader.LoadLevel();
            }
            else
            {
                Debug.LogWarning("找不到LevelDataLoader，游戏将不会初始化卡牌");
            }
        }
        
        // 初始化组件
        private void InitializeComponents()
        {
            // 初始化Board
            if (board == null)
            {
                board = FindObjectOfType<Board>();
                Debug.Log($"找到Board: {(board != null ? "成功" : "失败")}");
            }
            
            // 初始化CardManager
            if (cardManager == null)
            {
                cardManager = FindObjectOfType<CardManager>();
                Debug.Log($"找到CardManager: {(cardManager != null ? "成功" : "失败")}");
            }
            
            // 初始化TurnManager
            if (turnManager == null)
            {
                turnManager = FindObjectOfType<TurnManager>();
                Debug.Log($"找到TurnManager: {(turnManager != null ? "成功" : "失败")}");
            }
            
            // 初始化AIController
            if (aiController == null)
            {
                aiController = FindObjectOfType<AIController>();
                Debug.Log($"找到AIController: {(aiController != null ? "成功" : "失败")}");
            }
            
            // 初始化GameStateManager
            if (gameStateManager == null)
            {
                gameStateManager = FindObjectOfType<GameStateManager>();
                Debug.Log($"找到GameStateManager: {(gameStateManager != null ? "成功" : "失败")}");
            }
            
            // 初始化GameEndChecker
            if (gameEndChecker == null)
            {
                gameEndChecker = FindObjectOfType<GameEndChecker>();
                Debug.Log($"找到GameEndChecker: {(gameEndChecker != null ? "成功" : "失败")}");
            }
            
            // 检查状态机是否初始化
            if (cardManager != null)
            {
                Debug.Log("检查CardManager的状态机是否初始化");
                cardManager.CheckStateMachine();
            }
        }
        
        private void Update()
        {
            // 处理鼠标点击
            HandleMouseClick();
            
            // 处理键盘输入
            HandleKeyboardInput();
        }
        
        // 处理鼠标点击
        private void HandleMouseClick()
        {
            if (Input.GetMouseButtonDown(0))
            {
                // 从摄像机发射射线到鼠标位置
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 1f); // 添加射线可视化
                
                Debug.Log($"发射射线: 起点={ray.origin}, 方向={ray.direction}");
                
                RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
                Debug.Log($"检测到 {hits.Length} 个碰撞体");
                
                // 首先尝试找到Card
                CardView cardView = null;
                CellView cellView = null;
                
                foreach (var hit in hits)
                {
                    Debug.Log($"碰撞体: {hit.collider.gameObject.name}, 距离: {hit.distance}");
                    
                    // 优先检查是否点击到了卡牌
                    if (cardView == null)
                    {
                        cardView = hit.collider.GetComponent<CardView>();
                        if (cardView != null)
                            Debug.Log($"找到CardView: {cardView.name}");
                    }
                    
                    // 然后检查是否点击到了格子
                    if (cellView == null)
                    {
                        cellView = hit.collider.GetComponent<CellView>();
                        if (cellView != null)
                            Debug.Log($"找到CellView: {cellView.name}");
                    }
                    
                    // 如果都找到了，就可以跳出循环
                    if (cardView != null && cellView != null)
                        break;
                }
                
                // 如果点击到了卡牌，优先处理卡牌点击
                if (cardView != null)
                {
                    try
                    {
                        Card card = cardView.GetCard();
                        if (card != null)
                        {
                            Vector2Int position = card.Position;
                            Debug.Log($"处理卡牌点击: {position}");
                            cardManager.HandleCellClick(position);
                        }
                        else
                        {
                            Debug.LogWarning("卡牌视图没有关联的卡牌数据");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"处理卡牌点击时出错: {e.Message}");
                    }
                }
                // 否则，如果点击到了格子，处理格子点击
                else if (cellView != null)
                {
                    Vector2Int position = cellView.GetCell().Position;
                    Debug.Log($"处理格子点击: {position}");
                    cardManager.HandleCellClick(position);
                }
                else
                {
                    Debug.Log("没有找到CellView或CardView");
                }
            }
        }
        
        // 处理键盘输入
        private void HandleKeyboardInput()
        {
            // 按空格键结束当前回合（测试用）
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (turnManager != null && turnManager.IsPlayerTurn())
                {
                    Debug.Log("手动结束玩家回合");
                    turnManager.EndPlayerTurn();
                }
            }
            
            // 按R键重置所有卡牌的行动状态
            if (Input.GetKeyDown(KeyCode.R))
            {
                if (cardManager != null)
                {
                    Debug.Log("重置所有卡牌的行动状态");
                    cardManager.ResetAllCardActions();
                }
            }
            
            // 按ESC键暂停/继续游戏
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
        }
        
        // 暂停/继续游戏
        private void TogglePause()
        {
            if (Time.timeScale == 0)
            {
                // 继续游戏
                Time.timeScale = 1;
                Debug.Log("游戏继续");
            }
            else
            {
                // 暂停游戏
                Time.timeScale = 0;
                Debug.Log("游戏暂停");
            }
        }
        
        // 重置游戏
        public void RestartGame()
        {
            Debug.Log("重新开始游戏");
            
            // 重置游戏状态
            _isGameOver = false;
            
            // 清空棋盘
            if (cardManager != null)
            {
                cardManager.ClearAllCards();
            }
            
            // 重新加载关卡
            LevelDataLoader levelLoader = FindObjectOfType<LevelDataLoader>();
            if (levelLoader != null)
            {
                levelLoader.LoadLevelByIndex(1); // 加载第一关
            }
            
            // 重置回合管理器
            if (turnManager != null)
            {
                turnManager.ResetTurns();
                turnManager.ResetTurnCount();
                turnManager.enabled = true; // 确保回合管理器被启用
            }
            
            // 重置游戏结束检查器
            if (gameEndChecker != null)
            {
                gameEndChecker.ResetGame();
            }
        }
    }
} 