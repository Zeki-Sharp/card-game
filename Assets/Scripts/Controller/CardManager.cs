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

        // 获取所有卡牌的字典
        public Dictionary<Vector2Int, Card> GetAllCards()
        {
            // 返回实际引用，而不是副本
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



        // 处理伤害和死亡
        private void ProcessDamageAndDeath(Card card, Vector2Int position, int hpBefore)
        {
            if (card.Data.Health <= 0)
            {
                // 卡牌死亡
                Debug.Log($"卡牌 {card.Data.Name} 被击败，移除卡牌");
                RemoveCard(position);
            }
            else if (hpBefore > card.Data.Health)
            {
                // 卡牌受伤但未死亡
                Debug.Log($"卡牌 {card.Data.Name} 受伤，当前血量: {card.Data.Health}");
                GameEventSystem.Instance.NotifyCardDamaged(position);
            }
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

        // 获取世界坐标
        private Vector3 GetWorldPosition(Vector2Int gridPosition)
        {
            // 计算棋盘中心偏移
            float offsetX = -((board.Width - 1) * 1f) / 2f;
            float offsetY = -((board.Height - 1) * 1f) / 2f;
            
            return new Vector3(
                gridPosition.x * 1f + offsetX,
                gridPosition.y * 1f + offsetY,
                -0.1f // 卡牌在单元格上方
            );
        }

        // 请求攻击
        public void RequestAttack()
        {
            Debug.Log("请求执行攻击");
            _stateMachine.ChangeState(CardState.Attacking);
        }

        // 请求移动 
        public void RequestMove()
        {
            Debug.Log("请求执行移动");
            _stateMachine.ChangeState(CardState.Moving);
        }

        // 对卡牌造成伤害
        public void DamageCard(Vector2Int position, int damage)
        {
            Card card = GetCard(position);
            if (card == null)
            {
                Debug.LogError($"伤害失败: 位置 {position} 没有卡牌");
                return;
            }
            
            // 记录伤害前的生命值
            int hpBefore = card.Data.Health;
            
            // 造成伤害
            card.Data.Health -= damage;
            
            Debug.Log($"卡牌 {card.Data.Name} 受到 {damage} 点伤害，当前血量: {card.Data.Health}");
            
            // 处理伤害和死亡
            ProcessDamageAndDeath(card, position, hpBefore);
        }

        private void OnDestroy()
        {
            // 释放状态机资源
            if (_stateMachine != null)
            {
                _stateMachine.Dispose();
            }
        }

        // 获取所有卡牌视图的字典
        public Dictionary<Vector2Int, CardView> GetAllCardViews()
        {
            // 返回实际引用，而不是副本
            return _cardViews;
        }

        // 更新卡牌位置
        public void UpdateCardPosition(Vector2Int fromPosition, Vector2Int toPosition)
        {
            if (_cards.ContainsKey(fromPosition))
            {
                Card card = _cards[fromPosition];
                _cards.Remove(fromPosition);
                card.Position = toPosition;
                _cards[toPosition] = card;
                Debug.Log($"CardManager: 更新卡牌数据位置 从 {fromPosition} 到 {toPosition}");
            }
            else
            {
                Debug.LogError($"CardManager: 找不到位置 {fromPosition} 的卡牌");
            }
        }

        // 更新卡牌视图位置
        public void UpdateCardViewPosition(Vector2Int fromPosition, Vector2Int toPosition)
        {
            if (_cardViews.ContainsKey(fromPosition))
            {
                CardView cardView = _cardViews[fromPosition];
                _cardViews.Remove(fromPosition);
                _cardViews[toPosition] = cardView;
                Debug.Log($"CardManager: 更新卡牌视图位置 从 {fromPosition} 到 {toPosition}");
            }
            else
            {
                Debug.LogError($"CardManager: 找不到位置 {fromPosition} 的卡牌视图");
            }
        }

        // 移动卡牌
        public bool MoveCard(Vector2Int fromPosition, Vector2Int toPosition)
        {
            MoveCardAction moveAction = new MoveCardAction(this, fromPosition, toPosition);
            return moveAction.Execute();
        }

        // 攻击卡牌
        public bool AttackCard(Vector2Int attackerPosition, Vector2Int targetPosition)
        {
            AttackCardAction attackAction = new AttackCardAction(this, attackerPosition, targetPosition);
            return attackAction.Execute();
        }

        // 翻转卡牌
        public bool FlipCard(Vector2Int position)
        {
            FlipCardAction flipAction = new FlipCardAction(this, position);
            return flipAction.Execute();
        }

        // 移除卡牌
        public bool RemoveCard(Vector2Int position)
        {
            Debug.Log($"CardManager.RemoveCard: 尝试移除位置 {position} 的卡牌");
            
            // 检查卡牌是否存在
            Card card = GetCard(position);
            if (card == null)
            {
                Debug.LogWarning($"CardManager.RemoveCard: 位置 {position} 没有卡牌");
                return false;
            }
            
            // 创建并执行移除行动
            RemoveCardAction removeAction = new RemoveCardAction(this, position);
            bool result = removeAction.Execute();
            
            if (result)
            {
                Debug.Log($"CardManager.RemoveCard: 成功移除位置 {position} 的卡牌 {card.Data.Name}");
            }
            else
            {
                Debug.LogError($"CardManager.RemoveCard: 移除位置 {position} 的卡牌失败");
            }
            
            return result;
        }

        // 添加卡牌
        public bool AddCard(Card card, Vector2Int position)
        {
            AddCardAction addAction = new AddCardAction(this, card, position);
            return addAction.Execute();
        }

    }


} 