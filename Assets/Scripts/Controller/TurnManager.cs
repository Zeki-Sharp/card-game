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
        
        // 当前回合属性
        public TurnState CurrentTurn => _currentTurn;
        
        // 添加一个标志，表示是否正在切换回合
        private bool _isSwitchingTurn = false;
        
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
            Debug.Log("开始玩家回合");
            _currentTurn = TurnState.PlayerTurn;
            
            // 重置所有卡牌的行动状态
            _cardManager.ResetAllCardActions();
            
            // 触发回合变化事件
            OnTurnChanged?.Invoke(_currentTurn);
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
            Debug.Log("TurnManager.StartEnemyTurn: 开始敌方回合");
            _currentTurn = TurnState.EnemyTurn;
            
            // 触发回合变化事件
            OnTurnChanged?.Invoke(_currentTurn);
            
            // 让AI执行行动
            if (_aiController != null)
            {
                Debug.Log("TurnManager.StartEnemyTurn: 调用AI执行行动");
                
                // 使用Invoke延迟调用，确保UI更新完成
                Invoke("CallAIExecuteTurn", 0.1f);
            }
            else
            {
                Debug.LogWarning("TurnManager.StartEnemyTurn: AIController未找到，自动结束敌方回合");
                EndEnemyTurn();
            }
        }
        
        // 添加一个新方法来调用AI执行回合
        private void CallAIExecuteTurn()
        {
            if (_aiController != null)
            {
                _aiController.ExecuteAITurn();
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
    }
} 