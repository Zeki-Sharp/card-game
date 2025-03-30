using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ChessGame.FSM;

namespace ChessGame
{
    /// <summary>
    /// 游戏结束检查器 - 负责检查游戏是否结束并判断胜负
    /// </summary>
    public class GameEndChecker : MonoBehaviour
    {
        [SerializeField] private CardManager cardManager;
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private GameStateManager gameStateManager;
        [SerializeField] private int maxTurnCount = 20; // 最大回合数
        [SerializeField]private List<GameStateManager.CardStateInfo> playerAliveCards = new List<GameStateManager.CardStateInfo>();
        [SerializeField]private List<GameStateManager.CardStateInfo> enemyAliveCards = new List<GameStateManager.CardStateInfo>(); 

        
        private int _currentTurnCount = 0;
        
        private void Awake()
        {
            if (cardManager == null)
                cardManager = FindObjectOfType<CardManager>();
                
            if (turnManager == null)
                turnManager = FindObjectOfType<TurnManager>();
        }
        
        private void Start()
        {
            // 订阅回合变化事件
            if (turnManager != null)
            {
                turnManager.OnTurnChanged += OnTurnChanged;
            }
            
            // 订阅卡牌移除事件
            if (GameEventSystem.Instance != null)
            {
                GameEventSystem.Instance.OnCardRemoved += CheckGameEndAfterCardRemoved;
            }
        }
        
        private void OnDestroy()
        {
            // 取消订阅事件
            if (turnManager != null)
            {
                turnManager.OnTurnChanged -= OnTurnChanged;
            }
            
            if (GameEventSystem.Instance != null)
            {
                GameEventSystem.Instance.OnCardRemoved -= CheckGameEndAfterCardRemoved;
            }
        }
        
        // 处理回合变化
        private void OnTurnChanged(TurnState turnState)
        {
            // 只在玩家回合开始时增加回合计数
            if (turnState == TurnState.PlayerTurn)
            {
                _currentTurnCount++;
                Debug.Log($"当前回合数: {_currentTurnCount}/{maxTurnCount}");
                
                // 检查是否达到最大回合数
                if (_currentTurnCount >= maxTurnCount)
                {
                    Debug.Log($"已达到最大回合数 {maxTurnCount}，游戏结束");
                    DetermineWinnerByHealthSum();
                }
            }
        }
        
        // 卡牌移除后检查游戏是否结束
        private void CheckGameEndAfterCardRemoved(Vector2Int position)
        {
            // 延迟一帧检查，确保卡牌已完全移除
            Invoke("CheckGameEndByCardCount", 0.1f);
        }
        
        // 根据卡牌数量检查游戏是否结束
        private void CheckGameEndByCardCount()
        {
            if (gameStateManager == null) return;
            
            int playerCardCount = gameStateManager.GetPlayerCardCount();
            int enemyCardCount = gameStateManager.GetEnemyCardCount();
            
            Debug.Log($"当前卡牌数量 - 玩家: {playerCardCount}, 敌方: {enemyCardCount}");
            
            // 检查是否一方没有卡牌了
            if (playerCardCount == 0 || enemyCardCount == 0)
            {
                if (playerCardCount == 0 && enemyCardCount == 0)
                {
                    Debug.Log("双方卡牌都已消灭，游戏平局");
                    GameOver(null);
                }
                else if (playerCardCount == 0)
                {
                    Debug.Log("玩家卡牌全部消灭，敌方获胜");
                    GameOver(1); // 敌方获胜
                }
                else // enemyCardCount == 0
                {
                    Debug.Log("敌方卡牌全部消灭，玩家获胜");
                    GameOver(0); // 玩家获胜
                }
            }
        }
        
        // 根据生命值总和判断胜负
        private void DetermineWinnerByHealthSum()
        {
            if (cardManager == null) return;
            
            playerAliveCards = gameStateManager.GetPlayerAliveCards();
            enemyAliveCards = gameStateManager.GetEnemyAliveCards();
            
            int playerHealthSum = 0;
            int enemyHealthSum = 0;
            
            // 计算双方生命值总和
            foreach (var card in playerAliveCards)
            {
                playerHealthSum += card.health;
            }
            
            foreach (var card in enemyAliveCards)
            {
                enemyHealthSum += card.health;
            }
            
            Debug.Log($"生命值总和 - 玩家: {playerHealthSum}, 敌方: {enemyHealthSum}");
            
            // 判断胜负
            if (playerHealthSum > enemyHealthSum)
            {
                Debug.Log("玩家生命值总和更高，玩家获胜");
                GameOver(0); // 玩家获胜
            }
            else if (enemyHealthSum > playerHealthSum)
            {
                Debug.Log("敌方生命值总和更高，敌方获胜");
                GameOver(1); // 敌方获胜
            }
            else
            {
                Debug.Log("双方生命值总和相等，游戏平局");
                GameOver(null); // 平局
            }
        }
        
        // 游戏结束处理
        private void GameOver(int? winnerId)
        {
            Debug.Log("游戏结束，禁用所有游戏交互");
            
            // 停止游戏逻辑
            if (turnManager != null)
            {
                // 禁用回合管理器，防止继续切换回合
                turnManager.enabled = false;
            }
            
            // 禁用卡牌管理器，防止继续选择和移动卡牌
            if (cardManager != null)
            {
                cardManager.enabled = false;
            }
            
            // 禁用AI控制器，防止AI继续行动
            AIController aiController = FindObjectOfType<AIController>();
            if (aiController != null)
            {
                aiController.enabled = false;
            }
            
            // 禁用卡牌状态机，防止继续处理卡牌状态变化
            CardStateMachine stateMachine = cardManager?.GetCardStateMachine();
            if (stateMachine != null)
            {
                // 如果CardStateMachine是MonoBehaviour，可以直接禁用
                // 如果是普通类，需要设置一个标志或调用特定方法
                stateMachine.Dispose(); // 假设有Dispose方法
            }
            
            // 触发游戏结束事件
            GameEventSystem.Instance.NotifyGameOver();
            
            // 如果有胜者，触发胜利事件
            if (winnerId.HasValue)
            {
                GameEventSystem.Instance.NotifyPlayerWon(winnerId.Value);
                
                if (winnerId.Value == 0)
                {
                    Debug.Log("游戏结束：玩家获胜！");
                    // 这里可以显示胜利UI、播放胜利动画等
                }
                else
                {
                    Debug.Log("游戏结束：敌方获胜！");
                    // 这里可以显示失败UI、播放失败动画等
                }
            }
            else
            {
                Debug.Log("游戏结束：平局！");
                // 这里可以显示平局UI
            }
            
            // 显示重新开始按钮或返回主菜单按钮
            Debug.Log("显示游戏结束UI，提供重新开始或返回主菜单选项");
        }
        
        // 重置游戏
        public void ResetGame()
        {
            _currentTurnCount = 0;
            
            // 重新启用回合管理器
            if (turnManager != null)
            {
                turnManager.enabled = true;
            }
            
            // 重新启用卡牌管理器
            if (cardManager != null)
            {
                cardManager.enabled = true;
            }
            
            // 重新启用AI控制器
            AIController aiController = FindObjectOfType<AIController>();
            if (aiController != null)
            {
                aiController.enabled = true;
            }
        }
        
        // 设置最大回合数
        public void SetMaxTurnCount(int maxTurns)
        {
            maxTurnCount = maxTurns;
            Debug.Log($"设置最大回合数为: {maxTurnCount}");
        }
    }
} 