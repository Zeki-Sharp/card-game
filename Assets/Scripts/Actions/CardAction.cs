using UnityEngine;
using System.Threading.Tasks;

namespace ChessGame
{
    /// <summary>
    /// 卡牌行动的抽象基类
    /// </summary>
    public abstract class CardAction
    {
        protected CardManager CardManager { get; private set; }
        
        public CardAction(CardManager cardManager)
        {
            CardManager = cardManager;
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