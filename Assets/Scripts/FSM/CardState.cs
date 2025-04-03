namespace ChessGame.FSM
{
    // 卡牌状态枚举
    public enum CardState
    {
        Idle,       // 对应IdleState
        Selected,   // 对应SelectedState
        Moving,     // 对应MoveState
        Attacking,  // 对应AttackState
        Ability     // 对应AbilityState
    }
} 