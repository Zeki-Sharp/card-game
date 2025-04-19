using System.Collections;
using UnityEngine;
using ChessGame.Cards;
using ChessGame.Utils;

namespace ChessGame.FSM
{
    /// <summary>
    /// 能力状态 - 处理卡牌能力的执行
    /// </summary>
    public class AbilityState : CardStateBase
    {
        private AbilityConfiguration _currentAbility;
        private Card _sourceCard;
        private Vector2Int _targetPosition;
        private bool _isExecuting = false;
        
        // 添加构造函数
        public AbilityState(CardStateMachine stateMachine) : base(stateMachine)
        {
        }
        
        public override void Enter()
        {
            Debug.Log("进入能力状态");
            
            // 获取当前选中的卡牌
            Vector2Int? selectedPosition = StateMachine.CardManager.GetSelectedPosition();
            if (!selectedPosition.HasValue)
            {
                Debug.LogError("能力状态：没有选中的卡牌");
                CompleteState(CardState.Idle);
                return;
            }
            
            // 获取目标位置
            Vector2Int? targetPosition = StateMachine.CardManager.GetTargetPosition();
            if (!targetPosition.HasValue)
            {
                Debug.LogError("能力状态：没有目标位置");
                CompleteState(CardState.Idle);
                return;
            }
            
            _sourceCard = StateMachine.CardManager.GetCard(selectedPosition.Value);
            _targetPosition = targetPosition.Value;
            
            if (_sourceCard == null)
            {
                Debug.LogError("能力状态：没有选中的卡牌");
                CompleteState(CardState.Idle);
                return;
            }
            
            // 查找可触发的能力
            foreach (var ability in _sourceCard.GetAbilities())
            {
                if (_sourceCard.CanTriggerAbility(ability, _targetPosition, StateMachine.CardManager))
                {
                    _currentAbility = ability;
                    break;
                }
            }
            
            if (_currentAbility == null)
            {
                Debug.LogError("能力状态：没有当前能力");
                CompleteState(CardState.Idle);
                return;
            }
            
            Debug.Log($"能力状态：执行能力 {_currentAbility.abilityName}，源卡牌：{_sourceCard.Data.Name}，目标位置：{_targetPosition}");
            
            // 开始执行能力
            _isExecuting = true;
            CoroutineManager.Instance.StartCoroutineEx(ExecuteAbility());
        }
        
        private IEnumerator ExecuteAbility()
        {
            Debug.Log($"开始执行能力: {_currentAbility.abilityName}");
            
            // 执行能力
            yield return AbilityManager.Instance.ExecuteAbility(_currentAbility, _sourceCard, _targetPosition);
            
            Debug.Log($"能力执行完成: {_currentAbility.abilityName}");
            
            // 标记卡牌已行动
            _sourceCard.HasActed = true;
            
            // 检查是否应该结束回合
            CheckEndTurn();
            
            // 完成状态
            _isExecuting = false;
            CompleteState(CardState.Idle);
        }
        
        // 添加检查结束回合的方法
        private void CheckEndTurn()
        {
            TurnManager turnManager = StateMachine.CardManager.GetTurnManager();
            if (turnManager == null) return;

            if (turnManager.IsPlayerTurn())
            {
                Debug.Log("任意玩家卡牌使用能力后立即结束回合");
                turnManager.EndPlayerTurn();
            }
        }
        
        public override void Exit()
        {
            Debug.Log("退出能力状态");
            
            // 清除引用
            _currentAbility = null;
            _sourceCard = null;
        }
        
        // 实现抽象方法
        public override void HandleCardClick(Vector2Int position)
        {
            // 能力执行过程中忽略点击
            if (_isExecuting)
            {
                Debug.Log("能力执行中，忽略点击");
                return;
            }
            
            // 如果能力已执行完毕，切换到空闲状态
            CompleteState(CardState.Idle);
        }
        
        // 保留原有方法，但不再是抽象方法的实现
        public override void HandleCellClick(Vector2Int position)
        {
            HandleCardClick(position);
        }
        
        public override void Update()
        {
            // 能力状态不需要特殊更新
        }
    }
} 