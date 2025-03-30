using UnityEngine;
using System.Collections.Generic;

namespace ChessGame.Cards
{
    public class CardBehaviorManager : MonoBehaviour
    {
        private static CardBehaviorManager _instance;
        public static CardBehaviorManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject obj = new GameObject("CardBehaviorManager");
                    _instance = obj.AddComponent<CardBehaviorManager>();
                    DontDestroyOnLoad(obj);
                }
                return _instance;
            }
        }
        
        private Dictionary<int, IMovementBehavior> _movementBehaviors = new Dictionary<int, IMovementBehavior>();
        private Dictionary<int, IAttackBehavior> _attackBehaviors = new Dictionary<int, IAttackBehavior>();
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        // 为卡牌设置行为
        public void SetCardBehaviors(Card card, MovementType movementType, AttackType attackType)
        {
            int cardId = card.Data.Id;
            
            // 设置移动行为
            if (!_movementBehaviors.ContainsKey(cardId))
            {
                _movementBehaviors[cardId] = BehaviorFactory.CreateMovementBehavior(movementType);
            }
            
            // 设置攻击行为
            if (!_attackBehaviors.ContainsKey(cardId))
            {
                _attackBehaviors[cardId] = BehaviorFactory.CreateAttackBehavior(attackType);
            }
        }
        
        // 获取卡牌的可移动位置
        public List<Vector2Int> GetMovablePositions(Card card, int boardWidth, int boardHeight, Dictionary<Vector2Int, Card> allCards)
        {
            int cardId = card.Data.Id;
            
            if (_movementBehaviors.TryGetValue(cardId, out IMovementBehavior behavior))
            {
                return behavior.GetMovablePositions(card, boardWidth, boardHeight, allCards);
            }
            
            // 如果没有特定行为，使用卡牌自己的方法
            return card.GetMovablePositions(boardWidth, boardHeight, allCards);
        }
        
        // 获取卡牌的可攻击位置
        public List<Vector2Int> GetAttackablePositions(Card card, int boardWidth, int boardHeight, Dictionary<Vector2Int, Card> allCards)
        {
            int cardId = card.Data.Id;
            
            if (_attackBehaviors.TryGetValue(cardId, out IAttackBehavior behavior))
            {
                return behavior.GetAttackablePositions(card, boardWidth, boardHeight, allCards);
            }
            
            // 如果没有特定行为，使用卡牌自己的方法
            return card.GetAttackablePositions(boardWidth, boardHeight, allCards);
        }
        
        // 清除卡牌行为
        public void ClearCardBehavior(int cardId)
        {
            _movementBehaviors.Remove(cardId);
            _attackBehaviors.Remove(cardId);
        }
        
        // 清除所有卡牌行为
        public void ClearAllCardBehaviors()
        {
            _movementBehaviors.Clear();
            _attackBehaviors.Clear();
        }
    }
} 