using UnityEngine;

namespace ChessGame
{
    public class Cell
    {
        // 单元格在棋盘中的坐标
        public Vector2Int Position { get; private set; }
        
        // 添加无参构造函数
        public Cell()
        {
            Position = new Vector2Int(0, 0);
        }
        
        // 保留带参数的构造函数
        public Cell(Vector2Int position)
        {
            Position = position;
        }
    }
} 