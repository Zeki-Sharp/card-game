using System.Collections.Generic;
using UnityEngine;

namespace ChessGame
{
    [CreateAssetMenu(fileName = "CardDataCollection", menuName = "ChessGame/Card Data Collection")]
    public class CardDataCollection : ScriptableObject
    {
        [SerializeField] private List<CardData> cards = new List<CardData>();
        
        public List<CardData> GetAllCards()
        {
            // 返回卡牌数据的副本，避免外部修改
            List<CardData> result = new List<CardData>();
            foreach (var card in cards)
            {
                result.Add(card.Clone());
            }
            return result;
        }
    }
} 