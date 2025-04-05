using System.Collections.Generic;
using UnityEngine;

namespace ChessGame.Cards
{
    /// <summary>
    /// 能力配置 - 定义一个完整的能力
    /// </summary>
    [CreateAssetMenu(fileName = "NewAbility", menuName = "ChessGame/Ability", order = 2)]
    public class AbilityConfiguration : ScriptableObject
    {
        // 能力名称
        public string abilityName;
        
        // 能力描述
        public string description;
        
        // 触发条件（如 "Distance==2", "Health<50%", "Cooldown==0" 等）
        public string triggerCondition;
        
        // 动作序列
        public List<AbilityActionConfig> actionSequence = new List<AbilityActionConfig>();
        
        // 冷却时间（回合数）
        public int cooldown = 0;
        
        // 范围类型枚举
        public enum RangeType
        {
            Default,        // 使用触发条件
            AttackRange,    // 使用卡牌的攻击范围
            MoveRange,      // 使用卡牌的移动范围
            Custom,         // 使用自定义范围值
            Unlimited       // 无限范围（全场）
        }
        
        // 范围类型
        public RangeType rangeType = RangeType.Default;
        
        // 自定义范围值（当rangeType为Custom时使用）
        public int customRangeValue = 1;
        
        // 范围条件（额外的条件，如"Enemy"表示只能选择敌方卡牌）
        public string rangeCondition = "";
    }
} 