using System.Collections.Generic;
using UnityEngine;
using ChessGame.Cards;

namespace ChessGame
{
    public class CardDataProvider : MonoBehaviour
    {
        [SerializeField] private string resourcesPath = "CardData"; // ScriptableObject资源路径
        
        private List<CardData> _availableCards = new List<CardData>();
        private Dictionary<int, MovementType> _cardMovementTypes = new Dictionary<int, MovementType>();
        private Dictionary<int, AttackType> _cardAttackTypes = new Dictionary<int, AttackType>();
        private bool _isInitialized = false;
        
        private void Awake()
        {
            LoadCardData();
        }
        
        // 加载卡牌数据
        public void LoadCardData()
        {
            _availableCards.Clear();
            _cardMovementTypes.Clear();
            _cardAttackTypes.Clear();
            
            // 从ScriptableObject加载数据
            CardDataSO[] cardDataSOs = Resources.LoadAll<CardDataSO>(resourcesPath);
            
            foreach (CardDataSO cardDataSO in cardDataSOs)
            {
                // 添加基本卡牌数据
                _availableCards.Add(cardDataSO.ToCardData());
                
                // 存储行为类型信息
                _cardMovementTypes[cardDataSO.id] = cardDataSO.movementType;
                _cardAttackTypes[cardDataSO.id] = cardDataSO.attackType;
            }
            
            Debug.Log($"从ScriptableObject加载了 {_availableCards.Count} 张卡牌");
            
            _isInitialized = true;
        }
        
        // 获取所有可用卡牌数据
        public List<CardData> GetAllCardData()
        {
            if (!_isInitialized)
            {
                LoadCardData();
            }
            
            return new List<CardData>(_availableCards);
        }
        
        // 根据ID获取卡牌数据
        public CardData GetCardDataById(int id)
        {
            if (!_isInitialized)
            {
                LoadCardData();
            }
            
            CardData cardData = _availableCards.Find(card => card.Id == id);
            if (cardData == null)
            {
                Debug.LogError($"找不到ID为 {id} 的卡牌数据");
                return null;
            }
            
            // 返回卡牌数据的副本，避免修改原始数据
            return cardData.Clone();
        }
        
        // 获取卡牌的移动类型
        public MovementType GetCardMovementType(int id)
        {
            if (_cardMovementTypes.TryGetValue(id, out MovementType type))
            {
                return type;
            }
            return MovementType.Default;
        }
        
        // 获取卡牌的攻击类型
        public AttackType GetCardAttackType(int id)
        {
            if (_cardAttackTypes.TryGetValue(id, out AttackType type))
            {
                return type;
            }
            return AttackType.Default;
        }
    }
} 