using System.Collections.Generic;
using UnityEngine;

namespace ChessGame
{
    public class CardDataProvider : MonoBehaviour
    {
        [SerializeField] private string resourcesPath = "CardData"; // ScriptableObject资源路径
        
        private List<CardData> _availableCards = new List<CardData>();
        private bool _isInitialized = false;
        
        private void Awake()
        {
            LoadCardData();
        }
        
        // 加载卡牌数据
        public void LoadCardData()
        {
            _availableCards.Clear();
            
            // 从ScriptableObject加载数据
            CardDataSO[] cardDataSOs = Resources.LoadAll<CardDataSO>(resourcesPath);
            
            foreach (CardDataSO cardDataSO in cardDataSOs)
            {
                _availableCards.Add(cardDataSO.ToCardData());
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
    }
} 