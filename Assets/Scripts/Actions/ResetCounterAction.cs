// 创建 ResetCounterAction.cs 文件
using UnityEngine;

namespace ChessGame
{
    /// <summary>
    /// 重置卡牌计数器行动
    /// </summary>
    public class ResetCounterAction : CardAction
    {
        private Vector2Int _targetPosition;
        private string _abilityId;
        
        public ResetCounterAction(CardManager cardManager, Vector2Int targetPosition, string abilityId) 
            : base(cardManager)
        {
            _targetPosition = targetPosition;
            _abilityId = abilityId;
        }
        
        public override bool CanExecute()
        {
            // 获取目标卡牌
            Card targetCard = CardManager.GetCard(_targetPosition);
            if (targetCard == null)
            {
                Debug.LogWarning($"重置计数器失败：目标位置 {_targetPosition} 没有卡牌");
                return false;
            }
            
            return true;
        }
        
        public override bool Execute()
        {
            if (!CanExecute())
                return false;
                
            // 获取目标卡牌
            Card targetCard = CardManager.GetCard(_targetPosition);
            
            Debug.Log($"重置卡牌计数器: {targetCard.Data.Name}, 能力ID: {_abilityId}, 当前值: {targetCard.GetTurnCounter(_abilityId)}");
            
            // 重置计数器
            targetCard.ResetTurnCounter(_abilityId);
            
            // 确认计数器已重置
            int newCount = targetCard.GetTurnCounter(_abilityId);
            Debug.Log($"卡牌 {targetCard.Data.Name} 的能力 {_abilityId} 计数器已重置为 {newCount}");
            
            return true;
        }
    }
}