namespace ChessGame.FSM.TurnState
{
    /// <summary>
    /// 回合阶段枚举 - 定义游戏回合的各个阶段
    /// </summary>
    public enum TurnPhase
    {
        PlayerTurnStart,  // 玩家回合开始
        PlayerMainPhase,  // 玩家主要行动阶段
        PlayerTurnEnd,    // 玩家回合结束
        
        EnemyTurnStart,   // 敌方回合开始
        EnemyMainPhase,   // 敌方主要行动阶段
        EnemyTurnEnd      // 敌方回合结束
    }
} 