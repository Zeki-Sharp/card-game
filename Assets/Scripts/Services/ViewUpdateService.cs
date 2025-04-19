using UnityEngine;
using ChessGame;
using System.Collections.Generic;

/// <summary>
/// 视图更新类型枚举，用于标识不同类型的视图更新事件
/// </summary>
public enum ViewUpdateType
{
    // 卡牌相关更新
    CardMoved,
    CardAttacked,
    CardDamaged,
    CardFlipped,
    CardAdded,
    CardRemoved,
    CardStatChanged,
    
    // 游戏状态相关更新
    TurnChanged,
    GameStateChanged
}

public class ViewUpdateService : MonoBehaviour
{
    private CardManager cardManager;
    
    // 延迟更新队列，用于存储需要延迟处理的更新操作
    private Dictionary<Vector2Int, List<ViewUpdateType>> delayedUpdates = new Dictionary<Vector2Int, List<ViewUpdateType>>();
    
    // 记录是否有动画正在播放
    private Dictionary<ViewUpdateType, bool> animationPlaying = new Dictionary<ViewUpdateType, bool>();

    private void Awake()
    {
        // 获取卡牌管理器引用
        cardManager = FindObjectOfType<CardManager>();
        if (cardManager == null)
        {
            Debug.LogError("无法找到CardManager实例");
        }
        
        // 初始化动画状态字典
        foreach (ViewUpdateType type in System.Enum.GetValues(typeof(ViewUpdateType)))
        {
            animationPlaying[type] = false;
        }
    }

    private void Start()
    {
        // 订阅GameEventSystem的事件
        if (GameEventSystem.Instance != null)
        {
            // 卡牌事件
            GameEventSystem.Instance.OnCardMoved += (from, to) => TriggerViewUpdate(ViewUpdateType.CardMoved);
            GameEventSystem.Instance.OnCardAttacked += (attacker, target) => TriggerViewUpdate(ViewUpdateType.CardAttacked);
            GameEventSystem.Instance.OnCardDamaged += (position) => TriggerViewUpdate(ViewUpdateType.CardDamaged);
            GameEventSystem.Instance.OnCardFlipped += (position, isFaceDown) => TriggerViewUpdate(ViewUpdateType.CardFlipped);
            GameEventSystem.Instance.OnCardAdded += (position, ownerId, isFaceDown) => TriggerViewUpdate(ViewUpdateType.CardAdded);
            GameEventSystem.Instance.OnCardRemoved += (position) => TriggerViewUpdate(ViewUpdateType.CardRemoved);
            GameEventSystem.Instance.OnCardStatModified += (position) => TriggerViewUpdate(ViewUpdateType.CardStatChanged);
            
            // 动画完成事件
            GameEventSystem.Instance.OnAttackAnimFinished += (attackerPosition, attackerId, targetId) => HandleAnimationComplete(ViewUpdateType.CardAttacked);
            GameEventSystem.Instance.OnFlipAnimFinished += (position, isFaceDown) => HandleAnimationComplete(ViewUpdateType.CardFlipped);
            GameEventSystem.Instance.OnDamageAnimFinished += (position, damage) => HandleAnimationComplete(ViewUpdateType.CardDamaged, position);
            GameEventSystem.Instance.OnDeathAnimFinished += (position, damage) => HandleAnimationComplete(ViewUpdateType.CardRemoved);
            
            // 游戏状态事件
            GameEventSystem.Instance.OnTurnStarted += (playerId) => TriggerViewUpdate(ViewUpdateType.TurnChanged);
            GameEventSystem.Instance.OnTurnEnded += (playerId) => TriggerViewUpdate(ViewUpdateType.TurnChanged);
            GameEventSystem.Instance.OnGameStarted += () => TriggerViewUpdate(ViewUpdateType.GameStateChanged);
            GameEventSystem.Instance.OnGameOver += () => TriggerViewUpdate(ViewUpdateType.GameStateChanged);
        }
        else
        {
            Debug.LogError("找不到GameEventSystem实例");
        }

        // 订阅动画完成事件
        if (CardAnimationService.Instance != null)
        {
            // 这些事件已经转移到GameEventSystem，不再需要直接订阅
            /*
            CardAnimationService.Instance.OnAttackAnimationComplete += () => HandleAnimationComplete(ViewUpdateType.CardAttacked);
            CardAnimationService.Instance.OnFlipAnimationComplete += () => HandleAnimationComplete(ViewUpdateType.CardFlipped);
            CardAnimationService.Instance.OnDamageAnimationComplete += () => HandleAnimationComplete(ViewUpdateType.CardDamaged);
            */
        }
        else
        {
            Debug.LogWarning("找不到CardAnimationService实例，视图更新将不会等待动画完成");
        }
    }

    /// <summary>
    /// 触发视图更新，负责处理不同类型的视图更新事件
    /// </summary>
    /// <param name="updateType">更新类型</param>
    /// <param name="position">可选的位置参数，指定需要更新的卡牌位置</param>
    public void TriggerViewUpdate(ViewUpdateType updateType, Vector2Int? position = null)
    {
        Debug.Log($"触发视图更新: {updateType}" + (position.HasValue ? $", 位置: {position.Value}" : ""));
        
        // 如果该类型的动画正在播放，延迟处理
        if (animationPlaying.ContainsKey(updateType) && animationPlaying[updateType])
        {
            if (position.HasValue)
            {
                // 将更新添加到延迟队列
                AddToDelayedUpdates(position.Value, updateType);
            }
            return;
        }
        
        // 根据更新类型执行不同的更新逻辑
        switch (updateType)
        {
            case ViewUpdateType.CardMoved:
                // 更新所有卡牌的视图位置
                UpdateAllCardViews();
                break;
                
            case ViewUpdateType.CardAttacked:
            case ViewUpdateType.CardDamaged:
            case ViewUpdateType.CardFlipped:
            case ViewUpdateType.CardStatChanged:
                // 如果提供了位置，更新特定卡牌的视图
                if (position.HasValue)
                {
                    UpdateCardView(position.Value);
                }
                break;
                
            case ViewUpdateType.CardAdded:
                // 添加新卡牌视图
                if (position.HasValue)
                {
                    UpdateCardView(position.Value);
                }
                break;
                
            case ViewUpdateType.CardRemoved:
                // 移除卡牌视图已由动画处理
                break;
                
            case ViewUpdateType.TurnChanged:
                // 更新回合相关UI
                UpdateTurnIndicator();
                break;
                
            case ViewUpdateType.GameStateChanged:
                // 更新游戏状态相关UI
                UpdateGameStateUI();
                break;
        }
        
        // 标记该类型的动画不再播放
        if (animationPlaying.ContainsKey(updateType))
        {
            animationPlaying[updateType] = false;
        }
    }
    
    /// <summary>
    /// 更新特定位置卡牌的视图
    /// </summary>
    /// <param name="position">卡牌位置</param>
    private void UpdateCardView(Vector2Int position)
    {
        Debug.Log($"更新卡牌视图: {position}");
        
        // 从卡牌管理器获取卡牌数据和视图
        CardView cardView = cardManager?.GetCardView(position);
        
        // 如果卡牌存在，更新其视图
        if (cardView != null)
        {
            cardView.UpdateVisuals();
        }
    }
    
    /// <summary>
    /// 处理延迟的更新队列
    /// </summary>
    /// <param name="position">卡牌位置</param>
    private void ProcessDelayedUpdates(Vector2Int position)
    {
        // 检查该位置是否有延迟的更新
        if (delayedUpdates.ContainsKey(position) && delayedUpdates[position].Count > 0)
        {
            Debug.Log($"处理延迟更新: 位置 {position}, 更新数量 {delayedUpdates[position].Count}");
            
            // 处理该位置所有延迟的更新
            foreach (ViewUpdateType updateType in delayedUpdates[position])
            {
                // 重新触发视图更新
                TriggerViewUpdate(updateType, position);
            }
            
            // 清空该位置的延迟更新队列
            delayedUpdates[position].Clear();
        }
    }
    
    /// <summary>
    /// 添加到延迟更新队列
    /// </summary>
    private void AddToDelayedUpdates(Vector2Int position, ViewUpdateType updateType)
    {
        // 如果该位置还没有延迟更新队列，创建一个
        if (!delayedUpdates.ContainsKey(position))
        {
            delayedUpdates[position] = new List<ViewUpdateType>();
        }
        
        // 添加到延迟更新队列
        if (!delayedUpdates[position].Contains(updateType))
        {
            delayedUpdates[position].Add(updateType);
            Debug.Log($"添加到延迟更新队列: 位置 {position}, 类型 {updateType}");
        }
    }
    
    /// <summary>
    /// 更新所有卡牌的视图
    /// </summary>
    private void UpdateAllCardViews()
    {
        Debug.Log("更新所有卡牌视图");
        
        // 具体实现取决于卡牌管理器如何存储和访问所有卡牌
        // 这里仅为示例
        foreach (var cardView in FindObjectsOfType<CardView>())
        {
            cardView.UpdateVisuals();
        }
    }
    
    /// <summary>
    /// 更新回合指示器
    /// </summary>
    private void UpdateTurnIndicator()
    {
        Debug.Log("更新回合指示器");
        // 实现回合指示器的更新逻辑
        // 例如，可以查找和更新回合UI元素
    }
    
    /// <summary>
    /// 更新游戏状态UI
    /// </summary>
    private void UpdateGameStateUI()
    {
        Debug.Log("更新游戏状态UI");
        // 实现游戏状态UI的更新逻辑
        // 例如，显示游戏结束界面、更新分数等
    }

    /// <summary>
    /// 处理动画完成事件，根据positionId转换回Vector2Int位置
    /// </summary>
    /// <param name="updateType">更新类型</param>
    /// <param name="positionId">位置ID</param>
    public void HandleAnimationComplete(ViewUpdateType updateType, Vector2Int position = default)
    {
        Debug.Log($"动画完成，触发相关视图更新: {updateType}");
        
        // 如果提供了有效的positionId，转换为Vector2Int位置
        if (position != default && cardManager != null)
        {
            // 立即更新特定位置的视图
            UpdateCardView(position);
            
            // 处理所有延迟的更新
            ProcessDelayedUpdates(position);
        }
        
        // 触发相关的视图更新
        TriggerViewUpdate(updateType);
    }
} 