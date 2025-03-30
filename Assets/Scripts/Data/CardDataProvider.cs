using System.Collections.Generic;
using UnityEngine;

namespace ChessGame
{
    public class CardDataProvider : MonoBehaviour
    {
        [SerializeField] private string csvFilePath = "CardData"; // CSV文件路径（相对于Resources文件夹）
        
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
                // 从CSV文件加载数据
                _availableCards = CSVReader.ReadCardDataFromCSV(csvFilePath);
                Debug.Log($"从CSV文件加载了 {_availableCards.Count} 张卡牌");
            
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