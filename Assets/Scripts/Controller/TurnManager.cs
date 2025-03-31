using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

namespace ChessGame
{
    public enum TurnState
    {
        PlayerTurn,
        EnemyTurn
    }

    public class TurnManager : MonoBehaviour
    {
        [SerializeField] private float aiTurnDelay = 1.0f; // AI行动前的延迟时间
        
        private TurnState _currentTurn = TurnState.PlayerTurn;
        private CardManager _cardManager;
        private AIController _aiController;
        
        // 回合变化事件
        public event Action<TurnState> OnTurnChanged;
        
        // 回合开始事件
        public event System.Action<int> OnTurnStarted;
        
        // 当前回合属性
        public TurnState CurrentTurn => _currentTurn;
        
        // 添加一个标志，表示是否正在切换回合
        private bool _isSwitchingTurn = false;
        
        // 添加回合计数属性
        private int _turnCount = 0;
        public int TurnCount => _turnCount;
        
        private void Awake()
        {
            _cardManager = FindObjectOfType<CardManager>();
            _aiController = FindObjectOfType<AIController>();
            
            if (_cardManager == null)
                Debug.LogError("找不到CardManager组件");
                
            if (_aiController == null)
            {
                Debug.LogWarning("找不到AIController组件，尝试创建一个");
                GameObject aiObject = new GameObject("AIController");
                _aiController = aiObject.AddComponent<AIController>();
            }
        }
        
        private void Start()
        {
            // 开始游戏，默认玩家先行动
            StartPlayerTurn();
        }
        
        private void OnEnable()
        {
            Debug.Log("TurnManager.OnEnable: 组件启用");
        }
        
        private void OnDisable()
        {
            Debug.Log("TurnManager.OnDisable: 组件禁用");
        }
        
        // 开始玩家回合
        public void StartPlayerTurn()
        {
            _currentTurn = TurnState.PlayerTurn;
            _turnCount++;
            Debug.Log($"玩家回合开始，当前回合数: {_turnCount}");
            
            // 重置所有玩家卡牌的行动状态
            if (_cardManager != null)
            {
                _cardManager.ResetPlayerCardActions();
            }
            
            // 触发回合变化事件
            OnTurnChanged?.Invoke(_currentTurn);
            
            // 触发回合开始事件，传递玩家ID (0)
            OnTurnStarted?.Invoke(0);
        }
        
        // 结束玩家回合
        public void EndPlayerTurn()
        {
            Debug.Log("TurnManager.EndPlayerTurn: 结束玩家回合");
            
            // 确保所有玩家卡牌都标记为已行动，防止在敌方回合点击
            CardManager cardManager = FindObjectOfType<CardManager>();
            if (cardManager != null)
            {
                cardManager.MarkAllPlayerCardsAsActed();
            }
            
            // 延迟一段时间后开始AI回合
            StartCoroutine(StartEnemyTurnDelayed());
        }
        
        // 延迟开始AI回合
        private IEnumerator StartEnemyTurnDelayed()
        {
            Debug.Log("TurnManager.StartEnemyTurnDelayed: 延迟开始敌方回合...");
            yield return new WaitForSeconds(aiTurnDelay);
            StartEnemyTurn();
        }
        
        // 开始敌方回合
        public void StartEnemyTurn()
        {
            _currentTurn = TurnState.EnemyTurn;
            Debug.Log("敌方回合开始");
            
            // 触发回合变化事件
            OnTurnChanged?.Invoke(_currentTurn);
            
            // 触发回合开始事件，传递敌方ID (1)
            OnTurnStarted?.Invoke(1);
            
            // 启动AI行动
            if (_aiController != null)
            {
                StartCoroutine(_aiController.ExecuteAITurn());
            }
        }
        
        // 结束敌方回合
        public void EndEnemyTurn()
        {
            // 如果已经在切换回合，则不再重复执行
            if (_isSwitchingTurn)
            {
                Debug.LogWarning("TurnManager.EndEnemyTurn: 已经在切换回合，忽略重复调用");
                return;
            }
            
            Debug.Log("TurnManager.EndEnemyTurn被调用");
            Debug.Log("结束敌方回合");
            
            _isSwitchingTurn = true;
            
            // 重置所有敌方卡牌的行动状态
            if (_cardManager != null)
            {
                Debug.Log("重置所有敌方卡牌的行动状态");
                Dictionary<Vector2Int, Card> allCards = _cardManager.GetAllCards();
                foreach (var cardPair in allCards)
                {
                    Card card = cardPair.Value;
                    if (card.OwnerId == 1) // 敌方卡牌
                    {
                        card.HasActed = false;
                        Debug.Log($"重置敌方卡牌行动状态: 位置 {card.Position}");
                    }
                }
            }
            
            // 使用Invoke延迟调用，确保当前帧处理完成
            Invoke("StartPlayerTurnDelayed", 0.1f);
        }
        
        // 添加一个新方法来延迟开始玩家回合
        private void StartPlayerTurnDelayed()
        {
            _isSwitchingTurn = false;
            StartPlayerTurn();
        }
        
        // 检查是否是玩家回合
        public bool IsPlayerTurn()
        {
            return _currentTurn == TurnState.PlayerTurn;
        }
        
        // 添加重置回合计数的方法
        public void ResetTurnCount()
        {
            _turnCount = 0;
        }

        public void ResetTurns()
        {
            _currentTurn = TurnState.PlayerTurn;
            _turnCount = 0;
        }
    }
} 