using UnityEngine;
using System;
using ChessGame.FSM.TurnState;
using System.Collections;
using ChessGame.Utils;
using ChessGame.Cards; // 添加对Cards命名空间的引用，AbilityManager在这个命名空间中

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
                
                // 订阅敌方行动完成事件
                GameEventSystem.Instance.OnEnemyActionCompleted += HandleEnemyActionCompleted;
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
                GameEventSystem.Instance.OnEnemyActionCompleted -= HandleEnemyActionCompleted;
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
            
            // 检查是否有能力正在执行
            if (AbilityManager.IsExecutingAbility)
            {
                Debug.LogWarning("TurnManager: 检测到能力正在执行，延迟结束玩家回合");
                // 使用协程延迟结束回合
                CoroutineManager.Instance.StartCoroutineEx(DelayEndPlayerTurn());
                return;
            }
            
            // 检查是否有动画正在播放
            if (AnimationManager.Instance != null && AnimationManager.Instance.IsPlayingAnimation)
            {
                Debug.LogWarning("TurnManager: 检测到动画正在播放，延迟结束玩家回合");
                // 使用协程延迟结束回合
                CoroutineManager.Instance.StartCoroutineEx(DelayEndPlayerTurn());
                return;
            }
            
            if (_turnStateMachine != null && _turnStateMachine.GetCurrentPhase() == TurnPhase.PlayerMainPhase)
            {
                _turnStateMachine.EndCurrentTurn();
            }
        }
        
        // 延迟结束玩家回合的协程
        private IEnumerator DelayEndPlayerTurn()
        {
            Debug.Log("等待所有操作完成后再结束玩家回合...");
            
            // 等待能力执行完毕
            while (AbilityManager.IsExecutingAbility)
            {
                yield return new WaitForSeconds(0.1f);
            }
            
            // 等待动画播放完毕
            while (AnimationManager.Instance != null && AnimationManager.Instance.IsPlayingAnimation)
            {
                yield return new WaitForSeconds(0.1f);
            }
            
            Debug.Log("所有操作已完成，现在结束玩家回合");
            
            if (_turnStateMachine != null && _turnStateMachine.GetCurrentPhase() == TurnPhase.PlayerMainPhase)
            {
                _turnStateMachine.EndCurrentTurn();
            }
        }
        
        // 结束敌方回合
        public void EndEnemyTurn()
        {
            Debug.Log("TurnManager.EndEnemyTurn");
            
            // 避免重复调用
            if (_turnStateMachine == null || 
                _turnStateMachine.GetCurrentPhase() != TurnPhase.EnemyMainPhase)
            {
                Debug.LogWarning("尝试结束敌方回合，但不在敌方主要阶段，忽略此调用");
                return;
            }
            
            // 检查是否有能力正在执行
            if (AbilityManager.IsExecutingAbility)
            {
                Debug.LogWarning("TurnManager: 检测到能力正在执行，延迟结束敌方回合");
                // 使用协程延迟结束回合
                CoroutineManager.Instance.StartCoroutineEx(DelayEndEnemyTurn());
                return;
            }
            
            // 检查是否有动画正在播放
            if (AnimationManager.Instance != null && AnimationManager.Instance.IsPlayingAnimation)
            {
                Debug.LogWarning("TurnManager: 检测到动画正在播放，延迟结束敌方回合");
                // 使用协程延迟结束回合
                CoroutineManager.Instance.StartCoroutineEx(DelayEndEnemyTurn());
                return;
            }
            
            // 获取当前状态
            TurnPhase currentPhase = _turnStateMachine.GetCurrentPhase();
            EnemyMainPhase enemyMainPhase = _turnStateMachine._states[currentPhase] as EnemyMainPhase;
            
            // 调用敌方主要阶段的EndTurn方法
            if (enemyMainPhase != null)
            {
                Debug.Log("正在结束敌方回合，调用EnemyMainPhase.EndTurn()");
                enemyMainPhase.EndTurn();
            }
            else
            {
                Debug.LogError("无法获取敌方主要阶段状态");
            }
        }
        
        // 延迟结束敌方回合的协程
        private IEnumerator DelayEndEnemyTurn()
        {
            Debug.Log("等待所有操作完成后再结束敌方回合...");
            
            // 等待能力执行完毕
            while (AbilityManager.IsExecutingAbility)
            {
                yield return new WaitForSeconds(0.1f);
            }
            
            // 等待动画播放完毕
            while (AnimationManager.Instance != null && AnimationManager.Instance.IsPlayingAnimation)
            {
                yield return new WaitForSeconds(0.1f);
            }
            
            Debug.Log("所有操作已完成，现在结束敌方回合");
            
            // 再次检查当前是否还在敌方主要阶段
            if (_turnStateMachine != null && _turnStateMachine.GetCurrentPhase() == TurnPhase.EnemyMainPhase)
            {
                // 获取当前状态
                TurnPhase currentPhase = _turnStateMachine.GetCurrentPhase();
                EnemyMainPhase enemyMainPhase = _turnStateMachine._states[currentPhase] as EnemyMainPhase;
                
                // 调用敌方主要阶段的EndTurn方法
                if (enemyMainPhase != null)
                {
                    Debug.Log("延迟后正在结束敌方回合，调用EnemyMainPhase.EndTurn()");
                    enemyMainPhase.EndTurn();
                }
                else
                {
                    Debug.LogError("无法获取敌方主要阶段状态");
                }
            }
            else
            {
                Debug.LogWarning("延迟结束敌方回合时发现已不在敌方主要阶段，回合可能已结束");
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
        
        // 处理敌方行动完成事件
        private void HandleEnemyActionCompleted(int playerId)
        {
            Debug.Log($"TurnManager.HandleEnemyActionCompleted: 玩家ID {playerId}");
            
            // 如果是敌方行动完成，结束敌方回合
            if (playerId == 1 && !IsPlayerTurn())
            {
                // 添加额外的判断，防止多次调用
                Debug.Log("敌方行动完成，结束回合");
                
                // 先检查当前是否在敌方主要行动阶段
                if (_turnStateMachine != null && 
                    _turnStateMachine.GetCurrentPhase() == TurnPhase.EnemyMainPhase)
                {
                    EndEnemyTurn();
                }
                else
                {
                    Debug.LogWarning("敌方行动完成事件触发，但当前不在敌方主要行动阶段，忽略此事件");
                }
            }
        }
    }
} 