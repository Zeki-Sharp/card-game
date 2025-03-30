using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;

namespace ChessGame
{
    public class LevelCardConfigurationCSVReader
    {
        /// <summary>
        /// 从单个CSV文件读取关卡卡牌配置
        /// </summary>
        /// <param name="levelCSVPath">关卡配置CSV文件路径（相对于Resources文件夹）</param>
        /// <returns>关卡卡牌配置</returns>
        public static LevelCardConfiguration ReadFromCSV(string levelCSVPath)
        {
            // 创建一个新的配置对象
            LevelCardConfiguration config = ScriptableObject.CreateInstance<LevelCardConfiguration>();
            
            // 读取所有卡牌条目
            List<LevelCardEntry> allEntries = ReadCardEntriesFromCSV(levelCSVPath);
            
            // 分离玩家和敌方卡牌
            List<LevelCardEntry> playerCards = new List<LevelCardEntry>();
            List<LevelCardEntry> enemyCards = new List<LevelCardEntry>();
            
            foreach (var entry in allEntries)
            {
                if (entry.ownerId == 0)
                    playerCards.Add(entry);
                else
                    enemyCards.Add(entry);
            }
            
            // 使用反射设置私有字段
            System.Reflection.FieldInfo playerCardsField = typeof(LevelCardConfiguration).GetField("playerCards", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            System.Reflection.FieldInfo enemyCardsField = typeof(LevelCardConfiguration).GetField("enemyCards", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (playerCardsField != null)
                playerCardsField.SetValue(config, playerCards);
                
            if (enemyCardsField != null)
                enemyCardsField.SetValue(config, enemyCards);
            
            return config;
        }
        
        /// <summary>
        /// 从CSV文件读取卡牌条目列表
        /// </summary>
        private static List<LevelCardEntry> ReadCardEntriesFromCSV(string filePath)
        {
            List<LevelCardEntry> entries = new List<LevelCardEntry>();
            
            TextAsset csvFile = Resources.Load<TextAsset>(filePath);
            if (csvFile == null)
            {
                Debug.LogError($"找不到CSV文件: {filePath}");
                return entries;
            }
            
            string[] lines = Regex.Split(csvFile.text, @"\r\n|\n\r|\n|\r");
            if (lines.Length <= 1) return entries;
            
            // 获取表头
            string[] headers = Regex.Split(lines[0], @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))");
            for (int i = 0; i < headers.Length; i++)
            {
                headers[i] = headers[i].Trim('"');
            }
            
            // 解析每一行数据
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0) continue;
                
                string[] values = Regex.Split(line, @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))");
                if (values.Length == 0 || values[0].Trim() == "") continue;
                
                LevelCardEntry entry = new LevelCardEntry();
                entry.ownerId = 0; // 默认为玩家
                
                for (int j = 0; j < headers.Length && j < values.Length; j++)
                {
                    string value = values[j].Trim('"');
                    
                    switch (headers[j].ToLower())
                    {
                        case "cardid":
                            if (int.TryParse(value, out int cardId))
                                entry.cardId = cardId;
                            break;
                            
                        case "count":
                            if (int.TryParse(value, out int count))
                                entry.count = count;
                            break;
                            
                        case "ownerid":
                            if (int.TryParse(value, out int ownerId))
                                entry.ownerId = ownerId;
                            break;
                            
                        case "isfacedown":
                            entry.isFaceDown = value.ToLower() == "true" || value == "1";
                            break;
                            
                        case "faceupcount":
                            if (int.TryParse(value, out int faceUpCount))
                                entry.faceUpCount = faceUpCount;
                            break;
                            
                        case "positions":
                            // 如果位置字段为空，则保持positions为null，表示随机放置
                            if (!string.IsNullOrEmpty(value))
                            {
                                // 解析位置格式，例如 "(0,1);(2,3)"
                                string[] posStrings = value.Split(';');
                                List<Vector2Int> positions = new List<Vector2Int>();
                                
                                foreach (string posStr in posStrings)
                                {
                                    // 提取坐标值
                                    Match match = Regex.Match(posStr, @"\((\d+),(\d+)\)");
                                    if (match.Success && match.Groups.Count >= 3)
                                    {
                                        int x = int.Parse(match.Groups[1].Value);
                                        int y = int.Parse(match.Groups[2].Value);
                                        positions.Add(new Vector2Int(x, y));
                                    }
                                }
                                
                                if (positions.Count > 0)
                                    entry.positions = positions.ToArray();
                            }
                            break;
                    }
                }
                
                entries.Add(entry);
            }
            
            return entries;
        }
    }
} 