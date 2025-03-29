using System.Collections.Generic;
using UnityEngine;

namespace ChessGame
{
    [CreateAssetMenu(fileName = "LevelCardConfiguration", menuName = "ChessGame/Level Card Configuration")]
    public class LevelCardConfiguration : ScriptableObject
    {
        [SerializeField] private List<LevelCardEntry> playerCards = new List<LevelCardEntry>();
        [SerializeField] private List<LevelCardEntry> enemyCards = new List<LevelCardEntry>();
        
        public List<LevelCardEntry> PlayerCards => playerCards;
        public List<LevelCardEntry> EnemyCards => enemyCards;
    }
} 