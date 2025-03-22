using UnityEngine;

namespace ChessGame
{
    [System.Serializable]
    public class CardData
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public int Attack { get; set; }
        public int Health { get; set; }
        public Sprite Image { get; private set; }

        public CardData(int id, string name, int attack, int health, Sprite image)
        {
            Id = id;
            Name = name;
            Attack = attack;
            Health = health;
            Image = image;
        }

        // 创建卡牌数据的副本
        public CardData Clone()
        {
            return new CardData(Id, Name, Attack, Health, Image);
        }
    }
} 