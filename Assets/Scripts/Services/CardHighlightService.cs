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
                
                // 订阅事件
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
            
            // 如果不是玩家回合，不显示可移动位置
            TurnManager turnManager = cardManager.GetTurnManager();
            if (turnManager != null && !turnManager.IsPlayerTurn())
                return;
                
            // 获取可移动的位置
            List<Vector2Int> movablePositions = card.GetMovablePositions(cardManager.BoardWidth, cardManager.BoardHeight, cardManager.GetAllCards());
            
            // 高亮可移动的格子
            foreach (Vector2Int pos in movablePositions)
            {
                CellView cellView = board.GetCellView(pos.x, pos.y);
                if (cellView != null)
                {
                    cellView.SetHighlight(CellView.HighlightType.Move);
                }
            }
        }
        
        // 高亮可攻击的卡牌
        public void HighlightAttackableCards(Vector2Int position)
        {
            Debug.Log($"高亮可攻击卡牌，选中位置: {position}");
            
            Card card = cardManager.GetCard(position);
            if (card == null || card.OwnerId != 0) return;
            
            // 如果不是玩家回合，不显示可攻击卡牌
            TurnManager turnManager = cardManager.GetTurnManager();
            if (turnManager != null && !turnManager.IsPlayerTurn())
                return;
                
            // 获取可攻击的位置
            List<Vector2Int> attackablePositions = card.GetAttackablePositions(cardManager.BoardWidth, cardManager.BoardHeight, cardManager.GetAllCards());
            
            // 高亮可攻击的格子
            foreach (Vector2Int pos in attackablePositions)
            {
                CellView cellView = board.GetCellView(pos.x, pos.y);
                if (cellView != null)
                {
                    cellView.SetHighlight(CellView.HighlightType.Attack);
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