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
        [SerializeField] private int initialCardCount = 3;
        
        // 游戏状态
        private bool _isGameInitialized = false;
        private bool _isGameOver = false;
        
        private void Start()
        {
            Debug.Log("GameController.Start: 开始初始化游戏");
            
            // 初始化组件
            InitializeComponents();
            
            // 使用协程延迟生成卡牌
            StartCoroutine(InitializeGameDelayed());
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
            
            // 检查状态机是否初始化
            if (cardManager != null)
            {
                Debug.Log("检查CardManager的状态机是否初始化");
                cardManager.CheckStateMachine();
            }
        }
        
        // 延迟初始化游戏
        private IEnumerator InitializeGameDelayed()
        {
            // 等待一帧，确保所有组件都已初始化
            yield return null;
            
            // 生成初始卡牌
            if (cardManager != null && board != null)
            {
                cardManager.SpawnRandomCards(initialCardCount);
            }
            
            // 标记游戏已初始化
            _isGameInitialized = true;
            
            Debug.Log("游戏初始化完成");
        }
        
        private void Update()
        {
            // 如果游戏结束，不处理输入
            if (_isGameOver)
                return;
                
            // 处理鼠标点击
            HandleMouseClick();
            
            // 处理键盘输入
            HandleKeyboardInput();
            
            // 检查游戏是否结束
            CheckGameOver();
        }
        
        // 处理鼠标点击
        private void HandleMouseClick()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mousePos.z = 0;
                Debug.Log($"鼠标点击位置: {mousePos}");
                
                // 检测所有碰撞体
                RaycastHit2D[] hits = Physics2D.RaycastAll(mousePos, Vector2.zero);
                Debug.Log($"检测到 {hits.Length} 个碰撞体");
                
                // 首先尝试找到Card
                CardView cardView = null;
                CellView cellView = null;
                
                foreach (var hit in hits)
                {
                    Debug.Log($"碰撞体: {hit.collider.gameObject.name}, 层级: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
                    
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
        
        // 检查游戏是否结束
        private void CheckGameOver()
        {
            if (!_isGameInitialized)
                return;
                
            // 检查是否有玩家卡牌
            bool hasPlayerCards = cardManager.HasPlayerCards();
            
            // 检查是否有敌方卡牌
            bool hasEnemyCards = cardManager.HasEnemyCards();
            
            // 如果一方没有卡牌，游戏结束
            if (!hasPlayerCards || !hasEnemyCards)
            {
                _isGameOver = true;
                
                if (!hasPlayerCards)
                {
                    Debug.Log("游戏结束：玩家失败");
                    // TODO: 显示失败界面
                }
                else
                {
                    Debug.Log("游戏结束：玩家胜利");
                    // TODO: 显示胜利界面
                }
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
    }
} 