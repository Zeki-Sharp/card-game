using System.Collections.Generic;
using UnityEngine;

namespace ChessGame
{
    [CreateAssetMenu(fileName = "ChessFactory", menuName = "ChessGame/Chess Factory")]
    public class ChessFactory : ScriptableObject
    {
        [System.Serializable]
        public class ChessPieceConfig
        {
            public string name;
            public int attack;
            public int health;
            public Sprite sprite;
            public int spawnWeight = 1;
        }
        
        [SerializeField] private List<ChessPieceConfig> pieceConfigs = new List<ChessPieceConfig>();
        
        // 根据配置创建卡牌数据
        public CardData CreateCardData(ChessPieceConfig config)
        {
            int id = Random.Range(1000, 9999);
            return new CardData(id, config.name, config.attack, config.health, config.sprite);
        }
        
        // 随机选择一个棋子配置
        public ChessPieceConfig GetRandomConfig()
        {
            if (pieceConfigs.Count == 0)
                return null;
                
            // 计算总权重
            int totalWeight = 0;
            foreach (var config in pieceConfigs)
            {
                totalWeight += config.spawnWeight;
            }
            
            // 随机选择
            int randomValue = Random.Range(0, totalWeight);
            int currentWeight = 0;
            
            foreach (var config in pieceConfigs)
            {
                currentWeight += config.spawnWeight;
                if (randomValue < currentWeight)
                {
                    return config;
                }
            }
            
            // 默认返回第一个
            return pieceConfigs[0];
        }
        
        // 生成指定数量的随机卡牌数据
        public List<CardData> GenerateRandomCardDatas(int count)
        {
            List<CardData> result = new List<CardData>();
            
            for (int i = 0; i < count; i++)
            {
                ChessPieceConfig config = GetRandomConfig();
                if (config != null)
                {
                    result.Add(CreateCardData(config));
                }
            }
            
            return result;
        }
    }
} 