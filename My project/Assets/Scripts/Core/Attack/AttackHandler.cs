using UnityEngine;
using HitWaves.Core.Attack.Behaviors;
using HitWaves.Core.Item;
using HitWaves.Entity.Player;

namespace HitWaves.Core.Attack
{
    [RequireComponent(typeof(StatHandler))]
    public class AttackHandler : MonoBehaviour
    {
        private const string LOG_TAG = "AttackHandler";

        [Header("기본 근접 공격 설정 (무기 미장착 시)")]
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
        private Inventory _inventory;

        private IAttackBehavior _currentBehavior;
        private StrikeBehavior _strikeBehavior;
        private ShootBehavior _shootBehavior;

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
            _inventory = GetComponent<Inventory>();

            // 두 가지 공격 방식 초기화
            _strikeBehavior = new StrikeBehavior();
            _strikeBehavior.Initialize(this);

            _shootBehavior = new ShootBehavior();
            _shootBehavior.Initialize(this);

            // 기본은 근접
            _currentBehavior = _strikeBehavior;

            DebugLogger.Log(LOG_TAG,
                $"{gameObject.name}: AttackHandler 초기화 — " +
                $"range:{_range}, angle:{_swingAngle}, coeff:{_damageCoefficient}, " +
                $"staminaCost:{_staminaCost}", this);
        }

        private void OnEnable()
        {
            if (_inventory != null)
            {
                _inventory.OnSlotChanged += HandleSlotChanged;
                _inventory.OnActiveHandChanged += HandleActiveHandChanged;
            }
        }

        private void OnDisable()
        {
            if (_inventory != null)
            {
                _inventory.OnSlotChanged -= HandleSlotChanged;
                _inventory.OnActiveHandChanged -= HandleActiveHandChanged;
            }
        }

        private void Start()
        {
            // 시작 무기 반영
            UpdateBehavior();
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
            if (_currentBehavior == null) return;

            // 스태미나 소모
            if (_staminaHandler != null)
            {
                if (!_staminaHandler.Consume(_staminaCost)) return;
            }

            int hitCount = _currentBehavior.Execute(this, direction);

            if (_currentBehavior.CooldownOnExecute)
            {
                float cooldown = GetCooldownTime();
                _cooldownTimer = cooldown;
            }

            DebugLogger.Log(LOG_TAG,
                $"{gameObject.name}: Attack — dir:{direction}, hits:{hitCount}, " +
                $"cooldown:{_cooldownTimer:F2}s", this);
        }

        /// <summary>
        /// 쿨다운 시간을 계산한다.
        /// 무기 장착 시 무기의 FireRate, 미장착 시 본체 AttackSpeed.
        /// </summary>
        private float GetCooldownTime()
        {
            WeaponData weapon = GetActiveWeaponData();
            if (weapon != null)
            {
                return weapon.FireRate > 0f ? 1f / weapon.FireRate : 1f;
            }

            float attackSpeed = _statHandler.GetStat(StatType.AttackSpeed);
            return attackSpeed > 0f ? 1f / attackSpeed : 1f;
        }

        /// <summary>
        /// 활성 손의 무기 데이터를 반환한다. 무기가 아니면 null.
        /// </summary>
        private WeaponData GetActiveWeaponData()
        {
            if (_inventory == null || _inventory.ActiveHand == null) return null;

            ItemInstance item = _inventory.ActiveHand.EquippedItem;
            if (item == null || item.Data == null) return null;

            return item.Data as WeaponData;
        }

        /// <summary>
        /// 활성 무기에 따라 공격 방식을 전환한다.
        /// </summary>
        private void UpdateBehavior()
        {
            WeaponData weapon = GetActiveWeaponData();

            if (weapon != null && weapon.IsRanged)
            {
                _shootBehavior.SetWeaponData(weapon);
                _currentBehavior = _shootBehavior;
                DebugLogger.Log(LOG_TAG,
                    $"공격 방식 전환 — 원거리 ({weapon.ItemName})", this);
            }
            else
            {
                _currentBehavior = _strikeBehavior;
                DebugLogger.Log(LOG_TAG,
                    "공격 방식 전환 — 근접", this);
            }
        }

        private void HandleSlotChanged(EquipmentSlot slot)
        {
            UpdateBehavior();
        }

        private void HandleActiveHandChanged(int index)
        {
            UpdateBehavior();
        }

        private void OnDestroy()
        {
            _currentBehavior?.Cleanup();
        }
    }
}
