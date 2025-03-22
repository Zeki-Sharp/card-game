using System.Collections.Generic;
using UnityEngine;
using ChessGame.Utils;

namespace ChessGame
{
    public class CardDataProvider : MonoBehaviour
    {
        [SerializeField] private string csvResourcePath = "Cards/CardData";
        [SerializeField] private List<Sprite> cardSprites = new List<Sprite>();
        
        private List<CardData> _availableCards = new List<CardData>();
        private bool _isInitialized = false;
        
        private void Awake()
        {
            LoadCardData();
        }
        
        // 加载卡牌数据
        public void LoadCardData()
        {
            _availableCards.Clear();
            
            Debug.Log("开始加载卡牌数据，路径：" + csvResourcePath);
            
            // 从Resources加载CSV数据
            List<Dictionary<string, string>> csvData = CSVReader.ReadCSVFromResources(csvResourcePath);
            
            Debug.Log($"从CSV读取了 {csvData.Count} 条数据");
            
            foreach (var row in csvData)
            {
                try
                {
                    // 添加调试信息
                    string keys = string.Join(", ", row.Keys);
                    Debug.Log($"CSV行包含的键: {keys}");
                    
                    // 检查必要的键是否存在
                    if (!row.ContainsKey("Id") || !row.ContainsKey("Name") || 
                        !row.ContainsKey("Attack") || !row.ContainsKey("Health") ||
                        !row.ContainsKey("SpriteIndex"))
                    {
                        Debug.LogError("CSV行缺少必要的列: " + string.Join(", ", row.Keys));
                        continue;
                    }
                    
                    // 解析CSV数据
                    int id = int.Parse(row["Id"]);
                    string name = row["Name"];
                    int attack = int.Parse(row["Attack"]);
                    int health = int.Parse(row["Health"]);
                    int spriteIndex = int.Parse(row["SpriteIndex"]);
                    
                    // 确保SpriteIndex在有效范围内
                    if (spriteIndex < 0 || spriteIndex >= cardSprites.Count)
                    {
                        Debug.LogWarning($"卡牌 {name} 的SpriteIndex ({spriteIndex}) 超出范围，使用默认图片");
                        spriteIndex = 0;
                    }
                    
                    // 获取对应的Sprite
                    Sprite sprite = cardSprites[spriteIndex];
                    
                    // 创建卡牌数据
                    CardData cardData = new CardData(id, name, attack, health, sprite);
                    _availableCards.Add(cardData);
                    
                    Debug.Log($"加载卡牌: ID={id}, 名称={name}, 攻击={attack}, 生命={health}, 图片索引={spriteIndex}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"解析卡牌数据时出错: {e.Message}, 行数据: {string.Join(", ", row.Keys)}");
                }
            }
            
            Debug.Log($"成功加载了 {_availableCards.Count} 张卡牌");
            _isInitialized = true;
        }
        
        // 获取所有可用卡牌数据
        public List<CardData> GetAllCardData()
        {
            if (!_isInitialized)
            {
                LoadCardData();
            }
            
            return new List<CardData>(_availableCards);
        }
    }
} 