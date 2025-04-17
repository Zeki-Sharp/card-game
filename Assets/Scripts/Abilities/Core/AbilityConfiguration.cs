using System.Collections.Generic;
using UnityEngine;
using ChessGame.FSM.TurnState;

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
        
        // 触发条件（如 "Distance==2", "Health<50%" 等）
        public string triggerCondition;
        
        // 动作序列
        public List<AbilityActionConfig> actionSequence = new List<AbilityActionConfig>();
        
        // 冷却时间（回合数）
        // 0 表示无冷却，可以每回合触发
        // 1 表示每隔一个回合触发一次
        // 2 表示每隔两个回合触发一次，以此类推
        [Tooltip("冷却回合数：0=无冷却，1=每隔一回合，2=每隔两回合...")]
        public int cooldown = 0;
        
        // 触发阶段（直接使用回合状态机的枚举类型）
        [Tooltip("能力触发的回合阶段")]
        public TurnPhase triggerPhase = TurnPhase.PlayerMainPhase;
        
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
        
        // 获取冷却计数器ID
        public string GetCooldownCounterId()
        {
            return "cooldown_" + abilityName;
        }
        
        // 判断能力是否为自动触发
        public bool IsAutomatic()
        {
            return triggerPhase == TurnPhase.PlayerTurnStart || 
                   triggerPhase == TurnPhase.PlayerTurnEnd ||
                   triggerPhase == TurnPhase.EnemyTurnStart ||
                   triggerPhase == TurnPhase.EnemyTurnEnd;
        }
        
        // 判断能力是否属于指定玩家
        public bool BelongsToPlayer(int playerId)
        {
            bool isPlayerAbility = triggerPhase == TurnPhase.PlayerTurnStart || 
                                   triggerPhase == TurnPhase.PlayerMainPhase ||
                                   triggerPhase == TurnPhase.PlayerTurnEnd;
                                   
            return (playerId == 0 && isPlayerAbility) || (playerId != 0 && !isPlayerAbility);
        }
    }
} 