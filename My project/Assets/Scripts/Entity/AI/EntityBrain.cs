using UnityEngine;
using HitWaves.Core;
using HitWaves.Entity.AI.MoveBehaviors;
using HitWaves.Entity.AI.Boss;

namespace HitWaves.Entity.AI
{
    /// <summary>
    /// 엔티티 행동 관리자.
    /// 부착된 IMoveBehavior 모듈 중 활성화된 것을 실행하여 이동을 처리.
    /// 사망 시 자동으로 이동 중단 + Destroy.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(StatHandler))]
    public class EntityBrain : MonoBehaviour
    {
        private const string LOG_TAG = "EntityBrain";

        private Rigidbody2D _rigidbody;
        private StatHandler _statHandler;
        private HealthHandler _healthHandler;
        private IMoveBehavior[] _moveBehaviors;
        private ChargeBehavior _chargeBehavior;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _statHandler = GetComponent<StatHandler>();
            _healthHandler = GetComponent<HealthHandler>();

            // Rigidbody2D 기본 설정
            _rigidbody.gravityScale = 0f;
            _rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
            _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;

            // 이동 모듈 수집
            _moveBehaviors = GetComponents<IMoveBehavior>();
            _chargeBehavior = GetComponent<ChargeBehavior>();

            if (_healthHandler != null)
            {
                _healthHandler.OnDeath += HandleDeath;
            }

            if (_moveBehaviors.Length == 0)
            {
                DebugLogger.LogWarning(LOG_TAG,
                    $"{gameObject.name}: 이동 모듈(IMoveBehavior)이 없음! " +
                    $"DriftBehavior 또는 ChaseBehavior를 추가하세요.", this);
            }
            else
            {
                DebugLogger.Log(LOG_TAG,
                    $"{gameObject.name}: EntityBrain 초기화 — " +
                    $"이동 모듈: {_moveBehaviors.Length}개", this);
            }
        }

        private void FixedUpdate()
        {
            if (_healthHandler != null && _healthHandler.IsDead) return;

            // 보스 돌진 중이면 ChargeBehavior가 velocity를 직접 제어
            if (_chargeBehavior != null && _chargeBehavior.IsActive) return;

            Vector2 velocity = Vector2.zero;

            // 활성화된 이동 모듈의 속도를 합산
            for (int i = 0; i < _moveBehaviors.Length; i++)
            {
                if (_moveBehaviors[i].IsActive)
                {
                    velocity += _moveBehaviors[i].GetDesiredVelocity(_rigidbody, _statHandler);
                }
            }

            _rigidbody.linearVelocity = velocity;
        }

        /// <summary>
        /// 추적 대상 설정. ChaseBehavior, DriftBehavior 등 타겟이 필요한 모듈에 전달.
        /// EnemySpawner 등 외부에서 호출.
        /// </summary>
        public void SetTarget(Transform target)
        {
            ChaseBehavior chase = GetComponent<ChaseBehavior>();
            if (chase != null)
            {
                chase.SetTarget(target);
            }

            DriftBehavior drift = GetComponent<DriftBehavior>();
            if (drift != null)
            {
                drift.SetTarget(target);
            }

            KingDullFlyBrain bossBrain = GetComponent<KingDullFlyBrain>();
            if (bossBrain != null)
            {
                bossBrain.SetTarget(target);
            }
        }

        private void HandleDeath()
        {
            _rigidbody.linearVelocity = Vector2.zero;

            DebugLogger.Log(LOG_TAG,
                $"{gameObject.name}: 사망 처리 — Destroy", this);

            Destroy(gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // DriftBehavior가 있으면 벽 반사 처리
            DriftBehavior drift = GetComponent<DriftBehavior>();
            if (drift != null && drift.IsActive && collision.contactCount > 0)
            {
                Vector2 normal = collision.contacts[0].normal;
                drift.Reflect(normal);
            }
        }

        private void OnDestroy()
        {
            if (_healthHandler != null)
            {
                _healthHandler.OnDeath -= HandleDeath;
            }
        }
    }
}
