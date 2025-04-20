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

        // 回合计数器字典，键为能力ID，值为计数值
        private Dictionary<string, int> _turnCounters = new Dictionary<string, int>();

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
            // 基本检查
            if (HasActed || IsFaceDown) return false;
            
            // 获取卡牌能力
            List<AbilityConfiguration> abilities = GetAbilities();
            
            // 遍历能力，检查是否有可用的攻击能力
            foreach (var ability in abilities)
            {
                // 检查能力的第一个动作是否为Attack类型
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
        
        // 获取可攻击的位置
        public virtual List<Vector2Int> GetAttackablePositions(int boardWidth, int boardHeight, Dictionary<Vector2Int, Card> allCards)
        {
            List<Vector2Int> attackablePositions = new List<Vector2Int>();
            
            // 基本检查
            if (HasActed || IsFaceDown) return attackablePositions;
            
            // 获取普通攻击范围内的位置
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
            
            return attackablePositions;
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
            Debug.Log($"【能力检查】卡牌 {Data.Name} 检查能力 {ability.abilityName} 是否可触发，目标位置: {targetPosition}");
            bool canTrigger = AbilityManager.Instance.CanTriggerAbility(ability, this, targetPosition, cardManager);
            Debug.Log($"【能力检查】结果: {canTrigger}");
            return canTrigger;
        }

        /// <summary>
        /// 获取所有能力可作用的位置
        /// </summary>
        public List<Vector2Int> GetAbilityTargetPositions(int boardWidth, int boardHeight, Dictionary<Vector2Int, Card> allCards)
        {
            List<Vector2Int> targetPositions = new List<Vector2Int>();
            
            // 获取所有能力
            List<AbilityConfiguration> abilities = GetAbilities();
            
            // 获取卡牌管理器
            CardManager cardManager = GameObject.FindObjectOfType<CardManager>();
            if (cardManager == null) return targetPositions;
            
            // 遍历所有能力
            foreach (var ability in abilities)
            {
                // 获取能力可作用的位置
                List<Vector2Int> abilityPositions = AbilityManager.Instance.GetAbilityRange(ability, this, cardManager);
                
                // 添加到结果中（去重）
                foreach (var pos in abilityPositions)
                {
                    if (!targetPositions.Contains(pos))
                    {
                        targetPositions.Add(pos);
                    }
                }
            }
            
            return targetPositions;
        }

        /// <summary>
        /// 获取卡牌可移动的范围
        /// </summary>
        public List<Vector2Int> GetMoveRange(Board board)
        {
            // 获取所有卡牌
            Dictionary<Vector2Int, Card> allCards = new Dictionary<Vector2Int, Card>();
            
            // 从CardManager获取所有卡牌
            CardManager cardManager = GameObject.FindObjectOfType<CardManager>();
            if (cardManager != null)
            {
                allCards = cardManager.GetAllCards();
            }
            
            // 调用现有方法
            return GetMovablePositions(cardManager.BoardWidth, cardManager.BoardHeight, allCards);
        }

        /// <summary>
        /// 获取卡牌可攻击的范围
        /// </summary>
        public List<Vector2Int> GetAttackRange(Board board)
        {
            // 获取所有卡牌
            Dictionary<Vector2Int, Card> allCards = new Dictionary<Vector2Int, Card>();
            
            // 从CardManager获取所有卡牌
            CardManager cardManager = GameObject.FindObjectOfType<CardManager>();
            if (cardManager != null)
            {
                allCards = cardManager.GetAllCards();
            }
            
            // 调用现有方法
            return GetAttackablePositions(cardManager.BoardWidth, cardManager.BoardHeight, allCards);
        }

        /// <summary>
        /// 获取指定能力的回合计数器值
        /// </summary>
        public int GetTurnCounter(string abilityId)
        {
            if (_turnCounters.TryGetValue(abilityId, out int count))
            {
                return count;
            }
            return 0;
        }
        
        /// <summary>
        /// 设置指定能力的回合计数器值
        /// </summary>
        public void SetTurnCounter(string abilityId, int value)
        {
            _turnCounters[abilityId] = value;
        }
        
        /// <summary>
        /// 增加指定能力的回合计数器值
        /// </summary>
        public void IncrementTurnCounter(string abilityId)
        {
            if (!_turnCounters.ContainsKey(abilityId))
            {
                _turnCounters[abilityId] = 0;
            }
            _turnCounters[abilityId]++;
        }
        
        /// <summary>
        /// 重置指定能力的回合计数器值
        /// </summary>
        public void ResetTurnCounter(string abilityId)
        {
            _turnCounters[abilityId] = 0;
        }
    }

} 