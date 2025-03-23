using System.Collections.Generic;
using UnityEngine;
using ChessGame.FSM;
using System;

namespace ChessGame
{
    public class CardManager : MonoBehaviour
    {
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private Board board;
        [SerializeField] private List<Sprite> cardImages = new List<Sprite>();
        [SerializeField] private CardDataProvider cardDataProvider;
        [SerializeField] private TurnManager turnManager;
        
        // 所有卡牌的字典，键为位置，值为卡牌
        private Dictionary<Vector2Int, Card> _cards = new Dictionary<Vector2Int, Card>();
        private Dictionary<Vector2Int, CardView> _cardViews = new Dictionary<Vector2Int, CardView>();
        
        // 当前选中的卡牌位置
        private Vector2Int? _selectedPosition;
        // 目标位置（移动或攻击）
        private Vector2Int? _targetPosition;
        
        // 状态机
        private CardStateMachine _stateMachine;
        
        // 公开属性
        public int BoardWidth => board != null ? board.Width : 0;
        public int BoardHeight => board != null ? board.Height : 0;
        public int MoveRange => 1; // 默认值，实际应该从选中的卡牌获取
        public int AttackRange => 1; // 默认值，实际应该从选中的卡牌获取

        public event Action<Vector2Int> OnCardRemoved;
        public event Action<Vector2Int, bool> OnCardFlipped;
        public event Action<Vector2Int, int, bool> OnCardAdded;
        public event Action<Vector2Int> OnCardSelected;
        public event Action OnCardDeselected;
        public event Action<Vector2Int, Vector2Int> OnCardMoved;
        public event Action<Vector2Int, Vector2Int> OnCardAttacked;
        public event Action<Card> OnCardDamaged;
        
        private void Awake()
        {
            Debug.Log("CardManager.Awake: 开始初始化状态机");
            
            try
            {
                // 确保在初始化状态机之前，所有必要的组件都已准备好
                if (board == null)
                {
                    board = FindObjectOfType<Board>();
                    Debug.Log($"找到Board: {(board != null ? "成功" : "失败")}");
                }
                
                // 初始化状态机
                _stateMachine = new CardStateMachine(this);
                Debug.Log("状态机初始化成功，当前状态：" + _stateMachine.GetCurrentStateType().ToString());

                if (turnManager == null)
                    turnManager = FindObjectOfType<TurnManager>();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"状态机初始化失败: {e.Message}\n{e.StackTrace}");
            }
        }
        
        private void Start()
        {
            
            // 加载卡牌图片资源
            if (cardImages.Count == 0)
            {
                LoadCardImages();
            }
        }
        
        private void Update()
        {
            if (_stateMachine != null)
            {
                _stateMachine.Update();
            }
        }
        
        // 加载卡牌图片资源
        private void LoadCardImages()
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>("Cards");
            if (sprites != null && sprites.Length > 0)
            {
                cardImages.AddRange(sprites);
                Debug.Log($"加载了 {sprites.Length} 张卡牌图片");
            }
            else
            {
                Debug.LogWarning("未找到卡牌图片资源，请确保Resources/Cards文件夹中有图片");
            }
        }
        
        // 生成卡牌
        public void SpawnRandomCards(int count)
        {
            Debug.Log($"尝试生成卡牌，最大数量: {count}");
            
            if (cardDataProvider == null)
            {
                cardDataProvider = FindObjectOfType<CardDataProvider>();
                if (cardDataProvider == null)
                {
                    Debug.LogError("找不到CardDataProvider组件");
                    return;
                }
            }
            
            // 获取所有空白格子
            List<Vector2Int> emptyPositions = GetEmptyPositions();
            
            if (emptyPositions.Count == 0)
            {
                Debug.LogWarning("没有空白格子可以生成卡牌");
                return;
            }
            
            // 获取所有卡牌数据
            List<CardData> allCardDatas = cardDataProvider.GetAllCardData();
            
            if (allCardDatas == null || allCardDatas.Count == 0)
            {
                Debug.LogError("没有可用的卡牌数据");
                return;
            }
            
            // 确定实际可以生成的卡牌数量
            int actualCount = Mathf.Min(count, emptyPositions.Count, allCardDatas.Count);
            
            Debug.Log($"卡牌集合中有 {allCardDatas.Count} 张卡牌，空白格子有 {emptyPositions.Count} 个，将生成 {actualCount} 张卡牌");
            
            // 随机打乱空白格子和卡牌数据
            ShuffleList(emptyPositions);
            ShuffleList(allCardDatas);
            
            // 分离玩家和敌方的卡牌数据
            List<CardData> playerCards = new List<CardData>();
            List<CardData> enemyCards = new List<CardData>();
            
            foreach (var cardData in allCardDatas)
            {
                if (cardData.Faction == 0)
                {
                    playerCards.Add(cardData);
                }
                else
                {
                    enemyCards.Add(cardData);
                }
            }
            
            // 确保每方至少有一张卡牌
            if (playerCards.Count == 0 || enemyCards.Count == 0)
            {
                Debug.LogWarning("无法为双方分配卡牌");
                return;
            }
            
            // 计算每方应该生成的卡牌数量
            int playerCardCount = Mathf.Min(playerCards.Count, actualCount / 2 + actualCount % 2);
            int enemyCardCount = Mathf.Min(enemyCards.Count, actualCount / 2);
            
            // 生成玩家卡牌
            for (int i = 0; i < playerCardCount && i < emptyPositions.Count; i++)
            {
                Vector2Int position = emptyPositions[i];
                CardData cardData = playerCards[i];
                
                // 第一张玩家卡牌是正面的，其余是背面的
                bool isFaceDown = i > 0;
                SpawnCard(cardData, position, isFaceDown);
            }
            
            // 生成敌方卡牌
            for (int i = 0; i < enemyCardCount && i + playerCardCount < emptyPositions.Count; i++)
            {
                Vector2Int position = emptyPositions[i + playerCardCount];
                CardData cardData = enemyCards[i];
                
                // 第一张敌方卡牌是正面的，其余是背面的
                bool isFaceDown = i > 0;
                SpawnCard(cardData, position, isFaceDown);
            }
            
            Debug.Log($"成功生成了 {playerCardCount + enemyCardCount} 张卡牌");
        }
        
        // 获取所有空白格子
        private List<Vector2Int> GetEmptyPositions()
        {
            List<Vector2Int> emptyPositions = new List<Vector2Int>();
            
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    Vector2Int position = new Vector2Int(x, y);
                    if (!_cards.ContainsKey(position))
                    {
                        emptyPositions.Add(position);
                    }
                }
            }
            
            return emptyPositions;
        }
        
        // 在指定位置生成卡牌
        public void SpawnCard(CardData cardData, Vector2Int position, bool isFaceDown = true)
        {
            // 检查位置是否已有卡牌
            if (_cards.ContainsKey(position))
            {
                Debug.LogWarning($"位置 {position} 已有卡牌，无法生成新卡牌");
                return;
            }
            
            // 创建卡牌模型，使用CardData中的阵营属性
            Card card = new Card(cardData, position, cardData.Faction, isFaceDown);
            _cards[position] = card;
            
            // 创建卡牌视图
            CellView cellView = board.GetCellView(position.x, position.y);
            if (cellView != null)
            {
                // 使用单元格的实际位置，并稍微调整Z坐标使卡牌显示在单元格上方
                Vector3 cardPosition = cellView.transform.position;
                cardPosition.z = -0f; // 调整Z坐标，使卡牌显示在单元格上方
                Quaternion rotation = Quaternion.Euler(Camera.main.transform.rotation.eulerAngles.x, Camera.main.transform.rotation.eulerAngles.y, 0);

                GameObject cardObject = Instantiate(cardPrefab, cardPosition, rotation);
                cardObject.name = $"Card_{cardData.Name}_{position.x}_{position.y}";
                
                CardView cardView = cardObject.GetComponent<CardView>();
                if (cardView != null)
                {
                    cardView.Initialize(card);
                    _cardViews[position] = cardView;
                }
            }
            
            // 触发卡牌添加事件
            OnCardAdded?.Invoke(position, card.OwnerId, card.IsFaceDown);
            
            Debug.Log($"生成卡牌: {cardData.Name}, 位置: {position}, 阵营: {card.OwnerId}, 背面: {isFaceDown}");
        }
        
        // 选中卡牌
        public void SelectCard(Vector2Int position)
        {
            Debug.Log($"CardManager.SelectCard: 位置 {position}");
            
            if (!_cards.ContainsKey(position))
            {
                Debug.LogWarning($"位置 {position} 没有卡牌，无法选择");
                return;
            }
            
            _selectedPosition = position;
            
            // 获取卡牌视图并高亮
            if (_cardViews.ContainsKey(position))
            {
                CardView cardView = _cardViews[position];
                Debug.Log($"找到卡牌视图: {cardView.name}");
            }
            else
            {
                Debug.LogWarning($"位置 {position} 没有对应的卡牌视图");
            }
            
            // 触发选中事件
            Debug.Log($"触发OnCardSelected事件，位置: {position}");
            OnCardSelected?.Invoke(position);
        }
        
        // 获取选中的卡牌
        public Card GetSelectedCard()
        {
            if (_selectedPosition.HasValue)
                return GetCard(_selectedPosition.Value);
            return null;
        }
        
        // 获取指定位置的单元格视图
        public CellView GetCellView(int x, int y)
        {
            return board.GetCellView(x, y);
        }
        
        // 清除所有高亮
        public void ClearAllHighlights()
        {
            // 清除卡牌选中状态
            foreach (var cardView in _cardViews.Values)
            {
                cardView.SetSelected(false);
                cardView.SetAttackable(false);
            }
            
            // 清除格子高亮
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    CellView cellView = board.GetCellView(x, y);
                    if (cellView != null)
                    {
                        cellView.SetHighlight(CellView.HighlightType.None);
                    }
                }
            }
        }
        
        // 高亮选中的棋子位置
        public void HighlightSelectedPosition(Card card)
        {
            Vector2Int position = card.Position;
            CellView cellView = GetCellView(position.x, position.y);
            if (cellView != null)
            {
                cellView.SetHighlight(CellView.HighlightType.Selected);
            }
        }
        
        // 高亮可移动的格子
        public void HighlightMovablePositions(Card card)
        {
            if (card == null)
                return;
                
            // 获取可移动的位置
            List<Vector2Int> movablePositions = card.GetMovablePositions(BoardWidth, BoardHeight, _cards);
            
            // 高亮可移动的格子
            foreach (Vector2Int pos in movablePositions)
            {
                CellView cellView = GetCellView(pos.x, pos.y);
                if (cellView != null)
                {
                    cellView.SetHighlight(CellView.HighlightType.Move);
                }
            }
        }
        
        // 高亮可攻击的卡牌
        public void HighlightAttackableCards(Card card)
        {
            if (card == null)
                return;
                
            // 获取可攻击的位置
            List<Vector2Int> attackablePositions = card.GetAttackablePositions(BoardWidth, BoardHeight, _cards);
            
            // 高亮可攻击的格子
            foreach (Vector2Int pos in attackablePositions)
            {
                CellView cellView = GetCellView(pos.x, pos.y);
                if (cellView != null)
                {
                    cellView.SetHighlight(CellView.HighlightType.Attack);
                }
            }
        }
        
        // 检查状态机
        public void CheckStateMachine()
        {
            if (_stateMachine == null)
            {
                Debug.LogWarning("状态机未初始化，尝试重新初始化");
                try
                {
                    _stateMachine = new CardStateMachine(this);
                    Debug.Log("状态机重新初始化成功，当前状态：" + _stateMachine.GetCurrentStateType().ToString());
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"状态机重新初始化失败: {e.Message}\n{e.StackTrace}");
                }
            }
            else
            {
                Debug.Log("状态机已初始化，当前状态：" + _stateMachine.GetCurrentStateType().ToString());
            }
        }
        
        // 辅助方法：随机打乱列表
        private void ShuffleList<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = UnityEngine.Random.Range(0, n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
        
        // 执行移动
        public bool ExecuteMove()
        {
            Debug.Log("CardManager.ExecuteMove: 开始执行移动");
            
            if (!_selectedPosition.HasValue || !_targetPosition.HasValue)
            {
                Debug.LogWarning("无法执行移动：未选中卡牌或未设置目标位置");
                return false;
            }
            
            Vector2Int fromPos = _selectedPosition.Value;
            Vector2Int toPos = _targetPosition.Value;
            
            Debug.Log($"移动卡牌: 从 {fromPos} 到 {toPos}");
            
            // 获取移动的卡牌
            Card card = GetCard(fromPos);
            if (card == null)
            {
                Debug.LogWarning("无法执行移动：起始位置没有卡牌");
                return false;
            }
            
            bool success = MoveCard(fromPos, toPos);
            
            // 清除选择和目标
            _selectedPosition = null;
            _targetPosition = null;
            
            Debug.Log($"移动执行完成: {(success ? "成功" : "失败")}");
            
            // 如果是玩家回合且移动成功，结束玩家回合
            if (success && turnManager != null && turnManager.IsPlayerTurn())
            {
                if (card.OwnerId == 0) // 确保是玩家的卡牌
                {
                    Debug.Log("玩家移动完成，结束玩家回合");
                    turnManager.EndPlayerTurn();
                }
            }
            
            return success;
        }
        
        // 执行攻击
        public bool ExecuteAttack()
        {
            Debug.Log("CardManager.ExecuteAttack: 开始执行攻击");
            
            if (!_selectedPosition.HasValue || !_targetPosition.HasValue)
            {
                Debug.LogWarning("无法执行攻击：未选中卡牌或未设置目标位置");
                return false;
            }
            
            Vector2Int attackerPos = _selectedPosition.Value;
            Vector2Int targetPos = _targetPosition.Value;
            
            Debug.Log($"攻击: 从 {attackerPos} 到 {targetPos}");
            
            // 获取攻击的卡牌
            Card attackerCard = GetCard(attackerPos);
            if (attackerCard == null)
            {
                Debug.LogWarning("无法执行攻击：攻击者位置没有卡牌");
                return false;
            }
            
            bool success = CardAttack(attackerPos, targetPos);
            
            // 清除选择和目标
            _selectedPosition = null;
            _targetPosition = null;
            
            Debug.Log($"攻击执行完成: {(success ? "成功" : "失败")}");
            
            // 如果是玩家回合且攻击成功，结束玩家回合
            if (success && turnManager != null && turnManager.IsPlayerTurn())
            {
                if (attackerCard.OwnerId == 0) // 确保是玩家的卡牌
                {
                    Debug.Log("玩家攻击完成，结束玩家回合");
                    turnManager.EndPlayerTurn();
                }
            }
            
            return success;
        }
        
        // 移动卡牌
        private bool MoveCard(Vector2Int fromPosition, Vector2Int toPosition)
        {
            if (!_cards.ContainsKey(fromPosition) || _cards.ContainsKey(toPosition))
            {
                Debug.LogWarning("无法移动卡牌：起始位置没有卡牌或目标位置已有卡牌");
                return false;
            }
            
            // 获取目标单元格的位置
            CellView targetCellView = board.GetCellView(toPosition.x, toPosition.y);
            if (targetCellView == null)
            {
                Debug.LogWarning("无法移动卡牌：目标位置无效");
                return false;
            }
            
            // 更新卡牌模型
            Card card = _cards[fromPosition];
            card.Position = toPosition;
            _cards.Remove(fromPosition);
            _cards[toPosition] = card;
            
            // 更新卡牌视图
            CardView cardView = _cardViews[fromPosition];
            _cardViews.Remove(fromPosition);
            _cardViews[toPosition] = cardView;
            
            // 移动卡牌视图到新位置
            Vector3 newPosition = targetCellView.transform.position;
            newPosition.z = -0.1f; // 保持Z坐标不变
            cardView.transform.position = newPosition;
            
            // 标记卡牌已行动
            card.HasActed = true;
            cardView.UpdateVisuals();
            
            return true;
        }
        
        // 执行卡牌攻击
        private bool CardAttack(Vector2Int attackerPosition, Vector2Int targetPosition)
        {
            if (!_cards.ContainsKey(attackerPosition) || !_cards.ContainsKey(targetPosition))
            {
                Debug.LogWarning("无法执行攻击：攻击者或目标位置没有卡牌");
                return false;
            }
            
            Card attacker = _cards[attackerPosition];
            Card target = _cards[targetPosition];
            
            // 如果攻击者是背面状态，不能攻击
            if (attacker.IsFaceDown)
            {
                Debug.LogWarning("背面状态的卡牌不能攻击");
                return false;
            }
            
            // 检查目标是否是背面状态
            bool targetWasFaceDown = target.IsFaceDown;
            bool targetWillDie = false;
            bool attackerWillDie = false;
            
            // 记录攻击前的血量
            int targetHealthBefore = target.Data.Health;
            int attackerHealthBefore = attacker.Data.Health;
            
            // 执行攻击
            if (attacker.Attack(target))
            {
                // 检查目标是否会死亡（只有正面卡牌才会死亡）
                targetWillDie = !targetWasFaceDown && target.Data.Health <= 0;
                
                // 检查攻击者是否会死亡（只有在攻击正面卡牌时才可能死亡）
                attackerWillDie = !targetWasFaceDown && attacker.Data.Health <= 0;
                
                // 播放攻击动画
                CardView attackerView = _cardViews[attackerPosition];
                CardView targetView = _cardViews[targetPosition];
                
                if (attackerView != null && targetView != null)
                {
                    attackerView.PlayAttackAnimation(targetView.transform.position);
                    
                    // 如果目标从背面翻转为正面，播放翻转动画
                    if (targetWasFaceDown)
                    {
                        OnCardFlipped?.Invoke(targetPosition, false);  
                        FlipCard(targetPosition);
                        
                        // 如果背面卡牌受到致命伤害，显示血量为1
                        if (targetHealthBefore - attacker.Data.Attack <= 0)
                        {
                            Debug.Log("背面卡牌受到致命伤害，血量设为1");
                        }
                    }
                    else
                    {
                        targetView.PlayDamageAnimation();
                        
                        // 如果攻击者也受伤，播放受伤动画
                        if (attackerHealthBefore > attacker.Data.Health)
                        {
                            attackerView.PlayDamageAnimation();
                        }
                    }
                    
                    // 更新目标卡牌视图
                    targetView.UpdateVisuals();
                    
                    // 更新攻击者卡牌视图
                    attackerView.UpdateVisuals();
                    
                    // 检查目标是否死亡
                    if (targetWillDie)
                    {
                        RemoveCard(targetPosition);
                    }
                    
                    // 检查攻击者是否死亡
                    if (attackerWillDie)
                    {
                        RemoveCard(attackerPosition);
                    }
                }
                
                return true;
            }
            
            return false;
        }
        
        // 移除卡牌
        public void RemoveCard(Vector2Int position)
        {
            if (!_cards.ContainsKey(position))
                return;
            
            Debug.Log($"移除卡牌，位置: {position}");
            
            // 移除卡牌模型
            _cards.Remove(position);
            
            // 播放死亡动画并移除卡牌视图
            if (_cardViews.ContainsKey(position))
            {
                CardView cardView = _cardViews[position];
                if (cardView != null)
                {
                    cardView.PlayDeathAnimation();
                }
                _cardViews.Remove(position);
            }
            
            // 触发卡牌移除事件
            OnCardRemoved?.Invoke(position);
        }
        
        // 重置所有卡牌的行动状态
        public void ResetAllCardActions()
        {
            foreach (var card in _cards.Values)
            {
                card.ResetAction();
            }
            
            foreach (var cardView in _cardViews.Values)
            {
                cardView.UpdateVisuals();
            }
        }
        
        // 处理单元格点击
        public void HandleCellClick(Vector2Int position)
        {
            Debug.Log($"CardManager.HandleCellClick: 位置 {position}, 当前状态: {_stateMachine.GetCurrentStateType().ToString()}");
            
            if (_stateMachine == null)
            {
                Debug.LogWarning("状态机未初始化，尝试重新初始化");
                try
                {
                    _stateMachine = new CardStateMachine(this);
                    Debug.Log("状态机重新初始化成功");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"状态机重新初始化失败: {e.Message}\n{e.StackTrace}");
                    return;
                }
            }
            
            // 检查是否有卡牌在该位置
            if (_cards.ContainsKey(position))
            {
                Debug.Log($"位置 {position} 有卡牌，调用HandleCardClick");
                _stateMachine.HandleCardClick(position);
            }
            else
            {
                Debug.Log($"位置 {position} 没有卡牌，调用HandleCellClick");
                _stateMachine.HandleCellClick(position);
            }
        }
        
        // 获取TurnManager
        public TurnManager GetTurnManager()
        {
            return turnManager;
        }
        
        // 检查是否有玩家卡牌
        public bool HasPlayerCards()
        {
            foreach (var card in _cards.Values)
            {
                if (card.OwnerId == 0)
                    return true;
            }
            return false;
        }
        
        // 检查是否有敌方卡牌
        public bool HasEnemyCards()
        {
            foreach (var card in _cards.Values)
            {
                if (card.OwnerId == 1)
                    return true;
            }
            return false;
        }

        // 翻转卡牌
        public void FlipCard(Vector2Int position)
        {
            if (!_cards.ContainsKey(position))
            {
                Debug.LogWarning($"CardManager: 位置 {position} 没有卡牌，无法翻转");
                return;
            }
                
            Card card = _cards[position];
            bool wasFaceDown = card.IsFaceDown;
            
            if (wasFaceDown)
            {
                Debug.Log($"CardManager: 翻转卡牌，位置: {position}, 卡牌: {card.Data.Name}");
                
                // 翻转卡牌
                card.FlipToFaceUp();
                
                // 更新视图
                if (_cardViews.ContainsKey(position))
                {
                    CardView cardView = _cardViews[position];
                    cardView.PlayFlipAnimation();
                    cardView.UpdateVisuals(); // 确保视图更新
                }
                
                // 触发卡牌翻面事件 - 确保参数正确
                OnCardFlipped?.Invoke(position, false); // false表示不再是背面
                
                Debug.Log($"CardManager: 卡牌翻面完成，位置: {position}, 从背面翻到正面");
            }
            else
            {
                Debug.LogWarning($"CardManager: 位置 {position} 的卡牌已经是正面，无需翻转");
            }
        }

        // 标记所有玩家卡牌为已行动
        public void MarkAllPlayerCardsAsActed()
        {
            foreach (var card in _cards.Values)
            {
                if (card.OwnerId == 0) // 玩家卡牌
                {
                    card.HasActed = true;
                }
            }
            
            // 更新所有卡牌视图
            foreach (var cardView in _cardViews.Values)
            {
                cardView.UpdateVisuals();
            }
        }

        // 获取所有卡牌
        public Dictionary<Vector2Int, Card> GetAllCards()
        {
            return new Dictionary<Vector2Int, Card>(_cards);
        }

        public void ClearSelectedPosition()
        {
            if (_selectedPosition.HasValue)
            {
                _selectedPosition = null;
                OnCardDeselected?.Invoke();
            }
        }

        public CardView GetCardView(Vector2Int position)
        {
            if (_cardViews.ContainsKey(position))
                return _cardViews[position];
            return null;
        }

        public bool HasCard(Vector2Int position)
        {
            return _cards.ContainsKey(position);
        }

        public Card GetCard(Vector2Int position)
        {
            if (_cards.ContainsKey(position))
                return _cards[position];
            return null;
        }

        public bool CanMoveTo(Vector2Int position)
        {
            if (!_selectedPosition.HasValue)
                return false;
                
            Card selectedCard = GetCard(_selectedPosition.Value);
            if (selectedCard == null)
                return false;
                
            return selectedCard.CanMoveTo(position, _cards);
        }

        public bool CanAttack(Vector2Int position)
        {
            if (!_selectedPosition.HasValue)
                return false;
                
            Card selectedCard = GetCard(_selectedPosition.Value);
            if (selectedCard == null)
                return false;
                
            return selectedCard.CanAttack(position, _cards);
        }

        // 添加SetTargetPosition方法
        public void SetTargetPosition(Vector2Int position)
        {
            Debug.Log($"CardManager.SetTargetPosition: 位置 {position}");
            _targetPosition = position;
        }
    }
} 