using UnityEngine;

namespace HitWaves.Core.Attack.Behaviors
{
    public class ContactBehavior : IAttackBehavior
    {
        private const string LOG_TAG = "ContactBehavior";
        private const string INTERACTABLE_TAG = "Interactable";

        public void Initialize(AttackHandler handler)
        {
        }

        public int Execute(AttackHandler handler, Vector2 direction)
        {
            float damage = handler.StatHandler.GetStat(StatType.Damage);
            int maxHitCount = Mathf.Max(1, (int)handler.StatHandler.GetStat(StatType.MaxHitCount));

            int candidateCount = Physics2D.OverlapCircleNonAlloc(
                handler.transform.position, handler.Range, handler.HitBuffer, handler.TargetLayer);

            int hitCount = 0;

            for (int i = 0; i < candidateCount; i++)
            {
                Collider2D candidate = handler.HitBuffer[i];
                if (hitCount >= maxHitCount) break;
                if (candidate.gameObject == handler.gameObject) continue;
                if (!candidate.CompareTag(INTERACTABLE_TAG)) continue;

                IDamageable damageable = candidate.GetComponent<IDamageable>();
                if (damageable == null || !damageable.IsAlive) continue;

                damageable.TakeDamage(damage, handler.gameObject);
                hitCount++;
                DebugLogger.Log(LOG_TAG, $"접촉 히트: {candidate.gameObject.name}, 데미지: {damage}", handler);
            }

            return hitCount;
        }

        public void Cleanup()
        {
        }
    }
}
