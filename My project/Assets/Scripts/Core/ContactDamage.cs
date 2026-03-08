using UnityEngine;

namespace HitWaves.Core
{
    [RequireComponent(typeof(StatHandler))]
    [RequireComponent(typeof(HealthHandler))]
    public class ContactDamage : MonoBehaviour
    {
        private const string LOG_TAG = "ContactDamage";

        [Header("무적 설정")]
        [Tooltip("피격 후 무적 시간 (초)")]
        [Min(0f)]
        [SerializeField] private float _invincibilityDuration = 0.5f;

        private StatHandler _statHandler;
        private HealthHandler _healthHandler;
        private float _lastDamageTime = -999f;

        private void Awake()
        {
            _statHandler = GetComponent<StatHandler>();
            _healthHandler = GetComponent<HealthHandler>();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleContact(collision.gameObject);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            HandleContact(collision.gameObject);
        }

        /// <summary>
        /// 다른 진영의 엔티티와 접촉 시, 상대의 Damage 스탯만큼 자신이 피해를 받는다.
        /// 무적 시간 내에는 피해를 받지 않는다.
        /// </summary>
        private void HandleContact(GameObject other)
        {
            if (_healthHandler.IsDead) return;
            if (Time.time - _lastDamageTime < _invincibilityDuration) return;

            StatHandler otherStats = other.GetComponent<StatHandler>();
            if (otherStats == null) return;

            if (otherStats.Faction == _statHandler.Faction) return;

            float damage = otherStats.GetStat(StatType.Damage);
            if (damage <= 0f) return;

            _healthHandler.TakeDamage(damage);
            _lastDamageTime = Time.time;

            DebugLogger.Log(LOG_TAG,
                $"{gameObject.name}: {other.name}({otherStats.Faction})과 접촉 — " +
                $"{damage} 데미지 수신", this);
        }
    }
}
