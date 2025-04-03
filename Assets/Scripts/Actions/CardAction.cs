using UnityEngine;
using System.Threading.Tasks;

namespace ChessGame
{
    /// <summary>
    /// 卡牌行动的抽象基类
    /// </summary>
    public abstract class CardAction
    {
        public CardManager CardManager { get; protected set; }
        
        protected CardAction(CardManager cardManager)
        {
            CardManager = cardManager;
            
            // 添加空检查
            if (CardManager == null)
            {
                Debug.LogError("CardAction: CardManager 为空");
                // 尝试从场景中获取
                CardManager = GameObject.FindObjectOfType<CardManager>();
                if (CardManager == null)
                {
                    Debug.LogError("CardAction: 无法从场景中找到 CardManager");
                }
            }
        }
        
        /// <summary>
        /// 检查行动是否可以执行
        /// </summary>
        /// <returns>如果可以执行返回true，否则返回false</returns>
        public abstract bool CanExecute();
        
        /// <summary>
        /// 执行行动
        /// </summary>
        /// <returns>行动是否成功执行</returns>
        public abstract bool Execute();
        
        /// <summary>
        /// 取消行动
        /// </summary>
        public virtual void Cancel()
        {
            // 默认实现为空，子类可以根据需要重写
        }
    }
} 