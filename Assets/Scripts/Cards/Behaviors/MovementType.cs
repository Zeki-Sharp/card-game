namespace ChessGame.Cards
{
    public enum MovementType
    {
        Default,    // 默认移动（上下左右）
        Knight,     // 骑士移动（L形）
        Diagonal,   // 对角线移动
        Queen,      // 皇后移动（八方向）
        Special     // 特殊移动（距离为2）
    }
} 