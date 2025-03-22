using System.Collections.Generic;
using UnityEngine;

namespace ChessGame
{
    public class CardDataProvider : MonoBehaviour
    {
        [SerializeField] private CardDataCollection cardDataCollection;
        
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
            
            if (cardDataCollection == null)
            {
                Debug.LogError("CardDataCollection未设置");
                return;
            }
            
            _availableCards = cardDataCollection.GetAllCards();
            
            Debug.Log($"成功加载了 {_availableCards.Count} 张卡牌");
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
    }
} 