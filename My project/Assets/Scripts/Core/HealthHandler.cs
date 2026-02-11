using System;
using UnityEngine;

namespace HitWaves.Core
{
    [RequireComponent(typeof(StatHandler))]
    public class HealthHandler : MonoBehaviour, IDamageable
    {
        private const string LOG_TAG = "HealthHandler";

        private StatHandler _statHandler;
        private float _currentHealth;

#if UNITY_EDITOR
        [Header("Debug (Read Only)")]
        [SerializeField] private float _debugCurrentHealth;
        [SerializeField] private float _debugMaxHealth;
        [SerializeField] private bool _debugIsAlive;

        private void UpdateDebugInfo()
        {
            _debugCurrentHealth = _currentHealth;
            _debugMaxHealth = _statHandler != null ? _statHandler.GetStat(StatType.MaxHealth) : 0f;
            _debugIsAlive = IsAlive;
        }
#endif

        public float CurrentHealth => _currentHealth;
        public bool IsAlive => _currentHealth > 0f;

        public event Action<float, float> OnHealthChanged;
        public event Action<GameObject> OnDeath;

        private void Awake()
        {
            _statHandler = GetComponent<StatHandler>();

            if (_statHandler == null)
            {
                DebugLogger.LogError(LOG_TAG, "StatHandler 컴포넌트를 찾을 수 없음", this);
                return;
            }

            _currentHealth = _statHandler.GetStat(StatType.MaxHealth);
            DebugLogger.Log(LOG_TAG, $"초기화 완료 - 체력: {_currentHealth}", this);

#if UNITY_EDITOR
            UpdateDebugInfo();
#endif
        }

        public void TakeDamage(float damage, GameObject attacker)
        {
            if (!IsAlive) return;
            if (damage <= 0f) return;

            _currentHealth -= damage;
            _currentHealth = Mathf.Max(_currentHealth, 0f);

            DebugLogger.Log(LOG_TAG, $"피격 - 데미지: {damage}, 공격자: {(attacker != null ? attacker.name : "null")}, 남은 체력: {_currentHealth}", this);

            OnHealthChanged?.Invoke(_currentHealth, _statHandler.GetStat(StatType.MaxHealth));

#if UNITY_EDITOR
            UpdateDebugInfo();
#endif

            if (!IsAlive)
            {
                Die(attacker);
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;
            if (amount <= 0f) return;

            float maxHealth = _statHandler.GetStat(StatType.MaxHealth);
            _currentHealth = Mathf.Min(_currentHealth + amount, maxHealth);

            DebugLogger.Log(LOG_TAG, $"회복 - 양: {amount}, 현재 체력: {_currentHealth}", this);

            OnHealthChanged?.Invoke(_currentHealth, maxHealth);

#if UNITY_EDITOR
            UpdateDebugInfo();
#endif
        }

        private void Die(GameObject attacker)
        {
            DebugLogger.Log(LOG_TAG, $"사망 - 공격자: {(attacker != null ? attacker.name : "null")}", this);
            OnDeath?.Invoke(attacker);
        }
    }
}
