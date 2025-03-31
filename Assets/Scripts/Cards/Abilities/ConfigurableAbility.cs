using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChessGame.Cards
{
    /// <summary>
    /// 可配置能力 - 基于配置实现的能力
    /// </summary>
    public class ConfigurableAbility : IAbility
    {
        private AbilityConfiguration _config;
        
        public string Name => _config.abilityName;
        public string Description => _config.description;
        
        public ConfigurableAbility(AbilityConfiguration config)
        {
            _config = config;
        }
        
        public bool CanTrigger(Card card, Vector2Int targetPosition, CardManager cardManager)
        {
            return AbilityManager.Instance.CanTriggerAbility(_config, card, targetPosition, cardManager);
        }
        
        public IEnumerator Execute(Card card, Vector2Int targetPosition, CardManager cardManager)
        {
            yield return AbilityManager.Instance.ExecuteAbility(_config, card, targetPosition);
        }
    }
} 