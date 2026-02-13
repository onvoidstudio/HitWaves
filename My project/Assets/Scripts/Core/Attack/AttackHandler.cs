using UnityEngine;

namespace HitWaves.Core.Attack
{
    [RequireComponent(typeof(StatHandler))]
    public class AttackHandler : MonoBehaviour
    {
        private const string LOG_TAG = "AttackHandler";
        private const int HIT_BUFFER_SIZE = 32;

        [Header("Behavior")]
        [SerializeField] private EntityAttackBehaviorType _behaviorType;

        [Header("Attack Settings")]
        [SerializeField] private float _range = 1f;
        [SerializeField] private float _swingAngle = 90f;
        [SerializeField] private LayerMask _targetLayer;

        private StatHandler _statHandler;
        private IAttackBehavior _behavior;
        private float _cooldownTimer;

        private Collider2D[] _hitBuffer;
        private float[] _hitDistances;

        public StatHandler StatHandler => _statHandler;
        public float Range => _range;
        public float SwingAngle => _swingAngle;
        public LayerMask TargetLayer => _targetLayer;
        public Collider2D[] HitBuffer => _hitBuffer;
        public float[] HitDistances => _hitDistances;

        private void Awake()
        {
            _statHandler = GetComponent<StatHandler>();

            if (_statHandler == null)
            {
                DebugLogger.LogError(LOG_TAG, "StatHandler 컴포넌트를 찾을 수 없음", this);
                return;
            }

            _hitBuffer = new Collider2D[HIT_BUFFER_SIZE];
            _hitDistances = new float[HIT_BUFFER_SIZE];

            SetBehavior(_behaviorType);
            DebugLogger.Log(LOG_TAG, $"초기화 완료 - 행동: {_behaviorType}", this);
        }

        private void Update()
        {
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.deltaTime;
            }
        }

        private void OnDestroy()
        {
            _behavior?.Cleanup();
        }

        public bool CanAttack()
        {
            return _cooldownTimer <= 0f;
        }

        public void Attack(Vector2 direction)
        {
            if (!CanAttack()) return;
            if (_behavior == null) return;

            float damage = _statHandler.GetStat(StatType.Damage);
            float strength = _statHandler.GetStat(StatType.Strength);
            int maxHitCount = Mathf.Max(1, (int)_statHandler.GetStat(StatType.MaxHitCount));

            int hitCount = _behavior.Execute(this, direction);

            if (_behavior.CooldownOnExecute)
            {
                float attackSpeed = _statHandler.GetStat(StatType.AttackSpeed);
                _cooldownTimer = attackSpeed > 0f ? 1f / attackSpeed : 1f;
            }

            DebugLogger.Log(LOG_TAG, $"공격 실행 - 행동: {_behaviorType}, 방향: {direction}, 데미지: {damage}, 히트 수: {hitCount}/{maxHitCount}", this);
        }

        public void SetBehavior(EntityAttackBehaviorType type)
        {
            _behavior?.Cleanup();

            _behaviorType = type;
            _behavior = CreateBehavior(type);
            _behavior?.Initialize(this);

            DebugLogger.Log(LOG_TAG, $"행동 변경: {type}", this);
        }

        private IAttackBehavior CreateBehavior(EntityAttackBehaviorType type)
        {
            return type switch
            {
                EntityAttackBehaviorType.Strike => new Behaviors.StrikeBehavior(),
                EntityAttackBehaviorType.Contact => new Behaviors.ContactBehavior(),
                _ => null
            };
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _range);
        }
#endif
    }
}
