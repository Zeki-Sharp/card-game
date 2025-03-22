using UnityEngine;
using System.Collections;

namespace ChessGame
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] private Board board;
        [SerializeField] private CardManager cardManager;
        [SerializeField] private int initialCardCount = 3;
        
        private void Start()
        {
            Debug.Log("GameController.Start: 开始初始化");
            
            if (board == null)
            {
                board = FindObjectOfType<Board>();
                Debug.Log($"通过FindObjectOfType找到Board: {(board != null ? "成功" : "失败")}");
            }
            
            if (cardManager == null)
            {
                cardManager = GetComponent<CardManager>();
                if (cardManager == null)
                    cardManager = FindObjectOfType<CardManager>();
                Debug.Log($"找到CardManager: {(cardManager != null ? "成功" : "失败")}");
            }
            
            // 检查状态机是否初始化
            if (cardManager != null)
            {
                Debug.Log("检查CardManager的状态机是否初始化");
                cardManager.CheckStateMachine();
            }
            
            // 使用协程延迟生成卡牌
            StartCoroutine(SpawnInitialCardsDelayed());
        }
        
        private IEnumerator SpawnInitialCardsDelayed()
        {
            // 等待一帧，确保棋盘已初始化
            yield return null;
            
            // 生成初始卡牌
            if (cardManager != null && board != null)
            {
                cardManager.SpawnRandomCards(initialCardCount);
            }
        }
        
        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mousePos.z = 0;
                Debug.Log($"鼠标点击位置: {mousePos}");
                
                // 简单的点击测试
                RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
                if (hit.collider != null)
                {
                    Debug.Log($"点击到了: {hit.collider.gameObject.name}, 层级: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
                }
                else
                {
                    Debug.Log("没有点击到任何对象");
                }
                
                // 检测所有碰撞体
                RaycastHit2D[] hits = Physics2D.RaycastAll(mousePos, Vector2.zero);
                Debug.Log($"检测到 {hits.Length} 个碰撞体");
                
                // 首先尝试找到Card
                CardView cardView = null;
                CellView cellView = null;
                
                foreach (var _hit in hits)
                {
                    Debug.Log($"碰撞体: {_hit.collider.gameObject.name}, 层级: {LayerMask.LayerToName(_hit.collider.gameObject.layer)}");
                    
                    // 优先检查是否点击到了卡牌
                    if (cardView == null)
                    {
                        cardView = _hit.collider.GetComponent<CardView>();
                        if (cardView != null)
                            Debug.Log($"找到CardView: {cardView.name}");
                    }
                    
                    // 然后检查是否点击到了格子
                    if (cellView == null)
                    {
                        cellView = _hit.collider.GetComponent<CellView>();
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
            
            // 按空格键生成新卡牌
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (cardManager != null)
                {
                    cardManager.SpawnRandomCards(1);
                }
            }
            
            // 按R键重置所有卡牌的行动状态
            if (Input.GetKeyDown(KeyCode.R))
            {
                if (cardManager != null)
                {
                    cardManager.ResetAllCardActions();
                }
            }
        }
    }
} 