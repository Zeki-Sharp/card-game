using UnityEngine;
using System.Collections.Generic;

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
        
        // 默认移动范围和攻击范围
        public virtual int MoveRange { get; protected set; } = 1;
        public virtual int AttackRange { get; protected set; } = 1;

        public Card(CardData data, Vector2Int position, int ownerId = 0, bool isFaceDown = true)
        {
            Data = data.Clone(); // 创建数据副本，避免共享引用
            Position = position;
            OwnerId = ownerId;
            HasActed = false;
            IsFaceDown = isFaceDown;
            
            // 可以根据CardData中的属性设置不同的移动和攻击范围
            // 例如：MoveRange = data.MoveRange;
            // 例如：AttackRange = data.AttackRange;
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
        


        // 判断是否可以移动到指定位置
        public virtual bool CanMoveTo(Vector2Int targetPosition, Dictionary<Vector2Int, Card> allCards)
        {
            // 如果卡牌已经行动过或是背面状态，不能移动
            if (HasActed || IsFaceDown)
                return false;
            
            // 检查目标位置是否已有卡牌
            if (allCards.ContainsKey(targetPosition))
                return false;
            
            // 计算曼哈顿距离
            int distance = Mathf.Abs(targetPosition.x - Position.x) + Mathf.Abs(targetPosition.y - Position.y);
            
            // 检查是否在移动范围内
            return distance <= MoveRange && distance > 0; // 不能移动到自己的位置
        }
        
        // 判断是否可以攻击指定位置
        public virtual bool CanAttack(Vector2Int targetPosition, Dictionary<Vector2Int, Card> allCards)
        {
            // 如果卡牌已经行动过或是背面状态，不能攻击
            if (HasActed || IsFaceDown)
                return false;
            
            // 检查目标位置是否有卡牌
            if (!allCards.ContainsKey(targetPosition))
                return false;
            
            // 获取目标卡牌
            Card targetCard = allCards[targetPosition];
            
            // 不能攻击己方正面卡牌
            if (targetCard.OwnerId == OwnerId && !targetCard.IsFaceDown)
                return false;
            
            // 计算曼哈顿距离
            int distance = Mathf.Abs(targetPosition.x - Position.x) + Mathf.Abs(targetPosition.y - Position.y);
            
            // 检查是否在攻击范围内
            return distance <= AttackRange && distance > 0; // 不能攻击自己
        }
        
        // 获取可移动的位置列表
        public virtual List<Vector2Int> GetMovablePositions(int boardWidth, int boardHeight, Dictionary<Vector2Int, Card> allCards)
        {
            List<Vector2Int> movablePositions = new List<Vector2Int>();
            
            // 如果卡牌已经行动过或是背面状态，不能移动
            if (HasActed || IsFaceDown)
                return movablePositions;
            
            // 检查周围的格子
            for (int dx = -MoveRange; dx <= MoveRange; dx++)
            {
                for (int dy = -MoveRange; dy <= MoveRange; dy++)
                {
                    // 跳过原位置
                    if (dx == 0 && dy == 0) continue;
                    
                    // 计算曼哈顿距离
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) <= MoveRange)
                    {
                        int x = Position.x + dx;
                        int y = Position.y + dy;
                        
                        // 检查是否在棋盘范围内
                        if (x >= 0 && x < boardWidth && y >= 0 && y < boardHeight)
                        {
                            Vector2Int targetPos = new Vector2Int(x, y);
                            if (!allCards.ContainsKey(targetPos))
                            {
                                movablePositions.Add(targetPos);
                            }
                        }
                    }
                }
            }
            
            return movablePositions;
        }
        
        // 获取可攻击的位置列表
        public virtual List<Vector2Int> GetAttackablePositions(int boardWidth, int boardHeight, Dictionary<Vector2Int, Card> allCards)
        {
            List<Vector2Int> attackablePositions = new List<Vector2Int>();
            
            // 如果卡牌已经行动过或是背面状态，不能攻击
            if (HasActed || IsFaceDown)
                return attackablePositions;
            
            // 检查周围的格子
            for (int dx = -AttackRange; dx <= AttackRange; dx++)
            {
                for (int dy = -AttackRange; dy <= AttackRange; dy++)
                {
                    // 跳过原位置
                    if (dx == 0 && dy == 0) continue;
                    
                    // 计算曼哈顿距离
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) <= AttackRange)
                    {
                        int x = Position.x + dx;
                        int y = Position.y + dy;
                        
                        // 检查是否在棋盘范围内
                        if (x >= 0 && x < boardWidth && y >= 0 && y < boardHeight)
                        {
                            Vector2Int targetPos = new Vector2Int(x, y);
                            Card targetCard = allCards.ContainsKey(targetPos) ? allCards[targetPos] : null;
                            
                            // 可以攻击敌方卡牌或任何背面卡牌
                            if (targetCard != null && (targetCard.OwnerId != OwnerId || targetCard.IsFaceDown))
                            {
                                attackablePositions.Add(targetPos);
                            }
                        }
                    }
                }
            }
            
            return attackablePositions;
        }
    }
} 