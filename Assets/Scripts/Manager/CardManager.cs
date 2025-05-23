using System.Collections.Generic;
using UnityEngine;
using ChessGame.FSM;
using System;
using System.Collections;
using ChessGame.Cards;

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
        
        
        // 选中卡牌
        public void SelectCard(Vector2Int position)
        {
            Debug.Log($"CardManager.SelectCard: 位置 {position}");
            
            // 检查是否有卡牌
            if (!_cards.ContainsKey(position))
            {
                Debug.LogWarning($"选择卡牌失败：位置 {position} 没有卡牌");
                return;
            }
            
            Card card = _cards[position];
            Debug.Log($"选中卡牌: {card.Data.Name}, 所有者: {card.OwnerId}, 是否背面: {card.IsFaceDown}");
            
            // 设置选中位置
            _selectedPosition = position;
            
            // 触发选中事件
            GameEventSystem.Instance.NotifyCardSelected(position);
        }
        
        // 获取选中的卡牌
        public Card GetSelectedCard()
        {
            if (_selectedPosition.HasValue)
                return GetCard(_selectedPosition.Value);
            return null;
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

        //获取状态机
        public CardStateMachine GetCardStateMachine()
        {
            return _stateMachine;
        }

        
        // 重置所有卡牌的行动状态
        public void ResetAllCardActions()
        {
            foreach (var card in _cards.Values)
            {
                card.ResetAction();
            }

            // 触发所有卡牌行动事件
            foreach (var card in _cards.Values)
            {
                GameEventSystem.Instance.NotifyCardActed(card.Position);
            }
        }

        /// <summary>
        /// 重置所有玩家卡牌的行动状态
        /// </summary>
        public void ResetPlayerCardActions()
        {
            Debug.Log("重置所有玩家卡牌的行动状态");
            
            foreach (var kvp in _cards)
            {
                Card card = kvp.Value;
                if (card.OwnerId == 0) // 玩家卡牌
                {
                    card.HasActed = false;

                    // 触发所有卡牌行动事件
                    GameEventSystem.Instance.NotifyCardActed(card.Position);
                }
            }
        }

        /// <summary>
        /// 重置所有敌方卡牌的行动状态
        /// </summary>
        public void ResetEnemyCardActions()
        {
            Debug.Log("重置所有敌方卡牌的行动状态");
            
            foreach (var kvp in _cards)
            {
                Card card = kvp.Value;
                if (card.OwnerId == 1) // 敌方卡牌
                {
                    card.HasActed = false;

                    // 触发所有卡牌行动事件
                    GameEventSystem.Instance.NotifyCardActed(card.Position);
                }
            }
        }
        
        // 处理单元格点击
        public void HandleCellClick(Vector2Int position)
        {
            // 检查是否正在执行能力
            if (AbilityManager.IsExecutingAbility)
            {
                Debug.Log("正在执行能力，忽略玩家输入");
                return;
            }
            
            // 检查当前回合状态是否允许玩家输入
            if (turnManager != null && turnManager.GetTurnStateMachine() != null && 
                !turnManager.GetTurnStateMachine().AllowPlayerInput)
            {
                Debug.Log("当前回合阶段不允许玩家输入");
                return;
            }
            
            Debug.Log($"CardManager.HandleCellClick: 位置 {position}, 当前状态: {_stateMachine.GetCurrentStateType().ToString()}");
            
            if (_stateMachine == null)
            {
                Debug.LogError("CardManager.HandleCellClick: 状态机为空");
                return;
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

            // 触发所有卡牌行动事件
            foreach (var card in _cards.Values)
            {
                GameEventSystem.Instance.NotifyCardActed(card.Position);
            }   
        }

        // 获取所有卡牌的字典
        public Dictionary<Vector2Int, Card> GetAllCards()
        {
            Debug.Log($"刺客攻击检查cardmanager：获取所有卡牌，数量：{_cards.Count}");
            foreach (var pair in _cards)
            {
                Debug.Log($"刺客攻击检查cardmanager：位置 {pair.Key}：卡牌 {pair.Value.Data.Name}，背面状态：{pair.Value.IsFaceDown}");
            }
            return _cards;
        }

        public Vector2Int GetSelectedPosition()
        {
            return _selectedPosition.Value;
        }

        public void ClearSelectedPosition()
        {
            if (_selectedPosition.HasValue)
            {
                Debug.Log($"CardManager: 清除选中位置 {_selectedPosition.Value}");
                _selectedPosition = null;
            }
            
            // 直接通知GameEventSystem
            GameEventSystem.Instance.NotifyCardDeselected();
        }

        // 获取指定位置的卡牌视图
        public CardView GetCardView(Vector2Int position)
        {
            if (_cardViews.ContainsKey(position))
            {
                return _cardViews[position];
            }
            return null;
        }

        public bool HasCard(Vector2Int position)
        {
            return _cards.ContainsKey(position);
        }

        public Card GetCard(Vector2Int position)
        {
            if (_cards.ContainsKey(position))
            {
                return _cards[position];
            }
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

        public void ClearTargetPosition()
        {
            _targetPosition = null;
        }

        // 修改DeselectCard方法
        public void DeselectCard()
        {
            // 检查是否有选中的卡牌
            if (!_selectedPosition.HasValue)
            {
                Debug.Log("没有选中的卡牌，无需取消选中");
                return;
            }
            
            // 清除选中位置
            _selectedPosition = null;
            
            // 清除目标位置
            _targetPosition = null;
            
            // 无论是否有选中位置，都触发OnCardDeselected事件
            GameEventSystem.Instance.NotifyCardDeselected();
        }


        // 创建卡牌视图
        public CardView CreateCardView(Card card, Vector2Int position)
        {
            if (card == null) return null;
            
            // 获取单元格视图
            CellView cellView = board.GetCellView(position.x, position.y);
            if (cellView == null)
            {
                Debug.LogError($"找不到位置 {position} 的单元格视图");
                return null;
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
            }
            
            return cardView;
        }


        // 移动卡牌
        public bool MoveCard(Vector2Int fromPosition, Vector2Int toPosition)
        {
            Debug.Log($"CardManager.MoveCard: 从 {fromPosition} 到 {toPosition}");
            
            // 确保自身引用正确传递
            MoveCardAction moveAction = new MoveCardAction(this, fromPosition, toPosition);
            return moveAction.Execute();
        }

        // 攻击卡牌
        public bool AttackCard(Vector2Int attackerPosition, Vector2Int targetPosition)
        {
            Debug.Log($"CardManager.AttackCard: 从 {attackerPosition} 攻击 {targetPosition}");
            
            // 确保自身引用正确传递
            AttackCardAction attackAction = new AttackCardAction(this, attackerPosition, targetPosition);
            return attackAction.Execute();
        }


        // 移除卡牌
        public bool RemoveCard(Vector2Int position)
        {
            RemoveCardAction removeAction = new RemoveCardAction(this, position);
            return removeAction.Execute();
        }

        // 添加卡牌
        public bool AddCard(Card card, Vector2Int position)
        {
            AddCardAction addAction = new AddCardAction(this, card, position);
            return addAction.Execute();
        }

        // 获取所有卡牌视图的字典
        public Dictionary<Vector2Int, CardView> GetAllCardViews()
        {
            // 返回实际引用，而不是副本
            return _cardViews;
        }

        // 在指定位置生成卡牌
        public void SpawnCardAt(Vector2Int position, int cardId, int ownerId = 0, bool isFaceDown = true)
        {
            if (this == null || cardDataProvider == null)
            {
                Debug.LogError("CardManager或CardDataProvider未设置");
                return;
            }
            
            // 检查位置是否已被占用
            if (GetCard(position) != null)
            {
                Debug.LogWarning($"位置 {position} 已被占用，无法生成卡牌");
                return;
            }
            
            // 获取卡牌数据
            CardData cardData = cardDataProvider.GetCardDataById(cardId);
            if (cardData == null)
            {
                Debug.LogError($"找不到ID为 {cardId} 的卡牌数据");
                return;
            }
            
            // 创建卡牌
            Card card = new Card(cardData, position, ownerId, isFaceDown);
            
            // 设置卡牌行为
            // SetCardBehavior(card);
            
            // 添加到卡牌管理器
            AddCard(card, position);
            
            Debug.Log($"在位置 {position} 生成卡牌: {cardData.Name}, 所有者: {ownerId}, 背面: {isFaceDown}");
        }
        

        // 翻开指定位置的卡牌
        public void FlipCard(Vector2Int position)
        {
            Card card = GetCard(position);
            if (card != null && card.IsFaceDown)
            {
                // 创建并执行翻转卡牌行动
                FlipCardAction action = new FlipCardAction(this, position);
                if (action.CanExecute())
                {
                    action.Execute();
                    Debug.Log($"翻开位置 {position} 的卡牌: {card.Data.Name}");
                }
                else
                {
                    Debug.LogWarning($"无法翻开位置 {position} 的卡牌");
                }
            }
        }

        // 清空所有卡牌
        public void ClearAllCards()
        {
            Debug.Log("CardManager.ClearAllCards: 开始清空所有卡牌");
            
            // 保存所有卡牌位置的副本
            List<Vector2Int> positions = new List<Vector2Int>(_cards.Keys);
            Debug.Log($"当前卡牌数量: {positions.Count}");
            
            // 移除所有卡牌
            foreach (Vector2Int position in positions)
            {
                Debug.Log($"移除位置 {position} 的卡牌");
                RemoveCard(position);
            }
            
            // 确保字典已清空
            _cards.Clear();
            _cardViews.Clear();
            
            // 清除选中状态
            _selectedPosition = null;
            _targetPosition = null;
            
            Debug.Log("已清空所有卡牌");
        }

    
    }
} 