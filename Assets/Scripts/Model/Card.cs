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
        
        // 卡牌是否为背面状态
        public bool IsFaceDown { get; private set; }

        public Card(CardData data, Vector2Int position, int ownerId = 0, bool isFaceDown = true)
        {
            Data = data.Clone(); // 创建数据副本，避免共享引用
            Position = position;
            OwnerId = ownerId;
            HasActed = false;
            IsFaceDown = isFaceDown;
        }

        // 卡牌攻击目标
        public bool Attack(Card target)
        {
            if (target == null) return false;
            
            // 如果卡牌是背面状态，不能主动攻击
            if (IsFaceDown)
                return false;
                
            // 如果目标是背面状态
            if (target.IsFaceDown)
            {
                // 翻转为正面
                target.FlipToFaceUp();
                
                // 对背面卡牌造成伤害，攻击方不受伤
                target.Data.Health -= this.Data.Attack;
                
                // 如果血量小于等于0，设置为1（背面卡牌不会直接死亡）
                if (target.Data.Health <= 0)
                {
                    target.Data.Health = 1;
                }
            }
            else
            {
                // 双方都是正面卡牌，双方都受伤
                target.Data.Health -= this.Data.Attack;
                this.Data.Health -= target.Data.Attack;
            }
            
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
        
        // 翻转卡牌为正面
        public void FlipToFaceUp()
        {
            if (IsFaceDown)
            {
                IsFaceDown = false;
                Debug.Log($"卡牌翻转为正面，位置: {Position}, 所有者: {OwnerId}");
            }
        }
        
        // 检查卡牌是否可以行动
        public bool CanAct()
        {
            return !HasActed && !IsFaceDown;
        }
    }
} 