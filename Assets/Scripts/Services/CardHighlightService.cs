using System.Collections.Generic;
using UnityEngine;
using ChessGame.Cards;

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
            // 订阅GameEventSystem的事件
            if (GameEventSystem.Instance != null)
            {
                // 订阅事件
                GameEventSystem.Instance.OnCardSelected += HighlightSelectedPosition;
                GameEventSystem.Instance.OnCardSelected += HighlightMovablePositions;
                GameEventSystem.Instance.OnCardDeselected += ClearAllHighlights;
                
                // 订阅自动触发能力事件
                GameEventSystem.Instance.OnAutomaticAbilityStart += (pos) => HighlightAutomaticAbilityTarget(pos, true);
                GameEventSystem.Instance.OnAutomaticAbilityEnd += (pos) => HighlightAutomaticAbilityTarget(pos, false);
            }
            else
            {
                Debug.LogError("找不到GameEventSystem实例");
            }
        }
        
        private void OnDestroy()
        {
            // 取消订阅GameEventSystem的事件
            if (GameEventSystem.Instance != null)
            {
                GameEventSystem.Instance.OnCardSelected -= HighlightSelectedPosition;
                GameEventSystem.Instance.OnCardSelected -= HighlightMovablePositions;
                GameEventSystem.Instance.OnCardDeselected -= ClearAllHighlights;
                
                // 取消订阅自动触发能力事件
                GameEventSystem.Instance.OnAutomaticAbilityStart -= (pos) => HighlightAutomaticAbilityTarget(pos, true);
                GameEventSystem.Instance.OnAutomaticAbilityEnd -= (pos) => HighlightAutomaticAbilityTarget(pos, false);
            }
        }
        
        // 高亮选中的卡牌位置
        public void HighlightSelectedPosition(Vector2Int position)
        {
            Debug.Log($"高亮选中位置: {position}");
            
            Card card = cardManager.GetCard(position);
            if (card == null) return;
            
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
            if (card == null || (card.OwnerId != 0 && !card.IsFaceDown)) return;
            
            // 如果不是玩家回合，不显示可移动位置
            TurnManager turnManager = cardManager.GetTurnManager();
            if (turnManager != null && !turnManager.IsPlayerTurn())
                return;
                
            // 使用新方法替代原有的高亮逻辑
            HighlightTargetablePositions(card);
            
            // 确保选中的卡牌仍然高亮
            HighlightSelectedPosition(position);
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
        
        /// <summary>
        /// 高亮显示可攻击位置
        /// </summary>
        public void HighlightAttackablePositions(Vector2Int selectedPosition)
        {
            Card selectedCard = cardManager.GetCard(selectedPosition);
            if (selectedCard == null) return;
            
            // 获取所有卡牌
            Dictionary<Vector2Int, Card> allCards = cardManager.GetAllCards();
            
            // 获取可攻击位置
            List<Vector2Int> attackablePositions = selectedCard.GetAttackablePositions(
                cardManager.BoardWidth, cardManager.BoardHeight, allCards);
            
            // 获取能力可作用位置
            List<Vector2Int> abilityPositions = selectedCard.GetAbilityTargetPositions(
                cardManager.BoardWidth, cardManager.BoardHeight, allCards);
            
            // 合并两个列表（去重）
            HashSet<Vector2Int> allTargetPositions = new HashSet<Vector2Int>(attackablePositions);
            foreach (var pos in abilityPositions)
            {
                allTargetPositions.Add(pos);
            }
            
            // 高亮显示
            foreach (var position in allTargetPositions)
            {
                CellView cellView = board.GetCellView(position.x, position.y);
                if (cellView != null)
                {
                    cellView.SetHighlight(CellView.HighlightType.Attack);
                }
            }
        }
        
        /// <summary>
        /// 高亮显示能力可作用的位置
        /// </summary>
        public void HighlightAbilityRange(Vector2Int selectedPosition)
        {
            Card selectedCard = cardManager.GetCard(selectedPosition);
            if (selectedCard == null) return;
            
            // 获取卡牌的所有能力
            List<AbilityConfiguration> abilities = selectedCard.GetAbilities();
            
            // 创建一个集合存储所有可作用位置
            HashSet<Vector2Int> allValidPositions = new HashSet<Vector2Int>();
            
            // 遍历所有能力
            foreach (var ability in abilities)
            {
                // 获取能力可作用的位置
                List<Vector2Int> abilityPositions = AbilityManager.Instance.GetAbilityRange(ability, selectedCard, cardManager);
                
                // 添加到集合中
                foreach (var pos in abilityPositions)
                {
                    allValidPositions.Add(pos);
                }
            }
            
            // 高亮显示所有可作用位置
            foreach (var position in allValidPositions)
            {
                CellView cellView = board.GetCellView(position.x, position.y);
                if (cellView != null)
                {
                    // 使用攻击高亮（红色）而不是移动高亮（绿色）
                    cellView.SetHighlight(CellView.HighlightType.Attack);
                }
            }
        }

        public void HighlightTargetablePositions(Card card)
        {
            if (card == null) return;
            
            // 保存选中卡牌的位置，以便稍后重新高亮
            Vector2Int selectedPosition = card.Position;
            
            // 清除之前的高亮
            ClearAllHighlights();
            
            // 重新高亮选中的卡牌
            CellView selectedCellView = board.GetCellView(selectedPosition.x, selectedPosition.y);
            if (selectedCellView != null)
            {
                selectedCellView.SetHighlight(CellView.HighlightType.Selected);
            }
            
            // 如果卡牌已经行动过，只显示选中高亮，不显示其他高亮
            if (card.HasActed) return;
            
            // 获取可移动位置
            List<Vector2Int> movePositions = card.GetMoveRange(board);
            
            // 获取可攻击位置
            List<Vector2Int> attackPositions = card.GetAttackRange(board);
            
            // 高亮显示可移动位置（绿色）
            foreach (var pos in movePositions)
            {
                CellView cellView = board.GetCellView(pos.x, pos.y);
                if (cellView != null)
                {
                    cellView.SetHighlight(CellView.HighlightType.Move);
                }
            }
            
            // 高亮显示可攻击位置（红色）
            foreach (var pos in attackPositions)
            {
                CellView cellView = board.GetCellView(pos.x, pos.y);
                if (cellView != null)
                {
                    cellView.SetHighlight(CellView.HighlightType.Attack);
                }
            }
            
            // 高亮显示能力可作用的位置，但只包括主动触发的能力
            foreach (var ability in card.GetAbilities())
            {
                // 跳过自动触发的能力
                if (ability.IsAutomatic())
                {
                    continue;
                }
                
                // 直接判断能力类型，决定使用哪种高亮
                CellView.HighlightType highlightType = DetermineAbilityHighlightType(ability);
                
                // 获取能力可作用的位置
                List<Vector2Int> abilityPositions = AbilityManager.Instance.GetAbilityRange(ability, card, cardManager);
                
                // 高亮显示
                foreach (var pos in abilityPositions)
                {
                    CellView cellView = board.GetCellView(pos.x, pos.y);
                    if (cellView != null)
                    {
                        cellView.SetHighlight(highlightType);
                    }
                }
            }
        }

        // 添加这个方法来判断能力应该使用哪种高亮类型
        private CellView.HighlightType DetermineAbilityHighlightType(AbilityConfiguration ability)
        {
            if (ability == null || ability.actionSequence == null || ability.actionSequence.Count == 0)
            {
                return CellView.HighlightType.Move;
            }
            
            // 检查能力的动作序列
            foreach (var action in ability.actionSequence)
            {
                // 如果包含攻击动作，使用攻击高亮
                if (action.actionType == AbilityActionConfig.ActionType.Attack)
                {
                    return CellView.HighlightType.Attack;
                }
            }
            
            // 默认使用移动高亮
            return CellView.HighlightType.Move;
        }

        /// <summary>
        /// 高亮显示自动触发能力的目标位置
        /// </summary>
        public void HighlightAutomaticAbilityTarget(Vector2Int position, bool highlight)
        {
            CellView cellView = board.GetCellView(position.x, position.y);
            if (cellView != null)
            {
                cellView.SetHighlight(highlight ? CellView.HighlightType.Ability : CellView.HighlightType.None);
            }
        }
    }
} 