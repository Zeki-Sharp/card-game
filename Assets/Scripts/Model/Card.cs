using UnityEngine;
using System.Collections.Generic;
using ChessGame.Cards;

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
        
        }

        // 卡牌攻击目标
        public bool Attack(Card target)
        {
            if (target == null) return false;
            
            // 计算伤害
            int damage = this.Data.Attack;
            
            // 记录攻击前的生命值
            int targetHealthBefore = target.Data.Health;
            
            // 应用伤害
            target.Data.Health -= damage;
            
            Debug.Log($"卡牌 {this.Data.Name}(攻击力:{this.Data.Attack}) 攻击 {target.Data.Name}，造成 {damage} 点伤害，目标生命值: {targetHealthBefore} -> {target.Data.Health}");
            
            return true;
        }

        // 添加一个新方法，判断是否应该受到反伤
        public bool ShouldReceiveCounterAttack(Card attacker, Dictionary<Vector2Int, Card> allCards)
        {
            // 获取被攻击者的可攻击位置
            List<Vector2Int> attackablePositions = this.GetAttackablePositions(100, 100, allCards);
            
            // 检查攻击者的位置是否在被攻击者的可攻击范围内
            bool canCounter = attackablePositions.Contains(attacker.Position);
            
            Debug.Log($"判断反伤: {this.Data.Name} {(canCounter ? "可以" : "不能")}反击 {attacker.Data.Name}");
            
            return canCounter;
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
        
        // 修改CanMoveTo方法
        public bool CanMoveTo(Vector2Int position, Dictionary<Vector2Int, Card> allCards)
        {
            // 如果卡牌已经行动过，不能再次行动
            if (HasActed) return false;
            
            // 如果是背面状态，不能移动
            if (IsFaceDown) return false;
            
            // 使用能力系统检查是否可以移动到目标位置
            List<AbilityConfiguration> abilities = GetAbilities();
            foreach (var ability in abilities)
            {
                // 检查是否是移动类型的能力
                if (ability.actionSequence.Count > 0 && 
                    ability.actionSequence[0].actionType == AbilityActionConfig.ActionType.Move)
                {
                    // 使用能力系统检查是否可以移动到目标位置
                    CardManager cardManager = GameObject.FindObjectOfType<CardManager>();
                    if (cardManager != null && CanTriggerAbility(ability, position, cardManager))
                    {
                        return true;
                    }
                }
            }
            
            // 如果没有找到可用的移动能力，使用基本移动逻辑
            // 计算曼哈顿距离
            int distance = Mathf.Abs(position.x - Position.x) + Mathf.Abs(position.y - Position.y);
            
            // 检查是否在移动范围内
            if (distance > Data.MoveRange) return false;
            
            // 检查目标位置是否有卡牌
            if (allCards.ContainsKey(position)) return false;
            
            return true;
        }
        
        // 修改CanAttack方法
        public bool CanAttack(Vector2Int position, Dictionary<Vector2Int, Card> allCards)
        {
            // 如果卡牌已经行动过，不能再次行动
            if (HasActed) return false;
            
            // 如果是背面状态，不能攻击
            if (IsFaceDown) return false;
            
            // 使用能力系统检查是否可以攻击目标位置
            List<AbilityConfiguration> abilities = GetAbilities();
            foreach (var ability in abilities)
            {
                // 检查是否是攻击类型的能力
                if (ability.actionSequence.Count > 0 && 
                    ability.actionSequence[0].actionType == AbilityActionConfig.ActionType.Attack)
                {
                    // 使用能力系统检查是否可以攻击目标位置
                    CardManager cardManager = GameObject.FindObjectOfType<CardManager>();
                    if (cardManager != null && CanTriggerAbility(ability, position, cardManager))
                    {
                        return true;
                    }
                }
            }
            
            // 如果没有找到可用的攻击能力，使用基本攻击逻辑
            // 计算曼哈顿距离
            int distance = Mathf.Abs(position.x - Position.x) + Mathf.Abs(position.y - Position.y);
            
            // 检查是否在攻击范围内
            if (distance > Data.AttackRange) return false;
            
            // 检查目标位置是否有卡牌
            if (!allCards.ContainsKey(position)) return false;
            
            // 检查目标卡牌是否是敌方卡牌或背面卡牌
            Card targetCard = allCards[position];
            return targetCard.OwnerId != OwnerId || targetCard.IsFaceDown;
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
            
            Debug.Log($"[Card] 卡牌 {Data.Id}({Data.Name}) 计算可移动位置，位置数量: {movablePositions.Count}");
            
            // 使用能力系统修改可移动位置
            // 这里可以添加能力系统的逻辑，暂时注释掉行为管理器的引用
            // CardBehaviorManager.Instance.ModifyMovablePositions(this, boardWidth, boardHeight, allCards, ref movablePositions);
            
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
            
            Debug.Log($"[Card] 卡牌 {Data.Id}({Data.Name}) 计算可攻击位置，位置数量: {attackablePositions.Count}");
            
            // 使用能力系统修改可攻击位置
            // 这里可以添加能力系统的逻辑，暂时注释掉行为管理器的引用
            // CardBehaviorManager.Instance.ModifyAttackablePositions(this, boardWidth, boardHeight, allCards, ref attackablePositions);
            
            return attackablePositions;
        }

        // 实现反击方法
        public void AntiAttack(Card attacker)
        {
            if (attacker == null) return;
            
            // 计算反伤伤害
            int damage = this.Data.Attack;
            
            // 记录反击前的生命值
            int attackerHealthBefore = attacker.Data.Health;
            
            // 应用伤害
            attacker.Data.Health -= damage;
            
            Debug.Log($"卡牌 {this.Data.Name}(攻击力:{this.Data.Attack}) 反击 {attacker.Data.Name}，造成 {damage} 点伤害，目标生命值: {attackerHealthBefore} -> {attacker.Data.Health}");
        }

        // 添加获取卡牌能力的方法
        public List<AbilityConfiguration> GetAbilities()
        {
            if (AbilityManager.Instance != null)
            {
                return AbilityManager.Instance.GetCardAbilities(this);
            }
            return new List<AbilityConfiguration>();
        }

        // 添加检查能力是否可触发的方法
        public bool CanTriggerAbility(AbilityConfiguration ability, Vector2Int targetPosition, CardManager cardManager)
        {
            if (AbilityManager.Instance != null)
            {
                return AbilityManager.Instance.CanTriggerAbility(ability, this, targetPosition, cardManager);
            }
            return false;
        }
    }
} 