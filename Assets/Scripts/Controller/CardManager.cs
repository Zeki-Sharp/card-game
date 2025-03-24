using System.Collections.Generic;
using UnityEngine;
using ChessGame.FSM;
using System;
using System.Collections;

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
        public event Action<Vector2Int> OnCardDamaged;
        
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
                
                // 触发卡牌翻面事件 - 确保参数正确
                OnCardFlipped?.Invoke(position, false); // false表示不再是背面
                
                Debug.Log($"CardManager: 卡牌翻面完成，位置: {position}, 从背面翻到正面");
            }
        }

        // 移除卡牌
        public void RemoveCard(Vector2Int position)
        {
            if (_cards.ContainsKey(position))
            {
                _cards.Remove(position);
                
                // 移除视图
                if (_cardViews.ContainsKey(position))
                {
                    CardView cardView = _cardViews[position];
                    if (cardView != null)
                    {
                        // 先触发销毁事件，让动画服务处理
                        OnCardRemoved?.Invoke(position);
                        
                        // 延迟销毁，给动画时间完成
                        StartCoroutine(DelayedDestroy(cardView.gameObject, 1.0f));
                    }
                    _cardViews.Remove(position);
                }
                else
                {
                    // 如果没有视图，直接触发事件
                    OnCardRemoved?.Invoke(position);
                }
            }
        }
        
        private IEnumerator DelayedDestroy(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null)
            {
                Destroy(obj);
            }
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
                Debug.Log($"CardManager: 清除选中位置 {_selectedPosition.Value}");
                _selectedPosition = null;
            }
            
            // 无论是否有选中位置，都触发OnCardDeselected事件
            OnCardDeselected?.Invoke();
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

        public Vector2Int GetTargetPosition()
        {
            return _targetPosition.Value;
        }

        public void NotifyCardDamaged(Vector2Int position)
        {
            OnCardDamaged?.Invoke(position);
        } 

        public void ForceAddCard(Card card, Vector2Int position)
        {
            _cards[position] = card;
            if (_cardViews.ContainsKey(card.Position))
            {
                CardView view = _cardViews[card.Position];
                _cardViews.Remove(card.Position);
                _cardViews[position] = view;
            }
        }

        public void NotifyCardMoved(Vector2Int fromPosition, Vector2Int toPosition)
        {
            OnCardMoved?.Invoke(fromPosition, toPosition);
        }

        public void NotifyCardAttacked(Vector2Int fromPosition, Vector2Int toPosition)
        {
            OnCardAttacked?.Invoke(fromPosition, toPosition);
        }

        public void NotifyCardRemoved(Vector2Int position)
        {
            OnCardRemoved?.Invoke(position);
        }

        // 请求攻击
        public void RequestAttack()
        {
            _stateMachine.ChangeState(CardState.Attacking);
        }

        // 请求移动 
        public void RequestMove()
        {
            _stateMachine.ChangeState(CardState.Moving);
        }

        // 移动卡牌（不销毁GameObject，只更新位置）
        public void MoveCard(Vector2Int fromPosition, Vector2Int toPosition)
        {
            if (!_cards.ContainsKey(fromPosition))
            {
                Debug.LogError($"移动卡牌失败：位置 {fromPosition} 没有卡牌");
                return;
            }
            
            if (_cards.ContainsKey(toPosition))
            {
                Debug.LogError($"移动卡牌失败：目标位置 {toPosition} 已被占用");
                return;
            }
            
            // 获取卡牌
            Card card = _cards[fromPosition];
            
            // 从旧位置移除
            _cards.Remove(fromPosition);
            
            // 添加到新位置
            _cards[toPosition] = card;
            card.Position = toPosition;
            
            // 移动视图
            if (_cardViews.ContainsKey(fromPosition))
            {
                CardView cardView = _cardViews[fromPosition];
                
                // 更新字典
                _cardViews.Remove(fromPosition);
                _cardViews[toPosition] = cardView;
                
                // 触发移动事件
                OnCardMoved?.Invoke(fromPosition, toPosition);
            }
        }

    }


} 