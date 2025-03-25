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

        // 移动卡牌
        public void MoveCard(Vector2Int fromPosition, Vector2Int toPosition)
        {
            // 检查源位置是否有卡牌
            if (!_cards.ContainsKey(fromPosition))
            {
                Debug.LogError($"位置 {fromPosition} 没有卡牌，无法移动");
                return;
            }
            
            // 检查目标位置是否已有卡牌
            if (_cards.ContainsKey(toPosition))
            {
                Debug.LogError($"位置 {toPosition} 已有卡牌，无法移动到此位置");
                return;
            }
            
            // 获取卡牌
            Card card = _cards[fromPosition];
            
            // 更新卡牌位置
            card.Position = toPosition;
            
            // 更新数据结构
            _cards.Remove(fromPosition);
            _cards[toPosition] = card;
            
            // 更新视图
            if (_cardViews.ContainsKey(fromPosition))
            {
                CardView cardView = _cardViews[fromPosition];
                _cardViews.Remove(fromPosition);
                _cardViews[toPosition] = cardView;
                
                // 更新卡牌视图位置（实际移动由CardAnimationService处理）
                CellView cellView = board.GetCellView(toPosition.x, toPosition.y);
                if (cellView != null)
                {
                    // 这里只设置目标位置，实际的移动动画由CardAnimationService处理
                    Vector3 targetPosition = cellView.transform.position;
                    targetPosition.z = -0.1f; // 保持卡牌在单元格上方
                    
                    // 这里不直接设置位置，而是让动画服务处理
                    // cardView.transform.position = targetPosition;
                }
            }
            
            Debug.Log($"移动卡牌: 从 {fromPosition} 到 {toPosition}");
        }

        // 添加卡牌到管理器
        public void AddCard(Card card, Vector2Int position)
        {
            // 检查位置是否已有卡牌
            if (_cards.ContainsKey(position))
            {
                Debug.LogWarning($"位置 {position} 已有卡牌，无法添加新卡牌");
                return;
            }
            
            // 添加卡牌数据
            _cards[position] = card;
            
            // 创建卡牌视图
            CreateCardView(card, position);
            
            // 触发卡牌添加事件
            OnCardAdded?.Invoke(position, card.OwnerId, card.IsFaceDown);
            
            Debug.Log($"添加卡牌: {card.Data.Name}, 位置: {position}, 所有者: {card.OwnerId}, 背面: {card.IsFaceDown}");
        }

        // 创建卡牌视图
        private void CreateCardView(Card card, Vector2Int position)
        {
            // 获取单元格视图
            CellView cellView = board.GetCellView(position.x, position.y);
            if (cellView == null)
            {
                Debug.LogError($"找不到位置 {position} 的单元格视图");
                return;
            }
            
            // 使用单元格的实际位置，并稍微调整Z坐标使卡牌显示在单元格上方
            Vector3 cardPosition = cellView.transform.position;
            cardPosition.z = -0.1f; // 调整Z坐标，使卡牌显示在单元格上方
            
            // 根据主相机的旋转设置卡牌的旋转
            Quaternion rotation = Quaternion.Euler(90, 0, 0);
            if (Camera.main != null)
            {
                rotation = Quaternion.Euler(Camera.main.transform.rotation.eulerAngles.x, 0, 0);
            }
            
            // 实例化卡牌预制体
            GameObject cardObject = Instantiate(cardPrefab, cardPosition, rotation);
            cardObject.name = $"Card_{card.Data.Name}_{position.x}_{position.y}";
            
            // 获取并初始化卡牌视图组件
            CardView cardView = cardObject.GetComponent<CardView>();
            if (cardView != null)
            {
                cardView.Initialize(card);
                _cardViews[position] = cardView;
            }
            else
            {
                Debug.LogError($"卡牌预制体没有CardView组件");
            }
        }

    }


} 