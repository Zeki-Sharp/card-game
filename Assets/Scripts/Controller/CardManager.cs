using System.Collections.Generic;
using UnityEngine;
using ChessGame.FSM;
using Unity.Mathematics;  // 使用FSM命名空间

namespace ChessGame
{
    public class CardManager : MonoBehaviour
    {
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private Board board;
        [SerializeField] private List<Sprite> cardImages = new List<Sprite>();
        [SerializeField] private CardDataProvider cardDataProvider;
        [SerializeField] private int moveRange = 1; // 移动范围
        [SerializeField] private int attackRange = 1; // 攻击范围
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
        public int MoveRange => moveRange;
        public int AttackRange => attackRange;
        
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
            if (board == null)
                board = FindObjectOfType<Board>();
            
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
            
            // 随机打乱空白格子
            ShuffleList(emptyPositions);
            
            // 生成卡牌，使用集合中的原始顺序
            for (int i = 0; i < actualCount; i++)
            {
                Vector2Int position = emptyPositions[i];
                SpawnCard(allCardDatas[i], position);
            }
            
            Debug.Log($"成功生成了 {actualCount} 张卡牌");
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
        
        // 创建随机卡牌数据
        private CardData CreateRandomCardData()
        {
            int id = UnityEngine.Random.Range(1000, 9999);
            string name = "Card_" + id;
            int attack = UnityEngine.Random.Range(1, 6);
            int health = UnityEngine.Random.Range(1, 10);
            
            // 随机选择一张图片
            Sprite image = null;
            if (cardImages.Count > 0)
            {
                image = cardImages[UnityEngine.Random.Range(0, cardImages.Count)];
            }
            
            return new CardData(id, name, attack, health, image);
        }
        
        // 在指定位置生成卡牌
        public void SpawnCard(CardData cardData, Vector2Int position)
        {
            // 检查位置是否已有卡牌
            if (_cards.ContainsKey(position))
            {
                Debug.LogWarning($"位置 {position} 已有卡牌，无法生成新卡牌");
                return;
            }
            
            // 创建卡牌模型，使用CardData中的阵营属性
            Card card = new Card(cardData, position, cardData.Faction);
            _cards[position] = card;
            
            // 创建卡牌视图
            CellView cellView = board.GetCellView(position.x, position.y);
            if (cellView != null)
            {
                // 使用单元格的实际位置，并稍微调整Z坐标使卡牌显示在单元格上方
                Vector3 cardPosition = cellView.transform.position;
                cardPosition.z = -0.1f; // 调整Z坐标，使卡牌显示在单元格上方
                Quaternion rotation = Quaternion.Euler(-60, 0, 0);

                GameObject cardObject = Instantiate(cardPrefab, cardPosition, rotation);
                cardObject.name = $"Card_{cardData.Name}_{position.x}_{position.y}";
                
                // 设置卡牌层，使其可以被单独检测
                cardObject.layer = LayerMask.NameToLayer("Card");
                
                CardView cardView = cardObject.GetComponent<CardView>();
                if (cardView != null)
                {
                    cardView.Initialize(card);
                    _cardViews[position] = cardView;
                }
            }
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
                _cardViews[position].SetSelected(true);
            }
        }
        
        // 设置目标位置
        public void SetTargetPosition(Vector2Int position)
        {
            Debug.Log($"CardManager.SetTargetPosition: 位置 {position}");
            _targetPosition = position;
        }
        
        // 获取选中的卡牌
        public Card GetSelectedCard()
        {
            if (_selectedPosition.HasValue && _cards.ContainsKey(_selectedPosition.Value))
                return _cards[_selectedPosition.Value];
            return null;
        }
        
        // 获取指定位置的卡牌
        public Card GetCard(Vector2Int position)
        {
            if (_cards.ContainsKey(position))
                return _cards[position];
            return null;
        }
        
        // 获取指定位置的卡牌视图
        public CardView GetCardView(Vector2Int position)
        {
            if (_cardViews.ContainsKey(position))
                return _cardViews[position];
            return null;
        }
        
        // 获取指定位置的单元格视图
        public CellView GetCellView(int x, int y)
        {
            return board.GetCellView(x, y);
        }
        
        // 检查指定位置是否有卡牌
        public bool HasCard(Vector2Int position)
        {
            return _cards.ContainsKey(position);
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
            Vector2Int position = card.Position;
            
            for (int x = position.x - moveRange; x <= position.x + moveRange; x++)
            {
                for (int y = position.y - moveRange; y <= position.y + moveRange; y++)
                {
                    // 检查是否在棋盘范围内
                    if (x >= 0 && x < BoardWidth && 
                        y >= 0 && y < BoardHeight)
                    {
                        // 计算曼哈顿距离
                        int distance = Mathf.Abs(x - position.x) + Mathf.Abs(y - position.y);
                        if (distance <= moveRange)
                        {
                            // 检查目标位置是否已有卡牌
                            Vector2Int targetPos = new Vector2Int(x, y);
                            if (!HasCard(targetPos))
                            {
                                // 高亮可移动的格子
                                CellView cellView = GetCellView(x, y);
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
        
        // 高亮可攻击的敌方棋子
        public void HighlightAttackableCards(Card card)
        {
            Vector2Int position = card.Position;
            
            for (int x = position.x - attackRange; x <= position.x + attackRange; x++)
            {
                for (int y = position.y - attackRange; y <= position.y + attackRange; y++)
                {
                    // 检查是否在棋盘范围内
                    if (x >= 0 && x < BoardWidth && 
                        y >= 0 && y < BoardHeight)
                    {
                        // 计算曼哈顿距离
                        int distance = Mathf.Abs(x - position.x) + Mathf.Abs(y - position.y);
                        if (distance <= attackRange)
                        {
                            // 检查目标位置是否有敌方卡牌
                            Vector2Int targetPos = new Vector2Int(x, y);
                            Card targetCard = GetCard(targetPos);
                            if (targetCard != null && targetCard.OwnerId != card.OwnerId)
                            {
                                // 高亮可攻击的敌方棋子所在地块
                                CellView cellView = GetCellView(targetPos.x, targetPos.y);
                                if (cellView != null)
                                {
                                    cellView.SetHighlight(CellView.HighlightType.Attack);
                                }
                                
                                // 同时高亮敌方棋子
                                CardView cardView = GetCardView(targetPos);
                                if (cardView != null)
                                {
                                    cardView.SetAttackable(true);
                                }
                            }
                        }
                    }
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
        
        // 检查是否可以移动到指定位置
        public bool CanMoveTo(Vector2Int position)
        {
            if (!_selectedPosition.HasValue)
                return false;
                
            // 检查目标位置是否已有卡牌
            if (_cards.ContainsKey(position))
                return false;
                
            Vector2Int pos = _selectedPosition.Value;
            
            // 计算曼哈顿距离
            int distance = Mathf.Abs(position.x - pos.x) + Mathf.Abs(position.y - pos.y);
            return distance <= moveRange;
        }
        
        // 检查是否可以攻击指定位置
        public bool CanAttack(Vector2Int position)
        {
            if (!_selectedPosition.HasValue)
                return false;
                
            // 检查目标位置是否有卡牌
            if (!_cards.ContainsKey(position))
                return false;
                
            Vector2Int pos = _selectedPosition.Value;
            
            // 计算曼哈顿距离
            int distance = Mathf.Abs(position.x - pos.x) + Mathf.Abs(position.y - pos.y);
            return distance <= attackRange;
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
        
        // 卡牌攻击
        private bool CardAttack(Vector2Int attackerPosition, Vector2Int targetPosition)
        {
            if (!_cards.ContainsKey(attackerPosition) || !_cards.ContainsKey(targetPosition))
            {
                Debug.LogWarning("无法执行攻击：攻击者或目标位置没有卡牌");
                return false;
            }
            
            Card attacker = _cards[attackerPosition];
            Card target = _cards[targetPosition];
            
            // 执行攻击
            if (attacker.Attack(target))
            {
                // 播放攻击动画
                CardView attackerView = _cardViews[attackerPosition];
                CardView targetView = _cardViews[targetPosition];
                
                if (attackerView != null && targetView != null)
                {
                    attackerView.PlayAttackAnimation(targetView.transform.position);
                    targetView.PlayDamageAnimation();
                    
                    // 更新目标卡牌视图
                    targetView.UpdateVisuals();
                    
                    // 更新攻击者卡牌视图（标记为已行动）
                    attackerView.UpdateVisuals();
                    
                    // 检查目标是否死亡
                    if (!target.IsAlive())
                    {
                        RemoveCard(targetPosition);
                    }
                }
            }
            
            return true;
        }
        
        // 移除卡牌
        private void RemoveCard(Vector2Int position)
        {
            if (!_cards.ContainsKey(position))
                return;
            
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
    }
} 