using UnityEngine;

namespace ChessGame
{
    [System.Serializable]
    public class CardData
    {
        [SerializeField] private int id;
        [SerializeField] private string name;
        [SerializeField] private int attack;
        [SerializeField] private int health;
        [SerializeField] private Sprite image;
        [SerializeField] private int faction; // 0为玩家阵营，1为敌方阵营
        
        public int Id => id;
        public string Name => name;
        public int Attack { get => attack; set => attack = value; }
        public int Health { get => health; set => health = value; }
        public Sprite Image => image;
        public int Faction => faction;
        
        // 默认构造函数，用于序列化
        public CardData() { }
        
        // 带参数的构造函数
        public CardData(int id, string name, int attack, int health, Sprite image, int faction = 0)
        {
            this.id = id;
            this.name = name;
            this.attack = attack;
            this.health = health;
            this.image = image;
            this.faction = faction;
        }
        
        // 创建卡牌数据的副本
        public CardData Clone()
        {
            return new CardData(id, name, attack, health, image, faction);
        }
    }
} 