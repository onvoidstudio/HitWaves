using System;
using UnityEngine;

namespace HitWaves.Core
{
    [RequireComponent(typeof(StatHandler))]
    public class StaminaHandler : MonoBehaviour
    {
        private const string LOG_TAG = "StaminaHandler";

        private StatHandler _statHandler;
        private float _currentStamina;
        private float _timeSinceLastConsume;
        private float _regenTickTimer;
        private bool _isExhausted;

        public float CurrentStamina => _currentStamina;
        public float MaxStamina => _statHandler.GetStat(StatType.MaxStamina);
        public bool IsExhausted => _isExhausted;

        /// <summary>
        /// 스태미나 변경 시 (현재값, 최대값)
        /// </summary>
        public event Action<float, float> OnStaminaChanged;

        /// <summary>
        /// 고갈 상태 변경 시 (고갈 여부)
        /// </summary>
        public event Action<bool> OnExhaustedChanged;

        private void Awake()
        {
            _statHandler = GetComponent<StatHandler>();
        }

        private void Start()
        {
            _currentStamina = MaxStamina;
            _timeSinceLastConsume = float.MaxValue;

            DebugLogger.Log(LOG_TAG,
                $"{gameObject.name}: 스태미나 초기화 — {_currentStamina}/{MaxStamina}", this);
        }

        private void Update()
        {
            _timeSinceLastConsume += Time.deltaTime;

            float regenDelay = _statHandler.GetStat(StatType.StaminaRegenDelay);

            if (_timeSinceLastConsume >= regenDelay && _currentStamina < MaxStamina)
            {
                Regenerate();
            }
        }

        /// <summary>
        /// 스태미나를 소모한다. 부족하면 false 반환.
        /// </summary>
        public bool Consume(float amount)
        {
            if (amount <= 0f) return true;
            if (_currentStamina < amount) return false;

            _currentStamina -= amount;
            _currentStamina = Mathf.Max(_currentStamina, 0f);
            _timeSinceLastConsume = 0f;
            _regenTickTimer = 0f;

            DebugLogger.Log(LOG_TAG,
                $"{gameObject.name}: 스태미나 소모 — {amount}, " +
                $"남은: {_currentStamina}/{MaxStamina}", this);

            OnStaminaChanged?.Invoke(_currentStamina, MaxStamina);

            if (_currentStamina <= 0f && !_isExhausted)
            {
                _isExhausted = true;
                OnExhaustedChanged?.Invoke(true);

                DebugLogger.Log(LOG_TAG,
                    $"{gameObject.name}: 스태미나 고갈!", this);
            }

            return true;
        }

        private void Regenerate()
        {
            float interval = _statHandler.GetStat(StatType.StaminaRegenInterval);
            _regenTickTimer += Time.deltaTime;

            if (_regenTickTimer < interval) return;

            _regenTickTimer -= interval;

            float rate = _statHandler.GetStat(StatType.StaminaRegenRate);
            float maxStamina = MaxStamina;

            _currentStamina = Mathf.Min(_currentStamina + rate, maxStamina);

            OnStaminaChanged?.Invoke(_currentStamina, maxStamina);

            if (_isExhausted && _currentStamina > 0f)
            {
                _isExhausted = false;
                OnExhaustedChanged?.Invoke(false);

                DebugLogger.Log(LOG_TAG,
                    $"{gameObject.name}: 스태미나 고갈 해제", this);
            }
        }
    }
}
