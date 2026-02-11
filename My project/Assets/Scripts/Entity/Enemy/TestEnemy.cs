using UnityEngine;
using HitWaves.Core;

namespace HitWaves.Entity.Enemy
{
    [RequireComponent(typeof(StatHandler))]
    [RequireComponent(typeof(HealthHandler))]
    public class TestEnemy : MonoBehaviour
    {
        private const string LOG_TAG = "TestEnemy";

        private HealthHandler _healthHandler;

        private void Awake()
        {
            _healthHandler = GetComponent<HealthHandler>();

            if (_healthHandler == null)
            {
                DebugLogger.LogError(LOG_TAG, "HealthHandler 컴포넌트를 찾을 수 없음", this);
                return;
            }

            _healthHandler.OnHealthChanged += HandleHealthChanged;
            _healthHandler.OnDeath += HandleDeath;

            DebugLogger.Log(LOG_TAG, $"초기화 완료 - {gameObject.name}", this);
        }

        private void OnDestroy()
        {
            if (_healthHandler != null)
            {
                _healthHandler.OnHealthChanged -= HandleHealthChanged;
                _healthHandler.OnDeath -= HandleDeath;
            }
        }

        private void HandleHealthChanged(float currentHealth, float maxHealth)
        {
            DebugLogger.Log(LOG_TAG, $"{gameObject.name} 체력 변경: {currentHealth}/{maxHealth}", this);
        }

        private void HandleDeath(GameObject attacker)
        {
            DebugLogger.Log(LOG_TAG, $"{gameObject.name} 사망 - 공격자: {(attacker != null ? attacker.name : "null")}", this);
            Destroy(gameObject);
        }
    }
}
