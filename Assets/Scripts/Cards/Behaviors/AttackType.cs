namespace ChessGame.Cards
{
    public enum AttackType
    {
        Default,    // 默认攻击（上下左右）
        Archer,     // 特殊攻击（只能攻击距离为2的格子）
        Assassin,   // 刺客攻击（上下左右距离1和2的位置，以及斜向四个方向距离1的位置）
    }
} 