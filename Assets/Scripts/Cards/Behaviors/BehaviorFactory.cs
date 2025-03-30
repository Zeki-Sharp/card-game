using UnityEngine;

namespace ChessGame.Cards
{
    public static class BehaviorFactory
    {
        // 创建移动行为
        public static IMovementBehavior CreateMovementBehavior(MovementType type)
        {
            switch (type)
            {
                case MovementType.Knight:
                    // 暂时使用默认行为
                    return new DefaultMovementBehavior();
                case MovementType.Diagonal:
                    // 暂时使用默认行为
                    return new DefaultMovementBehavior();
                case MovementType.Queen:
                    // 暂时使用默认行为
                    return new DefaultMovementBehavior();
                case MovementType.Special:
                    return new SpecialMovementBehavior();
                case MovementType.Default:
                default:
                    return new DefaultMovementBehavior();
            }
        }
        
        // 创建攻击行为
        public static IAttackBehavior CreateAttackBehavior(AttackType type)
        {
            switch (type)
            {
                case AttackType.Knight:
                    // 暂时使用默认行为
                    return new DefaultAttackBehavior();
                case AttackType.Diagonal:
                    // 暂时使用默认行为
                    return new DefaultAttackBehavior();
                case AttackType.Range:
                    // 暂时使用默认行为
                    return new DefaultAttackBehavior();
                case AttackType.Special:
                    return new SpecialAttackBehavior();
                case AttackType.Default:
                default:
                    return new DefaultAttackBehavior();
            }
        }
    }
} 