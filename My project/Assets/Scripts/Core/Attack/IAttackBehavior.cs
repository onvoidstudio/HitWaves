using UnityEngine;

namespace HitWaves.Core.Attack
{
    public interface IAttackBehavior
    {
        void Initialize(AttackHandler handler);
        int Execute(AttackHandler handler, Vector2 direction);
        void Cleanup();
    }
}
