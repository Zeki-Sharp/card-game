using UnityEngine;

namespace ChessGame
{
    [System.Serializable]
    public class LevelCardEntry
    {
        public int cardId;        // 卡牌ID
        public int count = 1;     // 卡牌数量
        public int ownerId = 0;   // 所有者ID（0为玩家，1为敌方）
        public bool isFaceDown = true; // 是否背面朝上
        public int faceUpCount = 0; // 正面朝上的卡牌数量（仅当isFaceDown为false时有效）
        public Vector2Int[] positions; // 指定位置（如果为空，则随机放置）
    }
} 