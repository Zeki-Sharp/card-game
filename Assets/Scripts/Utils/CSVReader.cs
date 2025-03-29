using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Text.RegularExpressions;

namespace ChessGame
{
    public class CSVReader
    {
        private static readonly string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";
        private static readonly string LINE_SPLIT_RE = @"\r\n|\n\r|\n|\r";
        private static readonly char[] TRIM_CHARS = { '\"' };

        /// <summary>
        /// 从CSV文件读取卡牌数据
        /// </summary>
        /// <param name="filePath">CSV文件路径（相对于Resources文件夹）</param>
        /// <returns>卡牌数据列表</returns>
        public static List<CardData> ReadCardDataFromCSV(string filePath)
        {
            List<CardData> cardDataList = new List<CardData>();
            TextAsset csvFile = Resources.Load<TextAsset>(filePath);

            if (csvFile == null)
            {
                Debug.LogError($"找不到CSV文件: {filePath}");
                return cardDataList;
            }

            string[] lines = Regex.Split(csvFile.text, LINE_SPLIT_RE);
            if (lines.Length <= 1) return cardDataList;

            // 获取表头
            string[] headers = Regex.Split(lines[0], SPLIT_RE);
            for (int i = 0; i < headers.Length; i++)
            {
                headers[i] = headers[i].Trim(TRIM_CHARS);
            }

            // 解析每一行数据
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0) continue;

                string[] values = Regex.Split(line, SPLIT_RE);
                if (values.Length == 0 || values[0].Trim() == "") continue;

                // 创建卡牌数据对象
                CardData cardData = new CardData();
                Dictionary<string, string> entry = new Dictionary<string, string>();

                for (int j = 0; j < headers.Length && j < values.Length; j++)
                {
                    string value = values[j].Trim(TRIM_CHARS);
                    entry[headers[j]] = value;
                }

                // 解析ID
                if (entry.TryGetValue("Id", out string idStr) && int.TryParse(idStr, out int id))
                {
                    // 使用反射设置私有字段
                    System.Reflection.FieldInfo idField = typeof(CardData).GetField("id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (idField != null)
                    {
                        idField.SetValue(cardData, id);
                    }
                }

                // 解析名称
                if (entry.TryGetValue("Name", out string name))
                {
                    System.Reflection.FieldInfo nameField = typeof(CardData).GetField("name", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (nameField != null)
                    {
                        nameField.SetValue(cardData, name);
                    }
                }

                // 解析攻击力
                if (entry.TryGetValue("Attack", out string attackStr) && int.TryParse(attackStr, out int attack))
                {
                    System.Reflection.FieldInfo attackField = typeof(CardData).GetField("attack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (attackField != null)
                    {
                        attackField.SetValue(cardData, attack);
                    }
                }

                // 解析生命值
                if (entry.TryGetValue("Health", out string healthStr) && int.TryParse(healthStr, out int health))
                {
                    System.Reflection.FieldInfo healthField = typeof(CardData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (healthField != null)
                    {
                        healthField.SetValue(cardData, health);
                    }
                }

                // 解析阵营
                if (entry.TryGetValue("Faction", out string factionStr) && int.TryParse(factionStr, out int faction))
                {
                    System.Reflection.FieldInfo factionField = typeof(CardData).GetField("faction", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (factionField != null)
                    {
                        factionField.SetValue(cardData, faction);
                    }
                }

                // 解析移动范围
                if (entry.TryGetValue("MoveRange", out string moveRangeStr) && int.TryParse(moveRangeStr, out int moveRange))
                {
                    System.Reflection.FieldInfo moveRangeField = typeof(CardData).GetField("moveRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (moveRangeField != null)
                    {
                        moveRangeField.SetValue(cardData, moveRange);
                    }
                }

                // 解析攻击范围
                if (entry.TryGetValue("AttackRange", out string attackRangeStr) && int.TryParse(attackRangeStr, out int attackRange))
                {
                    System.Reflection.FieldInfo attackRangeField = typeof(CardData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (attackRangeField != null)
                    {
                        attackRangeField.SetValue(cardData, attackRange);
                    }
                }

                // 解析图片名称并加载图片
                if (entry.TryGetValue("ImageName", out string imageName) && !string.IsNullOrEmpty(imageName))
                {
                    Sprite sprite = Resources.Load<Sprite>($"CardImages/{imageName}");
                    if (sprite != null)
                    {
                        System.Reflection.FieldInfo imageField = typeof(CardData).GetField("image", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (imageField != null)
                        {
                            imageField.SetValue(cardData, sprite);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"找不到卡牌图片: CardImages/{imageName}");
                    }
                }

                cardDataList.Add(cardData);
            }

            Debug.Log($"从CSV文件 {filePath} 中读取了 {cardDataList.Count} 张卡牌数据");
            return cardDataList;
        }

        /// <summary>
        /// 从CSV文件读取数据到字典列表
        /// </summary>
        /// <param name="filePath">CSV文件路径（相对于Resources文件夹）</param>
        /// <returns>字典列表，每个字典代表一行数据</returns>
        public static List<Dictionary<string, string>> ReadCSV(string filePath)
        {
            List<Dictionary<string, string>> list = new List<Dictionary<string, string>>();
            TextAsset csvFile = Resources.Load<TextAsset>(filePath);

            if (csvFile == null)
            {
                Debug.LogError($"找不到CSV文件: {filePath}");
                return list;
            }

            string[] lines = Regex.Split(csvFile.text, LINE_SPLIT_RE);
            if (lines.Length <= 1) return list;

            // 获取表头
            string[] headers = Regex.Split(lines[0], SPLIT_RE);
            for (int i = 0; i < headers.Length; i++)
            {
                headers[i] = headers[i].Trim(TRIM_CHARS);
            }

            // 解析每一行数据
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0) continue;

                string[] values = Regex.Split(line, SPLIT_RE);
                if (values.Length == 0 || values[0].Trim() == "") continue;

                Dictionary<string, string> entry = new Dictionary<string, string>();
                for (int j = 0; j < headers.Length && j < values.Length; j++)
                {
                    string value = values[j].Trim(TRIM_CHARS);
                    entry[headers[j]] = value;
                }
                list.Add(entry);
            }

            return list;
        }
    }
} 