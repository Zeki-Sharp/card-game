using UnityEngine;
using System.Collections;
using System;

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
                _aiController.ExecuteAITurn();
            }
            else
            {
                Debug.LogWarning("TurnManager.StartEnemyTurn: AIController未找到，自动结束敌方回合");
                EndEnemyTurn();
            }
        }
        
        // 结束敌方回合
        public void EndEnemyTurn()
        {
            Debug.Log("结束敌方回合");
            
            // 开始新的玩家回合
            StartPlayerTurn();
        }
        
        // 检查是否是玩家回合
        public bool IsPlayerTurn()
        {
            return _currentTurn == TurnState.PlayerTurn;
        }
    }
} 