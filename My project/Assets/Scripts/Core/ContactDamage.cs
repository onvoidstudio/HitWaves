using UnityEngine;

namespace HitWaves.Core
{
    [RequireComponent(typeof(StatHandler))]
    [RequireComponent(typeof(HealthHandler))]
    public class ContactDamage : MonoBehaviour
    {
        private const string LOG_TAG = "ContactDamage";

        [Header("접촉 데미지")]
        [Tooltip("이 엔티티가 접촉 시 상대에게 주는 데미지 (Damage 스탯과 독립)")]
        [Min(0f)]
        [SerializeField] private float _contactDamage = 1f;

        [Header("무적 설정")]
        [Tooltip("피격 후 무적 시간 (초)")]
        [Min(0f)]
        [SerializeField] private float _invincibilityDuration = 0.5f;

        public float ContactDamageValue => _contactDamage;
        public float InvincibilityDuration => _invincibilityDuration;

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
        /// 다른 진영의 엔티티와 접촉 시, 상대의 접촉 데미지만큼 자신이 피해를 받는다.
        /// 접촉 데미지는 상대의 ContactDamage._contactDamage 값 (Damage 스탯과 독립).
        /// 무적 시간 내에는 피해를 받지 않는다.
        /// </summary>
        private void HandleContact(GameObject other)
        {
            if (_healthHandler.IsDead) return;
            if (Time.time - _lastDamageTime < _invincibilityDuration) return;

            StatHandler otherStats = other.GetComponent<StatHandler>();
            if (otherStats == null) return;

            if (otherStats.Faction == _statHandler.Faction) return;

            ContactDamage otherContact = other.GetComponent<ContactDamage>();
            if (otherContact == null) return;

            float damage = otherContact.ContactDamageValue;
            if (damage <= 0f) return;

            bool damageApplied = _healthHandler.TakeDamage(damage);

            if (damageApplied)
            {
                _lastDamageTime = Time.time;

                DebugLogger.Log(LOG_TAG,
                    $"{gameObject.name}: {other.name}({otherStats.Faction})과 접촉 — " +
                    $"{damage} 접촉 데미지 수신", this);
            }
        }
    }
}
