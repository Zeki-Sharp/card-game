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
        
        // 移动类型枚举
        public enum MoveType
        {
            Manhattan, // 曼哈顿距离（上下左右）
            Diagonal   // 斜角移动（对角线）
        }
        
        // 添加范围类型枚举
        public enum RangeType
        {
            Manhattan, // 曼哈顿距离（上下左右）
            Diagonal   // 斜角移动（对角线）
        }
        
        // 默认移动范围和攻击范围
        public virtual int MoveRange { get; protected set; } = 1;
        public virtual int AttackRange { get; protected set; } = 1;
        
        // 默认移动和攻击类型
        public virtual MoveType MovementType { get; protected set; } = MoveType.Manhattan;
        public virtual MoveType AttackType { get; protected set; } = MoveType.Manhattan;

        public Card(CardData data, Vector2Int position, int ownerId = 0, bool isFaceDown = true)
        {
            Data = data.Clone(); // 创建数据副本，避免共享引用
            Position = position;
            OwnerId = ownerId;
            HasActed = false;
            IsFaceDown = isFaceDown;
            
            // 从卡牌数据中获取移动和攻击范围
            MoveRange = data.MoveRange;
            AttackRange = data.AttackRange;
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
        
        // 修改CanMoveTo方法，移除基础逻辑
        public bool CanMoveTo(Vector2Int position, Dictionary<Vector2Int, Card> allCards)
        {
            // 如果卡牌已经行动过，不能再次行动
            if (HasActed) return false;
            
            // 如果是背面状态，不能移动
            if (IsFaceDown) return false;
            
            // 完全依赖能力系统
            List<AbilityConfiguration> abilities = GetAbilities();
            
            // 如果没有能力，不能移动
            if (abilities.Count == 0)
            {
                Debug.Log($"卡牌 {Data.Name} 没有能力，不能移动");
                return false;
            }
            
            // 使用能力系统检查是否可以移动到目标位置
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
                        Debug.Log($"卡牌 {Data.Name} 使用能力 {ability.abilityName} 可以移动到 {position}");
                        return true;
                    }
                }
            }
            
            Debug.Log($"卡牌 {Data.Name} 不能移动到 {position}");
            return false;
        }
        
        // 修改CanAttack方法，移除基础逻辑
        public bool CanAttack(Vector2Int position, Dictionary<Vector2Int, Card> allCards)
        {
            // 如果卡牌已经行动过，不能再次行动
            if (HasActed)
            {
                Debug.Log($"卡牌 {Data.Name} 已经行动过，不能攻击");
                return false;
            }
            
            // 如果是背面状态，不能攻击
            if (IsFaceDown)
            {
                Debug.Log($"卡牌 {Data.Name} 是背面状态，不能攻击");
                return false;
            }
            
            // 完全依赖能力系统
            List<AbilityConfiguration> abilities = GetAbilities();
            Debug.Log($"卡牌 {Data.Name} 检查是否可以攻击 {position}，获取到 {abilities.Count} 个能力");
            
            // 如果没有能力，不能攻击
            if (abilities.Count == 0)
            {
                Debug.Log($"卡牌 {Data.Name} 没有能力，不能攻击");
                return false;
            }
            
            // 使用能力系统检查是否可以攻击目标位置
            foreach (var ability in abilities)
            {
                // 检查是否是攻击类型的能力
                if (ability.actionSequence.Count > 0 && 
                    ability.actionSequence[0].actionType == AbilityActionConfig.ActionType.Attack)
                {
                    Debug.Log($"检查攻击能力: {ability.abilityName}, 条件: {ability.triggerCondition}");
                    
                    // 使用能力系统检查是否可以攻击目标位置
                    CardManager cardManager = GameObject.FindObjectOfType<CardManager>();
                    if (cardManager != null && CanTriggerAbility(ability, position, cardManager))
                    {
                        Debug.Log($"能力 {ability.abilityName} 可以攻击位置 {position}");
                        return true;
                    }
                }
            }
            
            Debug.Log($"卡牌 {Data.Name} 没有可用的攻击能力可以攻击位置 {position}");
            return false;
        }
        
        // 修改GetMovablePositions方法，移除基础逻辑
        public virtual List<Vector2Int> GetMovablePositions(int boardWidth, int boardHeight, Dictionary<Vector2Int, Card> allCards)
        {
            List<Vector2Int> movablePositions = new List<Vector2Int>();
            
            // 如果卡牌已经行动过或是背面状态，不能移动
            if (HasActed || IsFaceDown)
                return movablePositions;
            
            // 获取卡牌的所有能力
            List<AbilityConfiguration> abilities = GetAbilities();
            
            // 如果没有能力，返回空列表
            if (abilities.Count == 0)
            {
                Debug.Log($"[Card] 卡牌 {Data.Id}({Data.Name}) 没有能力，不能移动");
                return movablePositions;
            }
            
            // 使用能力系统计算可移动位置
            CardManager cardManager = GameObject.FindObjectOfType<CardManager>();
            if (cardManager == null)
            {
                Debug.LogError("找不到CardManager实例");
                return movablePositions;
            }
            
            // 遍历棋盘上的所有位置
            for (int x = 0; x < boardWidth; x++)
            {
                for (int y = 0; y < boardHeight; y++)
                {
                    Vector2Int targetPos = new Vector2Int(x, y);
                    
                    // 跳过当前位置
                    if (targetPos == Position) continue;
                    
                    // 检查是否可以移动到该位置
                    if (CanMoveTo(targetPos, allCards))
                    {
                        movablePositions.Add(targetPos);
                    }
                }

            }
            
            Debug.Log($"[Card] 卡牌 {Data.Id}({Data.Name}) 使用能力系统计算可移动位置，位置数量: {movablePositions.Count}");
            return movablePositions;
        }
        
        // 修改GetAttackablePositions方法，移除基础逻辑
        public virtual List<Vector2Int> GetAttackablePositions(int boardWidth, int boardHeight, Dictionary<Vector2Int, Card> allCards)
        {
            List<Vector2Int> attackablePositions = new List<Vector2Int>();
            
            // 如果卡牌已经行动过或是背面状态，不能攻击
            if (HasActed || IsFaceDown)
                return attackablePositions;
            
            // 获取卡牌的所有能力
            List<AbilityConfiguration> abilities = GetAbilities();
            
            // 如果没有能力，返回空列表
            if (abilities.Count == 0)
            {
                Debug.Log($"[Card] 卡牌 {Data.Id}({Data.Name}) 没有能力，不能攻击");
                return attackablePositions;
            }
            
            // 使用能力系统计算可攻击位置
            CardManager cardManager = GameObject.FindObjectOfType<CardManager>();
            if (cardManager == null)
            {
                Debug.LogError("找不到CardManager实例");
                return attackablePositions;
            }
            
            // 遍历棋盘上的所有位置
            for (int x = 0; x < boardWidth; x++)
            {
                for (int y = 0; y < boardHeight; y++)
                {
                    Vector2Int targetPos = new Vector2Int(x, y);
                    
                    // 跳过当前位置
                    if (targetPos == Position) continue;
                    
                    // 检查是否可以攻击该位置
                    if (CanAttack(targetPos, allCards))
                    {
                        attackablePositions.Add(targetPos);
                    }
                }
            }
            
            Debug.Log($"[Card] 卡牌 {Data.Id}({Data.Name}) 使用能力系统计算可攻击位置，位置数量: {attackablePositions.Count}");
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

        // 检查是否在曼哈顿距离范围内
        public bool IsInManhattanRange(Vector2Int targetPosition, int range)
        {
            int distance = Mathf.Abs(targetPosition.x - Position.x) + 
                          Mathf.Abs(targetPosition.y - Position.y);
            return distance <= range;
        }
        
        // 检查是否在斜角范围内
        public bool IsInDiagonalRange(Vector2Int targetPosition, int range)
        {
            int dx = Mathf.Abs(targetPosition.x - Position.x);
            int dy = Mathf.Abs(targetPosition.y - Position.y);
            return dx == dy && dx <= range;
        }
        
        // 检查是否在移动范围内（供能力系统使用）
        public bool IsInMoveRange(Vector2Int targetPosition)
        {
            // 默认使用曼哈顿距离
            return IsInManhattanRange(targetPosition, MoveRange);
        }
        
        // 检查是否在攻击范围内（供能力系统使用）
        public bool IsInAttackRange(Vector2Int targetPosition)
        {
            // 默认使用曼哈顿距离
            return IsInManhattanRange(targetPosition, AttackRange);
        }
    }
} 