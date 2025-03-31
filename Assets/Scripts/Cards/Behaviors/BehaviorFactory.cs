using UnityEngine;

namespace ChessGame.Cards
{
    // 临时保留的类，将逐步移除
    public class BehaviorFactory
    {
        // 空方法，不执行任何操作
        public static IMovementBehavior CreateMovementBehavior(MovementType type)
        {
            return new DefaultMovementBehavior();
        }
        
        // 空方法，不执行任何操作
        public static IAttackBehavior CreateAttackBehavior(AttackType type)
        {
            return new DefaultAttackBehavior();
        }
    }
    
    // 临时接口和实现类
    public interface IMovementBehavior {}
    public interface IAttackBehavior {}
    
    public class DefaultMovementBehavior : IMovementBehavior {}
    public class DefaultAttackBehavior : IAttackBehavior {}
} 