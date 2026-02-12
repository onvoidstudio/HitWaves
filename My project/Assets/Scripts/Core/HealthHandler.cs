using System;
using UnityEngine;

namespace HitWaves.Core
{
    [RequireComponent(typeof(StatHandler))]
    public class HealthHandler : MonoBehaviour, IDamageable
    {
        private const string LOG_TAG = "HealthHandler";
        private const float BLINK_INTERVAL = 0.1f;
        private const float POST_INVINCIBILITY_GRACE = 0.1f;

        private StatHandler _statHandler;
        private float _currentHealth;
        private float _invincibilityTimer;
        private float _graceTimer;
        private SpriteRenderer _spriteRenderer;
        private float _blinkTimer;
        private bool _blinkVisible;

#if UNITY_EDITOR
        [Header("Debug (Read Only)")]
        [SerializeField] private float _debugCurrentHealth;
        [SerializeField] private float _debugMaxHealth;
        [SerializeField] private bool _debugIsAlive;
        [SerializeField] private bool _debugIsInvincible;

        private void UpdateDebugInfo()
        {
            _debugCurrentHealth = _currentHealth;
            _debugMaxHealth = _statHandler != null ? _statHandler.GetStat(StatType.MaxHealth) : 0f;
            _debugIsAlive = IsAlive;
            _debugIsInvincible = IsInvincible;
        }
#endif

        public float CurrentHealth => _currentHealth;
        public bool IsAlive => _currentHealth > 0f;
        public bool IsInvincible => _invincibilityTimer > 0f;

        public event Action<float, float> OnHealthChanged;
        public event Action<GameObject> OnDeath;
        public event Action OnInvincibilityStart;
        public event Action OnInvincibilityEnd;

        private void Awake()
        {
            _statHandler = GetComponent<StatHandler>();

            if (_statHandler == null)
            {
                DebugLogger.LogError(LOG_TAG, "StatHandler 컴포넌트를 찾을 수 없음", this);
                return;
            }

            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            _currentHealth = _statHandler.GetStat(StatType.MaxHealth);
            DebugLogger.Log(LOG_TAG, $"초기화 완료 - 체력: {_currentHealth}", this);

#if UNITY_EDITOR
            UpdateDebugInfo();
#endif
        }

        private void Update()
        {
            if (_graceTimer > 0f)
            {
                _graceTimer -= Time.deltaTime;
            }

            if (!IsInvincible) return;

            _invincibilityTimer -= Time.deltaTime;

            if (_spriteRenderer != null)
            {
                _blinkTimer -= Time.deltaTime;
                if (_blinkTimer <= 0f)
                {
                    _blinkVisible = !_blinkVisible;
                    Color color = _spriteRenderer.color;
                    color.a = _blinkVisible ? 1f : 0.3f;
                    _spriteRenderer.color = color;
                    _blinkTimer = BLINK_INTERVAL;
                }
            }

            if (!IsInvincible)
            {
                EndInvincibility();
            }
        }

        public void TakeDamage(float damage, GameObject attacker)
        {
            if (!IsAlive) return;
            if (damage <= 0f) return;
            if (IsInvincible) return;
            if (_graceTimer > 0f) return;

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
                return;
            }

            StartInvincibility();
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

        private void StartInvincibility()
        {
            float duration = _statHandler.GetStat(StatType.InvincibilityDuration);
            if (duration <= 0f) return;

            _invincibilityTimer = duration;
            _blinkTimer = 0f;
            _blinkVisible = true;

            DebugLogger.Log(LOG_TAG, $"무적 시작 - 지속시간: {duration}초", this);
            OnInvincibilityStart?.Invoke();

#if UNITY_EDITOR
            UpdateDebugInfo();
#endif
        }

        private void EndInvincibility()
        {
            _invincibilityTimer = 0f;
            _graceTimer = POST_INVINCIBILITY_GRACE;

            if (_spriteRenderer != null)
            {
                Color color = _spriteRenderer.color;
                color.a = 1f;
                _spriteRenderer.color = color;
            }

            _blinkVisible = true;

            DebugLogger.Log(LOG_TAG, "무적 종료", this);
            OnInvincibilityEnd?.Invoke();

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
