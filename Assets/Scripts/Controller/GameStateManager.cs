using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Linq;

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
                GameEventSystem.Instance.OnCardRemoved += OnCardRemoved;
                GameEventSystem.Instance.OnCardFlipped += OnCardFlipped;
                GameEventSystem.Instance.OnCardAdded += OnCardAdded;
            }
            
            // 延迟初始化卡牌状态，确保所有卡牌都已生成
            Invoke("InitializeCardStates", 1.0f);
            
            // 订阅卡牌移除事件
            if (GameEventSystem.Instance != null)
            {
                GameEventSystem.Instance.OnCardRemoved += CheckAndFlipCard;
            }
        }
        
        private void OnDestroy()
        {
            // 取消订阅事件
            if (cardManager != null)
            {
                GameEventSystem.Instance.OnCardRemoved -= OnCardRemoved;
                GameEventSystem.Instance.OnCardFlipped -= OnCardFlipped;
                GameEventSystem.Instance.OnCardAdded -= OnCardAdded;
            }
            
            if (GameEventSystem.Instance != null)
            {
                GameEventSystem.Instance.OnCardRemoved -= CheckAndFlipCard;
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
            
            // 更新卡牌在状态列表中的分类
            UpdateCardListClassification(card, position);
            
            Debug.Log($"GameStateManager: 卡牌翻面事件处理完成，位置: {position}, 是否背面: {isFaceDown}");
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

        //获取玩家存活的卡牌列表
        public List<CardStateInfo> GetPlayerAliveCards()
        {
            return playerFaceUpCards.Where(c => c.health > 0).ToList();
        }

        // 获取玩家卡牌总数
        public int GetPlayerCardCount()
        {
            return playerFaceUpCards.Count + playerFaceDownCards.Count;
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

        //获取敌方存活的卡牌列表
        public List<CardStateInfo> GetEnemyAliveCards()
        {
            return enemyFaceUpCards.Where(c => c.health > 0).ToList();
        }

        // 获取敌方卡牌总数
        public int GetEnemyCardCount()
        {
            return enemyFaceUpCards.Count + enemyFaceDownCards.Count;
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
        
        // 更新卡牌在状态列表中的分类
        private void UpdateCardListClassification(Card card, Vector2Int position)
        {
            // 从所有列表中移除
            RemoveCardFromLists(position);
            
            // 重新添加到正确的列表
            AddCardToLists(position, card);
        }
        
        // 检查并翻开卡牌的方法
        private void CheckAndFlipCard(Vector2Int removedPosition)
        {
            // 延迟一帧执行，确保卡牌已经被完全移除
            StartCoroutine(CheckAndFlipCardDelayed());
        }
        
        private IEnumerator CheckAndFlipCardDelayed()
        {
            // 等待一帧，确保卡牌状态已更新
            yield return null;
            
            // 更新卡牌状态
            UpdateCardStates();
            
            // 检查玩家卡牌
            if (playerFaceUpCards.Count == 0 && playerFaceDownCards.Count > 0)
            {
                Debug.Log("玩家没有正面卡牌，自动翻开一张背面卡牌");
                FlipRandomCard(0); // 0 表示玩家
            }
            
            // 检查敌方卡牌
            if (enemyFaceUpCards.Count == 0 && enemyFaceDownCards.Count > 0)
            {
                Debug.Log("敌方没有正面卡牌，自动翻开一张背面卡牌");
                FlipRandomCard(1); // 1 表示敌方
            }
        }
        
        // 随机翻开一张指定所有者的背面卡牌
        private void FlipRandomCard(int ownerId)
        {
            List<CardStateInfo> faceDownCards = ownerId == 0 ? playerFaceDownCards : enemyFaceDownCards;
            
            if (faceDownCards.Count == 0)
                return;
            
            // 随机选择一张背面卡牌
            int randomIndex = UnityEngine.Random.Range(0, faceDownCards.Count);
            CardStateInfo cardInfo = faceDownCards[randomIndex];
            
            Debug.Log($"随机选择翻开卡牌: {cardInfo.cardName} 在位置 {cardInfo.position}");
            
            // 获取卡牌管理器
            CardManager cardManager = FindObjectOfType<CardManager>();
            if (cardManager != null)
            {
                // 翻开卡牌
                cardManager.FlipCard(cardInfo.position);
                
                // 更新卡牌状态
                UpdateCardStates();
                
                Debug.Log($"自动翻开卡牌完成: {cardInfo.cardName}");
            }
            else
            {
                Debug.LogError("找不到 CardManager，无法翻开卡牌");
            }
        }
        
        // 更新所有卡牌状态
        private void UpdateCardStates()
        {
            // 清空所有列表
            ClearAllLists();
            
            // 获取所有卡牌
            Dictionary<Vector2Int, Card> allCards = cardManager.GetAllCards();
            
            // 遍历所有卡牌，根据状态分类
            foreach (var kvp in allCards)
            {
                Vector2Int position = kvp.Key;
                Card card = kvp.Value;
                
                // 添加到相应列表
                AddCardToLists(position, card);
            }
            
            // 输出调试信息
            Debug.Log($"更新卡牌状态 - 玩家正面: {playerFaceUpCards.Count}, 玩家背面: {playerFaceDownCards.Count}, " +
                      $"敌方正面: {enemyFaceUpCards.Count}, 敌方背面: {enemyFaceDownCards.Count}");
        }
    }
} 