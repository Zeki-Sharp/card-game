using UnityEngine;
using ChessGame.Cards;
using System.Collections.Generic;

namespace ChessGame
{
    [CreateAssetMenu(fileName = "NewCardData", menuName = "ChessGame/Card Data", order = 1)]
    public class CardDataSO : ScriptableObject
    {
        [Header("基本信息")]
        public int id;
        public string cardName;
        public int attack = 2;
        public int health = 10;
        public Sprite cardImage;
        public int faction = 0; // 0为玩家阵营，1为敌方阵营
        public int moveRange = 1;
        public int attackRange = 1;
        
        
        [Header("能力")]
        public List<AbilityConfiguration> abilities = new List<AbilityConfiguration>();
        
        // 转换为现有的 CardData 结构
        public CardData ToCardData()
        {
            return new CardData(id, cardName, attack, health, cardImage, faction, moveRange, attackRange);
        }
    }
} 