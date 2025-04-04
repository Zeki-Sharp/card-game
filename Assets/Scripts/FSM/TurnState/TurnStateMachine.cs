using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChessGame.FSM.TurnState
{
    /// <summary>
    /// 回合状态机 - 管理游戏回合的不同阶段
    /// </summary>
    public class TurnStateMachine
    {
        private ITurnState _currentState;
        private TurnPhase _currentPhase;
        
        // 所有状态
        public Dictionary<TurnPhase, ITurnState> _states = new Dictionary<TurnPhase, ITurnState>();
        
        // 回合管理器引用
        private TurnManager _turnManager;
        
        // 卡牌管理器引用
        private CardManager _cardManager;
        
        // 当前玩家ID (0为玩家，1为AI)
        private int _currentPlayerId;
        
        // 回合阶段变化事件
        public event Action<TurnPhase> OnPhaseChanged;
        
        // 添加一个标志来指示是否允许玩家输入
        private bool _allowPlayerInput = false;
        
        // 添加公共属性来访问这个标志
        public bool AllowPlayerInput
        {
            get { return _allowPlayerInput; }
            private set { _allowPlayerInput = value; }
        }
        
        public TurnStateMachine(TurnManager turnManager, CardManager cardManager)
        {
            Debug.Log("TurnStateMachine构造函数开始执行");
            
            _turnManager = turnManager;
            _cardManager = cardManager;
            
            try
            {
                // 初始化所有状态
                Debug.Log("开始初始化回合状态");
                
                // 玩家回合状态
                var playerStartPhase = new PlayerTurnStartPhase(this);
                playerStartPhase.PhaseCompleted += OnPhaseCompleted;
                _states[TurnPhase.PlayerTurnStart] = playerStartPhase;
                
                var playerMainPhase = new PlayerMainPhase(this);
                playerMainPhase.PhaseCompleted += OnPhaseCompleted;
                _states[TurnPhase.PlayerMainPhase] = playerMainPhase;
                
                var playerEndPhase = new PlayerTurnEndPhase(this);
                playerEndPhase.PhaseCompleted += OnPhaseCompleted;
                _states[TurnPhase.PlayerTurnEnd] = playerEndPhase;
                
                // 敌方回合状态
                var enemyStartPhase = new EnemyTurnStartPhase(this);
                enemyStartPhase.PhaseCompleted += OnPhaseCompleted;
                _states[TurnPhase.EnemyTurnStart] = enemyStartPhase;
                
                var enemyMainPhase = new EnemyMainPhase(this);
                enemyMainPhase.PhaseCompleted += OnPhaseCompleted;
                _states[TurnPhase.EnemyMainPhase] = enemyMainPhase;
                
                var enemyEndPhase = new EnemyTurnEndPhase(this);
                enemyEndPhase.PhaseCompleted += OnPhaseCompleted;
                _states[TurnPhase.EnemyTurnEnd] = enemyEndPhase;
                
                Debug.Log("回合状态初始化完成");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"回合状态机初始化失败: {e.Message}\n{e.StackTrace}");
                throw;
            }
        }
        
        // 启动状态机，从玩家回合开始
        public void Start()
        {
            _currentPlayerId = 0; // 玩家先手
            ChangePhase(TurnPhase.PlayerTurnStart);
        }
        
        // 切换到下一个玩家
        public void SwitchPlayer()
        {
            _currentPlayerId = 1 - _currentPlayerId; // 在0和1之间切换
            
            // 根据当前玩家切换到对应的回合开始阶段
            if (_currentPlayerId == 0)
            {
                ChangePhase(TurnPhase.PlayerTurnStart);
            }
            else
            {
                ChangePhase(TurnPhase.EnemyTurnStart);
            }
        }
        
        // 切换到下一个阶段
        public void ChangePhase(TurnPhase newPhase)
        {
            Debug.Log($"回合状态机切换阶段: {_currentPhase} -> {newPhase}");
            
            // 退出当前状态
            if (_currentState != null)
            {
                _currentState.Exit();
            }
            
            // 更新当前阶段
            _currentPhase = newPhase;
            
            // 获取新状态
            if (_states.TryGetValue(newPhase, out ITurnState newState))
            {
                _currentState = newState;
                _currentState.Enter();
            }
            else
            {
                Debug.LogError($"找不到回合阶段 {newPhase} 对应的状态");
            }
            
            // 更新玩家输入标志 - 只有在玩家主要回合阶段允许输入
            _allowPlayerInput = (newPhase == TurnPhase.PlayerMainPhase);
            
            Debug.Log($"回合阶段变更为 {newPhase}，玩家输入状态: {(_allowPlayerInput ? "允许" : "禁止")}");
            
            // 触发阶段变化事件
            OnPhaseChanged?.Invoke(newPhase);
        }
        
        // 处理阶段完成事件
        private void OnPhaseCompleted(TurnPhase nextPhase)
        {
            ChangePhase(nextPhase);
        }
        
        // 获取当前阶段
        public TurnPhase GetCurrentPhase()
        {
            return _currentPhase;
        }
        
        // 获取当前玩家ID
        public int GetCurrentPlayerId()
        {
            return _currentPlayerId;
        }
        
        // 获取回合管理器
        public TurnManager GetTurnManager()
        {
            return _turnManager;
        }
        
        // 获取卡牌管理器
        public CardManager GetCardManager()
        {
            return _cardManager;
        }
        
        // 是否是玩家回合
        public bool IsPlayerTurn()
        {
            return _currentPlayerId == 0;
        }
        
        // 结束当前玩家的回合
        public void EndCurrentTurn()
        {
            if (_currentPlayerId == 0)
            {
                // 如果是玩家回合，切换到玩家回合结束阶段
                ChangePhase(TurnPhase.PlayerTurnEnd);
            }
            else
            {
                // 如果是敌方回合，切换到敌方回合结束阶段
                ChangePhase(TurnPhase.EnemyTurnEnd);
            }
        }
    }
} 