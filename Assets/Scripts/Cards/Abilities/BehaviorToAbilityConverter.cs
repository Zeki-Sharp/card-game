using UnityEngine;
using System.Collections.Generic;
using ChessGame.Cards;

namespace ChessGame.Cards
{
    /// <summary>
    /// 行为类型转换器 - 将行为类型转换为对应的能力
    /// </summary>
    public class BehaviorToAbilityConverter : MonoBehaviour
    {
        [SerializeField] private AbilityConfiguration defaultMovementAbility;
        [SerializeField] private AbilityConfiguration assassinMovementAbility;
        [SerializeField] private AbilityConfiguration defaultAttackAbility;
        [SerializeField] private AbilityConfiguration archerAttackAbility;
        [SerializeField] private AbilityConfiguration assassinAttackAbility;
        
        private void Start()
        {
            try
            {
                // 确保AbilityManager已经初始化
                if (AbilityManager.Instance == null)
                {
                    Debug.LogError("AbilityManager实例不存在");
                    return;
                }
                
                // 加载所有卡牌数据
                CardDataSO[] cardDataSOs = Resources.LoadAll<CardDataSO>("CardData");
                foreach (var cardDataSO in cardDataSOs)
                {
                    // 为每张卡牌添加基本能力
                    AddBasicAbilities(cardDataSO);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"BehaviorToAbilityConverter初始化失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 为卡牌添加基本能力
        /// </summary>
        private void AddBasicAbilities(CardDataSO cardDataSO)
        {
            // 添加基本移动能力
            if (defaultMovementAbility != null)
            {
                AbilityManager.Instance.RegisterAbility(cardDataSO.id, defaultMovementAbility);
                Debug.Log($"为卡牌 {cardDataSO.cardName}(ID:{cardDataSO.id}) 添加基本移动能力: {defaultMovementAbility.abilityName}");
            }
            
            // 添加基本攻击能力
            if (defaultAttackAbility != null)
            {
                AbilityManager.Instance.RegisterAbility(cardDataSO.id, defaultAttackAbility);
                Debug.Log($"为卡牌 {cardDataSO.cardName}(ID:{cardDataSO.id}) 添加基本攻击能力: {defaultAttackAbility.abilityName}");
            }
            
            // 根据卡牌ID添加特殊能力
            switch (cardDataSO.id)
            {
                case 1: // 假设ID为1的卡牌是刺客
                    if (assassinMovementAbility != null)
                    {
                        AbilityManager.Instance.RegisterAbility(cardDataSO.id, assassinMovementAbility);
                        Debug.Log($"为卡牌 {cardDataSO.cardName}(ID:{cardDataSO.id}) 添加刺客移动能力: {assassinMovementAbility.abilityName}");
                    }
                    if (assassinAttackAbility != null)
                    {
                        AbilityManager.Instance.RegisterAbility(cardDataSO.id, assassinAttackAbility);
                        Debug.Log($"为卡牌 {cardDataSO.cardName}(ID:{cardDataSO.id}) 添加刺客攻击能力: {assassinAttackAbility.abilityName}");
                    }
                    break;
                case 2: // 假设ID为2的卡牌是弓箭手
                    if (archerAttackAbility != null)
                    {
                        AbilityManager.Instance.RegisterAbility(cardDataSO.id, archerAttackAbility);
                        Debug.Log($"为卡牌 {cardDataSO.cardName}(ID:{cardDataSO.id}) 添加弓箭手攻击能力: {archerAttackAbility.abilityName}");
                    }
                    break;
                default:
                    // 其他卡牌使用基本能力
                    break;
            }
        }
    }
} 