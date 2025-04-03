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
        
        // 能力图标
        public Sprite icon;
    }
} 