using UnityEngine;
using HitWaves.Core.Attack;

namespace HitWaves.Core.AI
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(StatHandler))]
    [RequireComponent(typeof(AttackHandler))]
    public class EntityAI : MonoBehaviour
    {
        private const string LOG_TAG = "EntityAI";

        [Header("AI Settings")]
        [SerializeField] private EntityAIState _initialState = EntityAIState.Wander;

        [Header("Wander")]
        [SerializeField] private float _wanderDirectionInterval = 2f;

        private Rigidbody2D _rigidbody;
        private StatHandler _statHandler;
        private AttackHandler _attackHandler;

        private EntityAIState _currentState;
        private Vector2 _wanderDirection;
        private float _wanderTimer;

#if UNITY_EDITOR
        [Header("Debug (Read Only)")]
        [SerializeField] private EntityAIState _debugCurrentState;
#endif

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _statHandler = GetComponent<StatHandler>();
            _attackHandler = GetComponent<AttackHandler>();

            if (_rigidbody == null)
            {
                DebugLogger.LogError(LOG_TAG, "Rigidbody2D 컴포넌트를 찾을 수 없음", this);
                return;
            }

            if (_statHandler == null)
            {
                DebugLogger.LogError(LOG_TAG, "StatHandler 컴포넌트를 찾을 수 없음", this);
                return;
            }

            if (_attackHandler == null)
            {
                DebugLogger.LogError(LOG_TAG, "AttackHandler 컴포넌트를 찾을 수 없음", this);
                return;
            }

            _rigidbody.gravityScale = 0f;
            _rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;

            _currentState = _initialState;
            PickRandomWanderDirection();

            DebugLogger.Log(LOG_TAG, $"초기화 완료 - 상태: {_currentState}", this);
        }

        private void Update()
        {
            switch (_currentState)
            {
                case EntityAIState.Idle:
                    UpdateIdle();
                    break;
                case EntityAIState.Wander:
                    UpdateWander();
                    break;
            }

#if UNITY_EDITOR
            _debugCurrentState = _currentState;
#endif
        }

        private void FixedUpdate()
        {
            switch (_currentState)
            {
                case EntityAIState.Idle:
                    _rigidbody.linearVelocity = Vector2.zero;
                    break;
                case EntityAIState.Wander:
                    FixedUpdateWander();
                    break;
            }
        }

        private void UpdateIdle()
        {
        }

        private void UpdateWander()
        {
            _wanderTimer -= Time.deltaTime;

            if (_wanderTimer <= 0f)
            {
                PickRandomWanderDirection();
            }

            if (_attackHandler.CanAttack())
            {
                _attackHandler.Attack(_wanderDirection);
            }
        }

        private void FixedUpdateWander()
        {
            float moveSpeed = _statHandler.GetStat(StatType.MoveSpeed);
            _rigidbody.linearVelocity = _wanderDirection * moveSpeed;
        }

        private void PickRandomWanderDirection()
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            _wanderDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            _wanderTimer = _wanderDirectionInterval;
        }

        public void SetState(EntityAIState newState)
        {
            if (_currentState == newState) return;

            DebugLogger.Log(LOG_TAG, $"상태 변경: {_currentState} → {newState}", this);
            _currentState = newState;
        }

        public EntityAIState GetCurrentState()
        {
            return _currentState;
        }
    }
}
