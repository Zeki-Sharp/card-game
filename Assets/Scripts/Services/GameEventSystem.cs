using UnityEngine;
using System;
using System.Collections.Generic;

namespace ChessGame
{
    /// <summary>
    /// 游戏事件系统 - 负责管理和分发游戏中的各种事件
    /// </summary>
    public class GameEventSystem : MonoBehaviour
    {
        private static GameEventSystem _instance;
        
        public static GameEventSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameEventSystem>();
                    
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("GameEventSystem");
                        _instance = obj.AddComponent<GameEventSystem>();
                        Debug.Log("GameEventSystem实例已自动创建");
                    }
                }
                
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"场景中存在多个GameEventSystem实例，销毁重复的: {gameObject.name}");
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            
            // 只有在需要跨场景保持的情况下才使用DontDestroyOnLoad
            // DontDestroyOnLoad(gameObject);
            
            Debug.Log("GameEventSystem已初始化");
        }
        
        // 卡牌事件
        public event Action<Vector2Int> OnCardSelected;
        public event Action OnCardDeselected;
        public event Action<Vector2Int, Vector2Int> OnCardMoved;
        public event Action<Vector2Int, Vector2Int> OnCardAttacked;
        public event Action<Vector2Int> OnCardRemoved;
        public event Action<Vector2Int, bool> OnCardFlipped;
        public event Action<Vector2Int> OnCardDamaged;
        public event Action<Vector2Int, int, bool> OnCardAdded;
        
        // 回合事件
        public event Action<int> OnTurnStarted;
        public event Action<int> OnTurnEnded;
        
        // 游戏状态事件
        public event Action OnGameStarted;
        public event Action OnGameOver;
        public event Action<int> OnPlayerWon; // 参数为玩家ID
        
        // 添加卡牌行动事件
        public event Action<Vector2Int> OnCardActed;
        
        // 通知卡牌选中
        public void NotifyCardSelected(Vector2Int position)
        {
            Debug.Log($"GameEventSystem: 卡牌选中事件 - 位置 {position}");
            OnCardSelected?.Invoke(position);
        }
        
        // 通知卡牌取消选中
        public void NotifyCardDeselected()
        {
            Debug.Log("GameEventSystem: 卡牌取消选中事件");
            OnCardDeselected?.Invoke();
        }
        
        // 通知卡牌移动
        public void NotifyCardMoved(Vector2Int fromPosition, Vector2Int toPosition)
        {
            Debug.Log($"GameEventSystem: 卡牌移动事件 - 从 {fromPosition} 到 {toPosition}");
            OnCardMoved?.Invoke(fromPosition, toPosition);
        }
        
        // 通知卡牌攻击
        public void NotifyCardAttacked(Vector2Int attackerPosition, Vector2Int targetPosition)
        {
            Debug.Log($"GameEventSystem: 卡牌攻击事件 - 攻击者 {attackerPosition}, 目标 {targetPosition}");
            OnCardAttacked?.Invoke(attackerPosition, targetPosition);
        }
        
        // 通知卡牌移除
        public void NotifyCardRemoved(Vector2Int position)
        {
            Debug.Log($"GameEventSystem: 卡牌移除事件 - 位置 {position}");
            OnCardRemoved?.Invoke(position);
        }
        
        // 通知卡牌翻转
        public void NotifyCardFlipped(Vector2Int position, bool isFaceDown)
        {
            Debug.Log($"GameEventSystem: 卡牌翻转事件 - 位置 {position}");
            OnCardFlipped?.Invoke(position, isFaceDown);
        }
        
        // 通知卡牌受伤
        public void NotifyCardDamaged(Vector2Int position)
        {
            Debug.Log($"GameEventSystem: 卡牌在位置 {position} 受伤");
            
            // 获取卡牌和视图
            CardManager cardManager = FindObjectOfType<CardManager>();
            if (cardManager != null)
            {
                Card card = cardManager.GetCard(position);
                CardView cardView = cardManager.GetCardView(position);
                
                if (card != null && cardView != null)
                {
                    // 立即更新卡牌视图
                    cardView.UpdateVisuals();
                    Debug.Log($"更新卡牌视图: {card.Data.Name}, 生命值: {card.Data.Health}");
                    
                    // 检查卡牌是否死亡
                    if (card.Data.Health <= 0)
                    {
                        Debug.Log($"卡牌 {card.Data.Name} 生命值为 {card.Data.Health}，将被移除");
                        cardManager.RemoveCard(position);
                    }
                }
            }
            
            if (OnCardDamaged != null)
            {
                OnCardDamaged.Invoke(position);
            }
        }
        
        // 通知卡牌添加
        public void NotifyCardAdded(Vector2Int position, int ownerId, bool isFaceDown)
        {
            Debug.Log($"GameEventSystem: 卡牌添加事件 - 位置 {position}, 所有者 {ownerId}, 背面 {isFaceDown}");
            OnCardAdded?.Invoke(position, ownerId, isFaceDown);
        }
        
        // 通知回合开始
        public void NotifyTurnStarted(int playerId)
        {
            Debug.Log($"GameEventSystem: 回合开始事件 - 玩家 {playerId}");
            OnTurnStarted?.Invoke(playerId);
        }
        
        // 通知回合结束
        public void NotifyTurnEnded(int playerId)
        {
            Debug.Log($"GameEventSystem: 回合结束事件 - 玩家 {playerId}");
            OnTurnEnded?.Invoke(playerId);
        }
        
        // 通知游戏开始
        public void NotifyGameStarted()
        {
            Debug.Log("GameEventSystem: 游戏开始事件");
            OnGameStarted?.Invoke();
        }
        
        // 通知游戏结束
        public void NotifyGameOver()
        {
            Debug.Log("GameEventSystem: 游戏结束事件");
            OnGameOver?.Invoke();
        }
        
        // 通知玩家获胜
        public void NotifyPlayerWon(int playerId)
        {
            Debug.Log($"GameEventSystem: 玩家获胜事件 - 玩家 {playerId}");
            OnPlayerWon?.Invoke(playerId);
        }
        
        // 通知卡牌已行动
        public void NotifyCardActed(Vector2Int position)
        {
            Debug.Log($"GameEventSystem: 卡牌行动事件 - 位置 {position}");
            OnCardActed?.Invoke(position);
        }
    }
} 