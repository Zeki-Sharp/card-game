using UnityEngine;

namespace ChessGame
{
    public class Card
    {
        // 卡牌数据
        public CardData Data { get; private set; }
        
        // 卡牌在棋盘上的位置
        public Vector2Int Position { get; set; }
        
        // 卡牌所属玩家ID (0为玩家，1为敌方)
        public int OwnerId { get; set; }
        
        // 卡牌是否已经行动过
        public bool HasActed { get; set; }

        public Card(CardData data, Vector2Int position, int ownerId = 0)
        {
            Data = data.Clone(); // 创建数据副本，避免共享引用
            Position = position;
            OwnerId = ownerId;
            HasActed = false;
        }

        // 卡牌攻击目标
        public bool Attack(Card target)
        {
            if (target == null) return false;
            
            // 目标受到伤害
            target.Data.Health -= this.Data.Attack;
            
            // 标记为已行动
            HasActed = true;
            
            return true;
        }

        // 检查卡牌是否存活
        public bool IsAlive()
        {
            return Data.Health > 0;
        }

        // 重置卡牌行动状态
        public void ResetAction()
        {
            HasActed = false;
        }
    }
} 