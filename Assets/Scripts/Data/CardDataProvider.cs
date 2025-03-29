using System.Collections.Generic;
using UnityEngine;

namespace ChessGame
{
    public class CardDataProvider : MonoBehaviour
    {
        [SerializeField] private CardDataCollection cardDataCollection;
        [SerializeField] private string csvFilePath = "CardData"; // CSV文件路径（相对于Resources文件夹）
        [SerializeField] private bool useCSV = false; // 是否使用CSV文件加载数据
        
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
            
            if (useCSV)
            {
                // 从CSV文件加载数据
                _availableCards = CSVReader.ReadCardDataFromCSV(csvFilePath);
                Debug.Log($"从CSV文件加载了 {_availableCards.Count} 张卡牌");
            }
            else if (cardDataCollection != null)
            {
                // 从ScriptableObject加载数据
                _availableCards = cardDataCollection.GetAllCards();
                Debug.Log($"从ScriptableObject加载了 {_availableCards.Count} 张卡牌");
            }
            else
            {
                Debug.LogError("CardDataCollection未设置，且未启用CSV加载");
                return;
            }
            
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