using System.Collections.Generic;
using UnityEngine;

namespace ChessGame.Cards
{
    /// <summary>
    /// 能力动作配置 - 定义能力中的单个动作
    /// </summary>
    [System.Serializable]
    public class AbilityActionConfig
    {
        // 动作类型枚举
        public enum ActionType
        {
            Move,       // 移动
            Attack,     // 攻击
            Heal,       // 治疗
            ApplyEffect, // 应用效果
            Wait        // 等待
        }
        
        // 动作类型
        public ActionType actionType;
        
        // 目标选择器（如 "Self", "Target", "TargetPosition-1,0" 等）
        public string targetSelector;
        
        // 动作参数
        [SerializeField]
        private List<AbilityParameterPair> parameterList = new List<AbilityParameterPair>();
        
        // 参数字典（运行时使用）
        [System.NonSerialized]
        private Dictionary<string, object> _parameters;
        
        // 获取参数字典
        public Dictionary<string, object> GetParameters()
        {
            if (_parameters == null)
            {
                _parameters = new Dictionary<string, object>();
                foreach (var pair in parameterList)
                {
                    _parameters[pair.key] = pair.value;
                }
            }
            return _parameters;
        }
    }
    
    /// <summary>
    /// 能力参数键值对 - 用于在Inspector中显示
    /// </summary>
    [System.Serializable]
    public class AbilityParameterPair
    {
        public string key;
        public string value;
    }
} 