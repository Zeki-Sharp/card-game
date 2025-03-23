using System.Collections.Generic;
using UnityEngine;

namespace ChessGame
{
    /// <summary>
    /// 卡牌高亮服务 - 负责处理卡牌和格子的高亮显示
    /// </summary>
    public class CardHighlightService : MonoBehaviour
    {
        [SerializeField] private CardManager cardManager;
        [SerializeField] private Board board;
        
        private void Awake()
        {
            if (cardManager == null)
                cardManager = FindObjectOfType<CardManager>();
                
            if (board == null)
                board = FindObjectOfType<Board>();
        }
        
        private void Start()
        {
            // 订阅卡牌管理器的事件
            if (cardManager != null)
            {
                Debug.Log("CardHighlightService: 订阅CardManager事件");
                cardManager.OnCardSelected += HighlightSelectedPosition;
                cardManager.OnCardSelected += HighlightMovablePositions;
                cardManager.OnCardSelected += HighlightAttackableCards;
                cardManager.OnCardDeselected += ClearAllHighlights;
            }
            else
            {
                Debug.LogError("CardHighlightService: cardManager引用为空");
            }
        }
        
        private void OnDestroy()
        {
            // 取消订阅事件
            if (cardManager != null)
            {
                cardManager.OnCardSelected -= HighlightSelectedPosition;
                cardManager.OnCardSelected -= HighlightMovablePositions;
                cardManager.OnCardSelected -= HighlightAttackableCards;
                cardManager.OnCardDeselected -= ClearAllHighlights;
            }
        }
        
        // 高亮选中的卡牌位置
        public void HighlightSelectedPosition(Vector2Int position)
        {
            Debug.Log($"高亮选中位置: {position}");
            
            Card card = cardManager.GetCard(position);
            if (card == null || card.OwnerId != 0) return;
            
            // 高亮选中的格子
            CellView cellView = board.GetCellView(position.x, position.y);
            if (cellView != null)
            {
                cellView.SetHighlight(CellView.HighlightType.Selected);
            }
        }
        
        // 高亮可移动的位置
        public void HighlightMovablePositions(Vector2Int position)
        {
            Debug.Log($"高亮可移动位置，选中位置: {position}");
            
            Card card = cardManager.GetCard(position);
            if (card == null || card.OwnerId != 0) return;
            
            // 如果卡牌已经行动过或是背面状态，不显示可移动位置
            if (card.HasActed || card.IsFaceDown)
                return;
                
            // 如果不是玩家回合或不是玩家卡牌，不显示可移动位置
            TurnManager turnManager = cardManager.GetTurnManager();
            if (turnManager != null && (!turnManager.IsPlayerTurn() || card.OwnerId != 0))
                return;
                
            int moveRange = cardManager.MoveRange;
            
            // 高亮可移动的格子
            for (int x = position.x - moveRange; x <= position.x + moveRange; x++)
            {
                for (int y = position.y - moveRange; y <= position.y + moveRange; y++)
                {
                    // 检查是否在棋盘范围内
                    if (x >= 0 && x < cardManager.BoardWidth && y >= 0 && y < cardManager.BoardHeight)
                    {
                        // 计算曼哈顿距离
                        int distance = Mathf.Abs(x - position.x) + Mathf.Abs(y - position.y);
                        if (distance <= moveRange && distance > 0)
                        {
                            Vector2Int targetPos = new Vector2Int(x, y);
                            
                            // 检查目标位置是否有卡牌
                            if (!cardManager.HasCard(targetPos))
                            {
                                CellView cellView = board.GetCellView(x, y);
                                if (cellView != null)
                                {
                                    cellView.SetHighlight(CellView.HighlightType.Move);
                                }
                            }
                        }
                    }
                }
            }
        }
        
        // 高亮可攻击的卡牌
        public void HighlightAttackableCards(Vector2Int position)
        {
            Debug.Log($"高亮可攻击卡牌，选中位置: {position}");
            
            Card card = cardManager.GetCard(position);
            if (card == null || card.OwnerId != 0) return;
            
            // 如果卡牌已经行动过或是背面状态，不显示可攻击卡牌
            if (card.HasActed || card.IsFaceDown)
                return;
                
            // 如果不是玩家回合或不是玩家卡牌，不显示可攻击卡牌
            TurnManager turnManager = cardManager.GetTurnManager();
            if (turnManager != null && (!turnManager.IsPlayerTurn() || card.OwnerId != 0))
                return;
                
            int attackRange = cardManager.AttackRange;
            
            // 高亮可攻击的卡牌
            for (int x = position.x - attackRange; x <= position.x + attackRange; x++)
            {
                for (int y = position.y - attackRange; y <= position.y + attackRange; y++)
                {
                    // 检查是否在棋盘范围内
                    if (x >= 0 && x < cardManager.BoardWidth && y >= 0 && y < cardManager.BoardHeight)
                    {
                        // 计算曼哈顿距离
                        int distance = Mathf.Abs(x - position.x) + Mathf.Abs(y - position.y);
                        if (distance <= attackRange && distance > 0)
                        {
                            Vector2Int targetPos = new Vector2Int(x, y);
                            Card targetCard = cardManager.GetCard(targetPos);
                            
                            // 检查目标位置是否有卡牌，且不是己方卡牌
                            if (targetCard != null && (targetCard.OwnerId != card.OwnerId || targetCard.IsFaceDown))
                            {
                                CellView cellView = board.GetCellView(x, y);
                                if (cellView != null)
                                {
                                    cellView.SetHighlight(CellView.HighlightType.Attack);
                                }
                            }
                        }
                    }
                }
            }
        }
        
        // 清除所有高亮
        public void ClearAllHighlights()
        {
            Debug.Log("清除所有高亮");
            
            for (int x = 0; x < cardManager.BoardWidth; x++)
            {
                for (int y = 0; y < cardManager.BoardHeight; y++)
                {
                    CellView cellView = board.GetCellView(x, y);
                    if (cellView != null)
                    {
                        cellView.SetHighlight(CellView.HighlightType.None);
                    }
                }
            }
        }
    }
} 