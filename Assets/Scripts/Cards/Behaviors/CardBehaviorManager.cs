using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
            string cardName = card.Data.Name;
            
            // 设置移动行为
            if (!_movementBehaviors.ContainsKey(cardId))
            {
                _movementBehaviors[cardId] = BehaviorFactory.CreateMovementBehavior(movementType);
                Debug.Log($"[CardBehavior] 为卡牌 {cardId}({cardName}) 设置移动行为: {movementType}");
            }
            
            // 设置攻击行为
            if (!_attackBehaviors.ContainsKey(cardId))
            {
                _attackBehaviors[cardId] = BehaviorFactory.CreateAttackBehavior(attackType);
                Debug.Log($"[CardBehavior] 为卡牌 {cardId}({cardName}) 设置攻击行为: {attackType}");
            }
        }
        
        // 修改卡牌的可移动位置
        public void ModifyMovablePositions(Card card, int boardWidth, int boardHeight, Dictionary<Vector2Int, Card> allCards, ref List<Vector2Int> positions)
        {
            int cardId = card.Data.Id;
            string cardName = card.Data.Name;
            
            Debug.Log($"[CardBehavior] 修改卡牌 {cardId}({cardName}) 的可移动位置，原始位置数量: {positions.Count}");
            
            if (_movementBehaviors.TryGetValue(cardId, out IMovementBehavior behavior))
            {
                List<Vector2Int> originalPositions = new List<Vector2Int>(positions);
                positions = behavior.GetMovablePositions(card, boardWidth, boardHeight, allCards);
                
                Debug.Log($"[CardBehavior] 使用卡牌 {cardId}({cardName}) 的自定义移动行为 {behavior.GetType().Name}，修改后位置数量: {positions.Count}");
                
                // 输出位置变化详情
                string positionsStr = string.Join(", ", positions.Select(p => $"({p.x},{p.y})"));
                Debug.Log($"[CardBehavior] 卡牌 {cardId}({cardName}) 的可移动位置: {positionsStr}");
            }
            else
            {
                Debug.Log($"[CardBehavior] 卡牌 {cardId}({cardName}) 没有自定义移动行为，使用默认行为");
            }
        }
        
        // 修改卡牌的可攻击位置
        public void ModifyAttackablePositions(Card card, int boardWidth, int boardHeight, Dictionary<Vector2Int, Card> allCards, ref List<Vector2Int> positions)
        {
            int cardId = card.Data.Id;
            string cardName = card.Data.Name;
            
            Debug.Log($"[CardBehavior] 修改卡牌 {cardId}({cardName}) 的可攻击位置，原始位置数量: {positions.Count}");
            
            if (_attackBehaviors.TryGetValue(cardId, out IAttackBehavior behavior))
            {
                List<Vector2Int> originalPositions = new List<Vector2Int>(positions);
                positions = behavior.GetAttackablePositions(card, boardWidth, boardHeight, allCards);
                
                Debug.Log($"[CardBehavior] 使用卡牌 {cardId}({cardName}) 的自定义攻击行为 {behavior.GetType().Name}，修改后位置数量: {positions.Count}");
                
                // 输出位置变化详情
                string positionsStr = string.Join(", ", positions.Select(p => $"({p.x},{p.y})"));
                Debug.Log($"[CardBehavior] 卡牌 {cardId}({cardName}) 的可攻击位置: {positionsStr}");
            }
            else
            {
                Debug.Log($"[CardBehavior] 卡牌 {cardId}({cardName}) 没有自定义攻击行为，使用默认行为");
            }
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