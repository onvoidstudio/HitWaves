using UnityEngine;
using HitWaves.Core;
using HitWaves.Entity.AI.MoveBehaviors;

namespace HitWaves.Entity.AI.Boss
{
    /// <summary>
    /// King Dull Fly 보스 AI.
    /// Idle(부유) → 스태미나/공격속도 기반 쿨타임 후 → 소환 or 돌진 랜덤 선택.
    /// 정면에 플레이어가 없으면 돌진 대신 소환으로 대체.
    /// </summary>
    [RequireComponent(typeof(StatHandler))]
    [RequireComponent(typeof(StaminaHandler))]
    public class KingDullFlyBrain : MonoBehaviour
    {
        private const string LOG_TAG = "KingDullFlyBrain";

        private enum BossState
        {
            Idle,
            Attacking,
            Cooldown
        }

        [Header("행동 설정")]
        [Tooltip("스폰 후 첫 행동까지 대기 시간 (초)")]
        [Min(0f)]
        [SerializeField] private float _initialDelay = 1f;

        [Tooltip("공격 후 다음 행동까지 지연 시간 (초)")]
        [Min(0.1f)]
        [SerializeField] private float _actionDelay = 1f;

        [Tooltip("스태미나 소모량 (행동 1회)")]
        [Min(1f)]
        [SerializeField] private float _staminaCost = 20f;

        [Tooltip("돌진 선택 확률 (0~1, 나머지는 소환)")]
        [Range(0f, 1f)]
        [SerializeField] private float _chargeChance = 0.5f;

        [Header("참조")]
        [Tooltip("소환 행동 모듈")]
        [SerializeField] private SummonBehavior _summonBehavior;

        [Tooltip("돌진 행동 모듈")]
        [SerializeField] private ChargeBehavior _chargeBehavior;

        private StatHandler _statHandler;
        private StaminaHandler _staminaHandler;
        private HealthHandler _healthHandler;
        private DriftBehavior _driftBehavior;
        private Transform _target;

        private BossState _state = BossState.Idle;
        private float _cooldownTimer;
        private float _attackCooldown;
        private float _initialTimer;

        private void Awake()
        {
            _statHandler = GetComponent<StatHandler>();
            _staminaHandler = GetComponent<StaminaHandler>();
            _healthHandler = GetComponent<HealthHandler>();
            _driftBehavior = GetComponent<DriftBehavior>();
        }

        private void Start()
        {
            if (_target != null) return;

            StatHandler[] allStats = FindObjectsByType<StatHandler>(FindObjectsSortMode.None);
            for (int i = 0; i < allStats.Length; i++)
            {
                if (allStats[i].Faction == Faction.Player)
                {
                    SetTarget(allStats[i].transform);
                    DebugLogger.Log(LOG_TAG,
                        $"Player 팩션 자동 감지 — 타겟: {allStats[i].gameObject.name}", this);
                    break;
                }
            }

            if (_target == null)
            {
                DebugLogger.LogWarning(LOG_TAG,
                    "Player 팩션 엔티티를 찾지 못함", this);
            }
        }

        private void OnEnable()
        {
            if (_chargeBehavior != null)
            {
                _chargeBehavior.OnChargeFinished += HandleActionFinished;
            }

            if (_summonBehavior != null)
            {
                _summonBehavior.OnSummonFinished += HandleActionFinished;
            }
        }

        private void OnDisable()
        {
            if (_chargeBehavior != null)
            {
                _chargeBehavior.OnChargeFinished -= HandleActionFinished;
            }

            if (_summonBehavior != null)
            {
                _summonBehavior.OnSummonFinished -= HandleActionFinished;
            }
        }

        /// <summary>
        /// 추적 대상 설정. EnemySpawner에서 호출.
        /// </summary>
        public void SetTarget(Transform target)
        {
            _target = target;

            if (_driftBehavior != null)
            {
                _driftBehavior.SetTarget(target);
            }

            if (_chargeBehavior != null)
            {
                _chargeBehavior.SetTarget(target);
            }

            if (_summonBehavior != null)
            {
                _summonBehavior.SetTarget(target);
            }
        }

        private void Update()
        {
            if (_healthHandler != null && _healthHandler.IsDead) return;
            if (_target == null) return;

            switch (_state)
            {
                case BossState.Idle:
                    UpdateIdle();
                    break;
                case BossState.Cooldown:
                    UpdateCooldown();
                    break;
            }

            UpdateFacing();
        }

        private void UpdateIdle()
        {
            // 스폰 후 초기 대기
            if (_initialTimer < _initialDelay)
            {
                _initialTimer += Time.deltaTime;
                return;
            }

            // 공격속도 기반 쿨타임 계산
            float attackSpeed = _statHandler.GetStat(StatType.AttackSpeed);
            _attackCooldown = attackSpeed > 0f ? 1f / attackSpeed : 999f;

            _cooldownTimer += Time.deltaTime;

            if (_cooldownTimer < _attackCooldown) return;
            if (_staminaHandler.IsExhausted) return;
            if (!_staminaHandler.Consume(_staminaCost)) return;

            _cooldownTimer = 0f;
            SelectAction();
        }

        private void UpdateCooldown()
        {
            _cooldownTimer += Time.deltaTime;

            if (_cooldownTimer >= _actionDelay)
            {
                _cooldownTimer = 0f;
                _state = BossState.Idle;

                if (_driftBehavior != null)
                {
                    _driftBehavior.IsActive = true;
                }

                DebugLogger.Log(LOG_TAG, "쿨다운 종료 → Idle", this);
            }
        }

        private void SelectAction()
        {
            _state = BossState.Attacking;

            if (_driftBehavior != null)
            {
                _driftBehavior.IsActive = false;
            }

            if (_chargeBehavior != null && Random.value < _chargeChance)
            {
                DebugLogger.Log(LOG_TAG, "패턴 선택: 돌진", this);
                _chargeBehavior.Execute();
            }
            else if (_summonBehavior != null)
            {
                DebugLogger.Log(LOG_TAG, "패턴 선택: 소환", this);
                _summonBehavior.Execute();
            }
            else
            {
                HandleActionFinished();
            }
        }

        private void HandleActionFinished()
        {
            _state = BossState.Cooldown;
            _cooldownTimer = 0f;

            // 쿨다운 중에도 부유 이동 활성화
            if (_driftBehavior != null)
            {
                _driftBehavior.IsActive = true;
            }

            DebugLogger.Log(LOG_TAG, "행동 완료 → 쿨다운", this);
        }

        /// <summary>
        /// 플레이어 방향에 따라 flipX 적용.
        /// </summary>
        private void UpdateFacing()
        {
            if (_target == null) return;

            float dirX = _target.position.x - transform.position.x;
            Vector3 scale = transform.localScale;

            if (dirX < 0f && scale.x > 0f)
            {
                scale.x = -Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
            else if (dirX > 0f && scale.x < 0f)
            {
                scale.x = Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
        }
    }
}
