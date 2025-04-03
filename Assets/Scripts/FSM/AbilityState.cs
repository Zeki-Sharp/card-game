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
                Debug.LogError("能力状态：选中位置没有卡牌");
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
                Debug.LogError("能力状态：没有找到可触发的能力");
                CompleteState(CardState.Idle);
                return;
            }
            
            // 开始执行能力
            _isExecuting = true;
            CoroutineManager.Instance.StartCoroutineEx(ExecuteAbility());
            
            // 添加测试代码
            // TestDirectAttack();
        }
        
        private IEnumerator ExecuteAbility()
        {
            Debug.Log($"【能力状态】开始执行能力: {_currentAbility.abilityName}, 源卡牌: {_sourceCard.Data.Name}, 目标位置: {_targetPosition}");
            
            // 打印能力的动作序列
            for (int i = 0; i < _currentAbility.actionSequence.Count; i++)
            {
                var action = _currentAbility.actionSequence[i];
                Debug.Log($"【能力状态】动作 {i+1}/{_currentAbility.actionSequence.Count}: {action.actionType}, 目标选择器: {action.targetSelector}");
            }
            
            // 执行能力
            yield return AbilityManager.Instance.ExecuteAbility(_currentAbility, _sourceCard, _targetPosition);
            
            Debug.Log($"【能力状态】能力执行完成: {_currentAbility.abilityName}");
            
            // 标记卡牌已行动
            _sourceCard.HasActed = true;
            
            // 完成状态
            _isExecuting = false;
            CompleteState(CardState.Idle);
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
        
        // 添加一个测试方法，直接执行攻击
        private void TestDirectAttack()
        {
            Debug.Log($"【测试】直接执行攻击: 从 {_sourceCard.Position} 到 {_targetPosition}");
            
            // 获取目标卡牌
            Card targetCard = StateMachine.CardManager.GetCard(_targetPosition);
            if (targetCard != null)
            {
                // 执行攻击
                int damage = StateMachine.CardManager.ExecuteAttack(_sourceCard.Position, _targetPosition);
                Debug.Log($"【测试】攻击完成，造成伤害: {damage}");
            }
            else
            {
                Debug.LogError($"【测试】攻击失败: 目标位置 {_targetPosition} 没有卡牌");
            }
        }
    }
} 