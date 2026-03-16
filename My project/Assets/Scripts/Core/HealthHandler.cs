using System;
using UnityEngine;

namespace HitWaves.Core
{
    [RequireComponent(typeof(StatHandler))]
    public class HealthHandler : MonoBehaviour
    {
        private const string LOG_TAG = "HealthHandler";

        private StatHandler _statHandler;
        private float _currentHealth;
        private bool _isDead;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _statHandler.GetStat(StatType.MaxHealth);
        public bool IsDead => _isDead;

        public event Action OnDeath;
        public event Action OnDamaged;
        public event Action<float, float> OnHealthChanged;

        private void Awake()
        {
            _statHandler = GetComponent<StatHandler>();
        }

        private void Start()
        {
            _currentHealth = MaxHealth;

            DebugLogger.Log(LOG_TAG,
                $"{gameObject.name}: 체력 초기화 — {_currentHealth}/{MaxHealth}", this);
        }

        /// <summary>
        /// 데미지를 받는다. 견고함+방어력 합산 이상이어야 실제 피해 발생.
        /// 무효화 시 false 반환 (히트 이펙트, 넉백 등 적용하지 않음).
        /// </summary>
        public bool TakeDamage(float amount)
        {
            if (_isDead) return false;
            if (amount <= 0f) return false;

            float toughness = _statHandler.GetStat(StatType.Toughness);
            float defense = _statHandler.GetStat(StatType.Defense);
            float totalProtection = toughness + defense;

            if (amount <= totalProtection)
            {
                DebugLogger.Log(LOG_TAG,
                    $"{gameObject.name}: 데미지 무효화 — " +
                    $"damage:{amount} <= protection:{totalProtection} " +
                    $"(toughness:{toughness} + defense:{defense})", this);
                return false;
            }

            float finalDamage = amount - totalProtection;
            _currentHealth -= finalDamage;
            _currentHealth = Mathf.Max(_currentHealth, 0f);

            DebugLogger.Log(LOG_TAG,
                $"{gameObject.name}: 피격 — {finalDamage} 데미지 " +
                $"(원본:{amount} - 보호:{totalProtection}), " +
                $"남은 체력: {_currentHealth}/{MaxHealth}", this);

            OnDamaged?.Invoke();
            OnHealthChanged?.Invoke(_currentHealth, MaxHealth);

            if (_currentHealth <= 0f)
            {
                Die();
            }

            return true;
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;

            DebugLogger.Log(LOG_TAG,
                $"{gameObject.name}: 사망", this);

            OnDeath?.Invoke();
        }
    }
}
