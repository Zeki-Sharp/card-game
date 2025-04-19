using UnityEngine;
using System;
using ChessGame.FSM.TurnState;

namespace ChessGame
{
    public enum TurnState
    {
        PlayerTurn,
        EnemyTurn
    }

    public class TurnManager : MonoBehaviour
    {
        [SerializeField] private int maxTurnCount = 30;
        
        // 回合状态
        public TurnState CurrentTurn { get; private set; } = TurnState.PlayerTurn;
        
        // 回合计数
        private int _turnCount = 1;
        
        // 回合状态机
        private TurnStateMachine _turnStateMachine;
        
        // 回合变化事件
        public event Action<TurnState> OnTurnChanged;
        
        // 回合计数变化事件
        public event Action<int> OnTurnCountChanged;
        
        private void Awake()
        {
            Debug.Log("TurnManager.Awake");
        }
        
        private void Start()
        {
            Debug.Log("TurnManager.Start");
            
            // 等待CardManager初始化完成后再初始化回合状态机
            CardManager cardManager = FindObjectOfType<CardManager>();
            if (cardManager != null)
            {
                // 创建回合状态机
                _turnStateMachine = new TurnStateMachine(this, cardManager);
                
                // 订阅回合阶段变化事件
                _turnStateMachine.OnPhaseChanged += HandlePhaseChanged;
                
                // 启动回合状态机
                _turnStateMachine.Start();
                
                // 订阅玩家行动完成事件
                GameEventSystem.Instance.OnPlayerActionCompleted += HandlePlayerActionCompleted;
            }
            else
            {
                Debug.LogError("找不到CardManager，无法初始化回合状态机");
            }
        }
        
        private void OnDestroy()
        {
            // 取消订阅事件
            if (_turnStateMachine != null)
            {
                _turnStateMachine.OnPhaseChanged -= HandlePhaseChanged;
            }
            
            // 取消订阅玩家行动完成事件
            if (GameEventSystem.Instance != null)
            {
                GameEventSystem.Instance.OnPlayerActionCompleted -= HandlePlayerActionCompleted;
            }
        }
        
        private void Update()
        {
            // 更新回合状态机
            if (_turnStateMachine != null)
            {
                TurnPhase currentPhase = _turnStateMachine.GetCurrentPhase();
                if (_turnStateMachine._states.ContainsKey(currentPhase))
                {
                    ITurnState currentState = _turnStateMachine._states[currentPhase];
                    currentState.Update();
                }
            }
        }
        
        // 处理回合阶段变化
        private void HandlePhaseChanged(TurnPhase phase)
        {
            Debug.Log($"回合阶段变化: {phase}");
            
            // 根据阶段更新回合状态
            switch (phase)
            {
                case TurnPhase.PlayerTurnStart:
                    // 玩家回合开始时增加回合计数
                    _turnCount++;
                    OnTurnCountChanged?.Invoke(_turnCount);
                    
                    // 更新回合状态
                    CurrentTurn = TurnState.PlayerTurn;
                    OnTurnChanged?.Invoke(CurrentTurn);
                    break;
                    
                case TurnPhase.EnemyTurnStart:
                    // 更新回合状态
                    CurrentTurn = TurnState.EnemyTurn;
                    OnTurnChanged?.Invoke(CurrentTurn);
                    break;
            }
        }
        
        // 结束玩家回合
        public void EndPlayerTurn()
        {
            Debug.Log("TurnManager.EndPlayerTurn");
            
            if (_turnStateMachine != null && _turnStateMachine.GetCurrentPhase() == TurnPhase.PlayerMainPhase)
            {
                _turnStateMachine.EndCurrentTurn();
            }
        }
        
        // 结束敌方回合
        public void EndEnemyTurn()
        {
            Debug.Log("TurnManager.EndEnemyTurn");
            
            if (_turnStateMachine != null && _turnStateMachine.GetCurrentPhase() == TurnPhase.EnemyMainPhase)
            {
                _turnStateMachine.EndCurrentTurn();
            }
        }
        
        // 检查是否是玩家回合
        public bool IsPlayerTurn()
        {
            return CurrentTurn == TurnState.PlayerTurn;
        }
        
        // 获取当前回合计数
        public int GetTurnCount()
        {
            return _turnCount;
        }
        
        // 重置回合计数
        public void ResetTurnCount()
        {
            _turnCount = 0;
            OnTurnCountChanged?.Invoke(_turnCount);
        }
        
        // 重置回合状态
        public void ResetTurns()
        {
            // 重新启动回合状态机
            if (_turnStateMachine != null)
            {
                _turnStateMachine.Start();
            }
        }
        
        // 获取回合状态机
        public TurnStateMachine GetTurnStateMachine()
        {
            return _turnStateMachine;
        }
        
        // 处理玩家行动完成事件
        private void HandlePlayerActionCompleted(int playerId)
        {
            Debug.Log($"TurnManager.HandlePlayerActionCompleted: 玩家ID {playerId}");
            
            // 如果是玩家行动完成，结束玩家回合
            if (playerId == 0 && IsPlayerTurn())
            {
                Debug.Log("玩家行动完成，结束回合");
                EndPlayerTurn();
            }
        }
    }
} 