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
                case MovementType.Assassin:
                    return new AssassinMovementBehavior();
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
                case AttackType.Archer:
                    return new ArcherAttackBehavior();
                case AttackType.Assassin:
                    return new AssassinAttackBehavior();
                case AttackType.Default:
                default:
                    return new DefaultAttackBehavior();
            }
        }
    }
} 