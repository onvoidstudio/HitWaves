using UnityEngine;
using HitWaves.Core.Attack.Behaviors;

namespace HitWaves.Core.Attack
{
    [RequireComponent(typeof(StatHandler))]
    public class AttackHandler : MonoBehaviour
    {
        private const string LOG_TAG = "AttackHandler";

        [Header("기본 근접 공격 설정")]
        [Tooltip("공격 사거리 (유닛)")]
        [Min(0.1f)]
        [SerializeField] private float _range = 1f;

        [Tooltip("공격 부채꼴 각도 (도)")]
        [Range(10f, 360f)]
        [SerializeField] private float _swingAngle = 60f;

        [Tooltip("딜 계수 (Damage 스탯의 배율)")]
        [Min(0.01f)]
        [SerializeField] private float _damageCoefficient = 0.3f;

        [Tooltip("1회 공격 최대 타격 수")]
        [Min(1)]
        [SerializeField] private int _maxHitCount = 1;

        [Tooltip("1회 공격당 스태미나 소모")]
        [Min(0f)]
        [SerializeField] private float _staminaCost = 15f;

        [Tooltip("타격 대상 레이어")]
        [SerializeField] private LayerMask _targetLayer;

        [Header("이펙트")]
        [Tooltip("타격 이펙트 표시 시간 (초)")]
        [Min(0.01f)]
        [SerializeField] private float _effectDuration = 0.1f;

        private StatHandler _statHandler;
        private StaminaHandler _staminaHandler;
        private IAttackBehavior _behavior;
        private float _cooldownTimer;

        // StrikeBehavior에서 참조하는 프로퍼티
        public float Range => _range;
        public float SwingAngle => _swingAngle;
        public float DamageCoefficient => _damageCoefficient;
        public int MaxHitCount => _maxHitCount;
        public LayerMask TargetLayer => _targetLayer;
        public float EffectDuration => _effectDuration;

        private void Awake()
        {
            _statHandler = GetComponent<StatHandler>();
            _staminaHandler = GetComponent<StaminaHandler>();

            // 기본 공격 = StrikeBehavior
            _behavior = new StrikeBehavior();
            _behavior.Initialize(this);

            DebugLogger.Log(LOG_TAG,
                $"{gameObject.name}: AttackHandler 초기화 — " +
                $"range:{_range}, angle:{_swingAngle}, coeff:{_damageCoefficient}, " +
                $"staminaCost:{_staminaCost}", this);
        }

        private void Update()
        {
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.deltaTime;
            }
        }

        /// <summary>
        /// 공격 가능 여부. 쿨다운 + 스태미나 체크.
        /// </summary>
        public bool CanAttack()
        {
            if (_cooldownTimer > 0f) return false;
            if (_staminaHandler != null && _staminaHandler.IsExhausted) return false;
            if (_staminaHandler != null && _staminaHandler.CurrentStamina < _staminaCost) return false;
            return true;
        }

        /// <summary>
        /// 공격 실행. 방향키 방향을 받아 공격한다.
        /// </summary>
        public void Attack(Vector2 direction)
        {
            if (!CanAttack()) return;
            if (_behavior == null) return;

            // 스태미나 소모
            if (_staminaHandler != null)
            {
                if (!_staminaHandler.Consume(_staminaCost)) return;
            }

            int hitCount = _behavior.Execute(this, direction);

            if (_behavior.CooldownOnExecute)
            {
                float attackSpeed = _statHandler.GetStat(StatType.AttackSpeed);
                _cooldownTimer = attackSpeed > 0f ? 1f / attackSpeed : 1f;
            }

            DebugLogger.Log(LOG_TAG,
                $"{gameObject.name}: Attack — dir:{direction}, hits:{hitCount}, " +
                $"cooldown:{_cooldownTimer:F2}s", this);
        }

        private void OnDestroy()
        {
            _behavior?.Cleanup();
        }
    }
}
