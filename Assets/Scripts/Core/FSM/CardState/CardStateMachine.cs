using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using ChessGame.Utils;

namespace ChessGame.FSM
{
    // 卡牌状态机
    public class CardStateMachine
    {
        // 当前状态
        private ICardState _currentState;
        private CardState _currentStateType;
        
        // 所有状态
        private Dictionary<CardState, ICardState> _states = new Dictionary<CardState, ICardState>();
        
        // 卡牌管理器引用
        public CardManager CardManager { get; private set; }
        
        public CardStateMachine(CardManager cardManager)
        {
            Debug.Log("CardStateMachine构造函数开始执行");
            
            CardManager = cardManager;
            
            try
            {
                // 初始化所有状态
                Debug.Log("开始初始化状态");
                
                var idleState = new IdleState(this);
                idleState.StateCompleted += OnStateCompleted;
                _states[CardState.Idle] = idleState;
                Debug.Log("IdleState初始化成功");
                
                var selectedState = new SelectedState(this);
                selectedState.StateCompleted += OnStateCompleted;
                _states[CardState.Selected] = selectedState;
                Debug.Log("SelectedState初始化成功");
                
                var moveState = new MoveState(this);
                moveState.StateCompleted += OnStateCompleted;
                _states[CardState.Moving] = moveState;
                Debug.Log("MoveState初始化成功");
                
                var attackState = new AttackState(this);
                attackState.StateCompleted += OnStateCompleted;
                _states[CardState.Attacking] = attackState;
                Debug.Log("AttackState初始化成功");
                
                var abilityState = new AbilityState(this);
                abilityState.StateCompleted += OnStateCompleted;
                _states[CardState.Ability] = abilityState;
                Debug.Log("AbilityState初始化成功");
                
                // 设置初始状态 
                Debug.Log("设置初始状态");
                ChangeState(CardState.Idle);
                
                Debug.Log("状态机初始化完成，当前状态：Idle");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"状态机初始化失败: {e.Message}\n{e.StackTrace}");
                throw;
            }
            
            // 订阅GameEventSystem事件
            GameEventSystem.Instance.OnCardSelected += HandleCardSelected;
            GameEventSystem.Instance.OnCardDeselected += HandleCardDeselected;
        }
        
        // 状态完成事件处理
        private void OnStateCompleted(CardState nextState)
        {
            Debug.Log($"状态 {_currentStateType.ToString()} 完成，切换到 {nextState.ToString()}");
            ChangeState(nextState);
        }
        
        // 切换状态
        public void ChangeState(CardState newState)
        {
            string fromState = _currentState?.GetType().Name ?? "null";
            string toState = newState.ToString();
            
            Debug.Log($"状态切换: {fromState} -> {toState}");
            
            if (_currentState != null)
            {
                Debug.Log($"调用 {fromState}.Exit()");
                _currentState.Exit();
            }
            
            _currentStateType = newState;
            _currentState = _states[newState];
            
            Debug.Log($"调用 {toState}.Enter()");
            _currentState.Enter();
            
            Debug.Log($"当前状态已更新为: {toState}");
        }
        
        // 处理单元格点击
        public void HandleCellClick(Vector2Int position)
        {
            Debug.Log($"CardStateMachine.HandleCellClick: 位置 {position}, 当前状态: {_currentStateType}");
            
            if (_currentState != null)
            {
                _currentState.HandleCellClick(position);
            }
        }
        
        // 处理卡牌点击
        public void HandleCardClick(Vector2Int position)
        {
            Debug.Log($"CardStateMachine.HandleCardClick: 位置 {position}, 当前状态: {_currentStateType}");
            
            if (_currentState != null)
            {
                _currentState.HandleCardClick(position);
            }
        }
        
        // 更新状态
        public void Update()
        {
            if (_currentState != null)
            {
                _currentState.Update();
            }
        }
        
        // 获取当前状态类型
        public CardState GetCurrentStateType()
        {
            return _currentStateType;
        }
        
        // 添加事件处理方法
        private void HandleCardSelected(Vector2Int position)
        {
            // 根据当前状态处理卡牌选中事件
            if (_currentState != null)
            {
                // 可以在这里添加特定的逻辑
            }
        }
        
        private void HandleCardDeselected()
        {
            // 根据当前状态处理卡牌取消选中事件
            if (_currentState != null)
            {
                // 可以在这里添加特定的逻辑
            }
        }
        
        // 在析构函数或Dispose方法中取消订阅
        public void Dispose()
        {
            // 取消订阅GameEventSystem事件
            if (GameEventSystem.Instance != null)
            {
                GameEventSystem.Instance.OnCardSelected -= HandleCardSelected;
                GameEventSystem.Instance.OnCardDeselected -= HandleCardDeselected;
            }
        }

        public Coroutine StartCoroutine(IEnumerator routine)
        {
            // 使用CoroutineManager启动协程
            return CoroutineManager.Instance.StartCoroutineEx(routine);
        }

        public void StopCoroutine(Coroutine coroutine)
        {
            // 使用CoroutineManager停止协程
            CoroutineManager.Instance.StopCoroutineEx(coroutine);
        }
    }
} 