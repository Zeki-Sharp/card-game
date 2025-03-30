namespace ChessGame.Cards
{
    public enum AttackType
    {
        Default,    // 默认攻击（上下左右）
        Knight,     // 骑士攻击（L形）
        Diagonal,   // 对角线攻击
        Range,      // 远程攻击（范围内所有敌人）
        Special     // 特殊攻击（只能攻击距离为2的格子）
    }
} 