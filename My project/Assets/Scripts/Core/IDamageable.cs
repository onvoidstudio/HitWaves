using UnityEngine;

namespace HitWaves.Core
{
    public interface IDamageable
    {
        void TakeDamage(float damage, GameObject attacker);
        bool IsAlive { get; }
    }
}
