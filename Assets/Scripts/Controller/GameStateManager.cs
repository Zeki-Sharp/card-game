using System.Collections.Generic;
using UnityEngine;
using System;

namespace ChessGame
{
    public class GameStateManager : MonoBehaviour
    {
        [SerializeField] private CardManager cardManager;
        
        // 在Inspector中显示卡牌状态列表
        [Header("玩家卡牌")]
        [SerializeField] private List<CardStateInfo> playerFaceUpCards = new List<CardStateInfo>();
        [SerializeField] private List<CardStateInfo> playerFaceDownCards = new List<CardStateInfo>();
        
        [Header("敌方卡牌")]
        [SerializeField] private List<CardStateInfo> enemyFaceUpCards = new List<CardStateInfo>();
        [SerializeField] private List<CardStateInfo> enemyFaceDownCards = new List<CardStateInfo>();
        
        // 卡牌状态信息类 (用于在Inspector中显示)
        [System.Serializable]
        public class CardStateInfo
        {
            public Vector2Int position;
            public string cardName;
            public int health;
            public int attack;
            
            public CardStateInfo(Vector2Int pos, string name, int hp, int atk)
            {
                position = pos;
                cardName = name;
                health = hp;
                attack = atk;
            }
        }
        
        private void Awake()
        {
            if (cardManager == null)
                cardManager = FindObjectOfType<CardManager>();
                
            if (cardManager == null)
                Debug.LogError("找不到CardManager组件");
        }
        
        private void Start()
        {
            // 订阅事件
            if (cardManager != null)
            {
                cardManager.OnCardRemoved += OnCardRemoved;
                cardManager.OnCardFlipped += OnCardFlipped;
                cardManager.OnCardAdded += OnCardAdded;
            }
            
            // 延迟初始化卡牌状态，确保所有卡牌都已生成
            Invoke("InitializeCardStates", 1.0f);
        }
        
        private void OnDestroy()
        {
            // 取消订阅事件
            if (cardManager != null)
            {
                cardManager.OnCardRemoved -= OnCardRemoved;
                cardManager.OnCardFlipped -= OnCardFlipped;
                cardManager.OnCardAdded -= OnCardAdded;
            }
        }
        
        // 初始化卡牌状态
        private void InitializeCardStates()
        {
            // 清空所有列表
            ClearAllLists();
            
            // 获取所有卡牌
            Dictionary<Vector2Int, Card> cards = cardManager.GetAllCards();
            
            Debug.Log($"GameStateManager: 初始化卡牌状态，找到 {cards.Count} 张卡牌");
            
            // 遍历所有卡牌，添加到对应列表
            foreach (var kvp in cards)
            {
                Vector2Int position = kvp.Key;
                Card card = kvp.Value;
                
                AddCardToLists(position, card);
            }
        }
        
        // 清空所有列表
        private void ClearAllLists()
        {
            playerFaceUpCards.Clear();
            playerFaceDownCards.Clear();
            enemyFaceUpCards.Clear();
            enemyFaceDownCards.Clear();
        }
        
        // 添加卡牌到对应列表
        private void AddCardToLists(Vector2Int position, Card card)
        {
            CardStateInfo info = new CardStateInfo(
                position, 
                card.Data.Name, 
                card.Data.Health, 
                card.Data.Attack
            );
            
            if (card.OwnerId == 0) // 玩家卡牌
            {
                if (card.IsFaceDown)
                {
                    playerFaceDownCards.Add(info);
                }
                else
                {
                    playerFaceUpCards.Add(info);
                }
            }
            else // 敌方卡牌
            {
                if (card.IsFaceDown)
                {
                    enemyFaceDownCards.Add(info);
                }
                else
                {
                    enemyFaceUpCards.Add(info);
                }
            }
        }
        
        // 从所有列表中移除指定位置的卡牌
        private void RemoveCardFromLists(Vector2Int position)
        {
            playerFaceUpCards.RemoveAll(c => c.position == position);
            playerFaceDownCards.RemoveAll(c => c.position == position);
            enemyFaceUpCards.RemoveAll(c => c.position == position);
            enemyFaceDownCards.RemoveAll(c => c.position == position);
        }
        
        // 卡牌被移除时的处理
        private void OnCardRemoved(Vector2Int position)
        {
            RemoveCardFromLists(position);
        }
        
        // 卡牌翻面时的处理
        private void OnCardFlipped(Vector2Int position, bool isFaceDown)
        {
            // 获取卡牌
            Card card = cardManager.GetCard(position);
            if (card == null) return;
            
            // 从所有列表中移除
            RemoveCardFromLists(position);
            
            // 重新添加到正确的列表
            AddCardToLists(position, card);
        }
        
        // 卡牌添加时的处理
        private void OnCardAdded(Vector2Int position, int ownerId, bool isFaceDown)
        {
            // 获取卡牌
            Card card = cardManager.GetCard(position);
            if (card == null) return;
            
            // 添加到对应列表
            AddCardToLists(position, card);
        }
        
        // 获取玩家正面卡牌列表
        public List<CardStateInfo> GetPlayerFaceUpCards()
        {
            return new List<CardStateInfo>(playerFaceUpCards);
        }
        
        // 获取玩家背面卡牌列表
        public List<CardStateInfo> GetPlayerFaceDownCards()
        {
            return new List<CardStateInfo>(playerFaceDownCards);
        }
        
        // 获取敌方正面卡牌列表
        public List<CardStateInfo> GetEnemyFaceUpCards()
        {
            return new List<CardStateInfo>(enemyFaceUpCards);
        }
        
        // 获取敌方背面卡牌列表
        public List<CardStateInfo> GetEnemyFaceDownCards()
        {
            return new List<CardStateInfo>(enemyFaceDownCards);
        }
        
        // 检查玩家是否有正面卡牌
        public bool HasPlayerFaceUpCards()
        {
            return playerFaceUpCards.Count > 0;
        }
        
        // 检查玩家是否有背面卡牌
        public bool HasPlayerFaceDownCards()
        {
            return playerFaceDownCards.Count > 0;
        }
        
        // 检查敌方是否有正面卡牌
        public bool HasEnemyFaceUpCards()
        {
            return enemyFaceUpCards.Count > 0;
        }
        
        // 检查敌方是否有背面卡牌
        public bool HasEnemyFaceDownCards()
        {
            return enemyFaceDownCards.Count > 0;
        }
        
        // 获取随机玩家背面卡牌位置
        public Vector2Int GetRandomPlayerFaceDownCardPosition()
        {
            if (playerFaceDownCards.Count == 0)
                return new Vector2Int(-1, -1);
                
            int index = UnityEngine.Random.Range(0, playerFaceDownCards.Count);
            return playerFaceDownCards[index].position;
        }
        
        // 获取随机敌方背面卡牌位置
        public Vector2Int GetRandomEnemyFaceDownCardPosition()
        {
            if (enemyFaceDownCards.Count == 0)
                return new Vector2Int(-1, -1);
                
            int index = UnityEngine.Random.Range(0, enemyFaceDownCards.Count);
            return enemyFaceDownCards[index].position;
        }
        
        // 手动刷新卡牌状态
        [ContextMenu("刷新卡牌状态")]
        public void RefreshCardStates()
        {
            InitializeCardStates();
        }
        
        // 输出卡牌状态
        [ContextMenu("输出卡牌状态")]
        private void LogCardStates()
        {
            Debug.Log($"玩家正面卡牌: {playerFaceUpCards.Count}, 背面卡牌: {playerFaceDownCards.Count}");
            Debug.Log($"敌方正面卡牌: {enemyFaceUpCards.Count}, 背面卡牌: {enemyFaceDownCards.Count}");
            
            Debug.Log("玩家正面卡牌:");
            foreach (var card in playerFaceUpCards)
            {
                Debug.Log($"  位置: {card.position}, 名称: {card.cardName}, 生命: {card.health}, 攻击: {card.attack}");
            }
            
            Debug.Log("玩家背面卡牌:");
            foreach (var card in playerFaceDownCards)
            {
                Debug.Log($"  位置: {card.position}, 名称: {card.cardName}");
            }
            
            Debug.Log("敌方正面卡牌:");
            foreach (var card in enemyFaceUpCards)
            {
                Debug.Log($"  位置: {card.position}, 名称: {card.cardName}, 生命: {card.health}, 攻击: {card.attack}");
            }
            
            Debug.Log("敌方背面卡牌:");
            foreach (var card in enemyFaceDownCards)
            {
                Debug.Log($"  位置: {card.position}, 名称: {card.cardName}");
            }
        }
    }
} 