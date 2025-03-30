using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace ChessGame
{
    /// <summary>
    /// 负责卡牌的初始化和生成
    /// </summary>
    public class CardInitializer : MonoBehaviour
    {
        private static CardInitializer _instance;
        
        public static CardInitializer Instance
        {
            get { return _instance; }
        }
        
        [SerializeField] private CardManager cardManager;
        [SerializeField] private CardDataProvider cardDataProvider;
        
        private void Awake()
        {
            // 查找必要组件
            if (cardManager == null)
                cardManager = FindObjectOfType<CardManager>();
                
            if (cardDataProvider == null)
                cardDataProvider = FindObjectOfType<CardDataProvider>();
        }
        
    }
} 