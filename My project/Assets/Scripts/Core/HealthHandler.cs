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
        /// 데미지를 받는다. 이미 사망 상태면 무시.
        /// </summary>
        public void TakeDamage(float amount)
        {
            if (_isDead) return;
            if (amount <= 0f) return;

            _currentHealth -= amount;
            _currentHealth = Mathf.Max(_currentHealth, 0f);

            DebugLogger.Log(LOG_TAG,
                $"{gameObject.name}: 피격 — {amount} 데미지, " +
                $"남은 체력: {_currentHealth}/{MaxHealth}", this);

            OnDamaged?.Invoke();
            OnHealthChanged?.Invoke(_currentHealth, MaxHealth);

            if (_currentHealth <= 0f)
            {
                Die();
            }
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
