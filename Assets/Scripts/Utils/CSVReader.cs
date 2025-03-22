using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ChessGame.Utils
{
    public static class CSVReader
    {
        // 从文件读取CSV数据
        public static List<Dictionary<string, string>> ReadCSVFile(string filePath)
        {
            List<Dictionary<string, string>> data = new List<Dictionary<string, string>>();
            
            try
            {
                // 读取所有文本
                string[] lines = File.ReadAllLines(filePath);
                if (lines.Length <= 1)
                {
                    Debug.LogWarning($"CSV文件 {filePath} 没有数据行");
                    return data;
                }
                
                // 解析标题行
                string[] headers = ParseCSVLine(lines[0]);
                
                // 解析数据行
                for (int i = 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i]))
                        continue;
                        
                    string[] values = ParseCSVLine(lines[i]);
                    Dictionary<string, string> entry = new Dictionary<string, string>();
                    
                    // 确保值的数量与标题数量一致
                    int valueCount = Mathf.Min(headers.Length, values.Length);
                    
                    for (int j = 0; j < valueCount; j++)
                    {
                        entry[headers[j]] = values[j];
                    }
                    
                    data.Add(entry);
                }
                
                Debug.Log($"成功从 {filePath} 读取了 {data.Count} 条数据");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"读取CSV文件 {filePath} 时出错: {e.Message}");
            }
            
            return data;
        }
        
        // 从Resources文件夹读取CSV数据
        public static List<Dictionary<string, string>> ReadCSVFromResources(string resourcePath)
        {
            List<Dictionary<string, string>> data = new List<Dictionary<string, string>>();
            
            try
            {
                // 从Resources加载文本资源
                TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
                if (textAsset == null)
                {
                    Debug.LogError($"找不到CSV资源: {resourcePath}");
                    return data;
                }
                
                string[] lines = textAsset.text.Split('\n');
                if (lines.Length <= 1)
                {
                    Debug.LogWarning($"CSV资源 {resourcePath} 没有数据行");
                    return data;
                }
                
                // 解析标题行
                string[] headers = ParseCSVLine(lines[0]);
                
                // 解析数据行
                for (int i = 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i]))
                        continue;
                        
                    string[] values = ParseCSVLine(lines[i]);
                    Dictionary<string, string> entry = new Dictionary<string, string>();
                    
                    // 确保值的数量与标题数量一致
                    int valueCount = Mathf.Min(headers.Length, values.Length);
                    
                    for (int j = 0; j < valueCount; j++)
                    {
                        entry[headers[j]] = values[j];
                    }
                    
                    data.Add(entry);
                }
                
                Debug.Log($"成功从资源 {resourcePath} 读取了 {data.Count} 条数据");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"读取CSV资源 {resourcePath} 时出错: {e.Message}");
            }
            
            return data;
        }
        
        // 解析CSV行
        private static string[] ParseCSVLine(string line)
        {
            List<string> result = new List<string>();
            bool inQuotes = false;
            string currentValue = "";
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentValue);
                    currentValue = "";
                }
                else
                {
                    currentValue += c;
                }
            }
            
            // 添加最后一个值
            result.Add(currentValue);
            
            return result.ToArray();
        }
    }
} 