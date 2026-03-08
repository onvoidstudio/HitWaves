using UnityEngine;
using UnityEngine.InputSystem;
using HitWaves.Core;

namespace HitWaves.Entity.Player
{
    public class PlayerController : EntityController
    {
        private const string LOG_TAG = "PlayerController";

        [Header("Input")]
        [Tooltip("Player > Move 액션 참조 (WASD / 좌스틱)")]
        [SerializeField] private InputActionReference _moveAction;

        [Header("Movement Feel")]
        [Tooltip("가속 계수 — 높을수록 빠르게 최고 속도 도달")]
        [Min(0f)]
        [SerializeField] private float _acceleration = 20f;

        [Tooltip("감속 계수 — 높을수록 빠르게 정지 (낮으면 미끄러짐)")]
        [Min(0f)]
        [SerializeField] private float _deceleration = 10f;

        private Vector2 _moveInput;

        protected override void Awake()
        {
            base.Awake();

            DebugLogger.Log(LOG_TAG,
                $"초기화 완료 — MoveSpeed: {_statHandler.GetStat(StatType.MoveSpeed)}, " +
                $"Accel: {_acceleration}, Decel: {_deceleration}", this);
        }

        private void OnEnable()
        {
            if (_moveAction != null && _moveAction.action != null)
            {
                _moveAction.action.Enable();
                DebugLogger.Log(LOG_TAG, "Move 액션 활성화", this);
            }
            else
            {
                DebugLogger.LogWarning(LOG_TAG, "Move 액션이 할당되지 않음", this);
            }
        }

        private void OnDisable()
        {
            if (_moveAction != null && _moveAction.action != null)
            {
                _moveAction.action.Disable();
                DebugLogger.Log(LOG_TAG, "Move 액션 비활성화", this);
            }
        }

        private void Update()
        {
            ReadMoveInput();
        }

        private void FixedUpdate()
        {
            Move();
        }

        private void ReadMoveInput()
        {
            if (_moveAction == null || _moveAction.action == null) return;

            _moveInput = _moveAction.action.ReadValue<Vector2>();
        }

        private void Move()
        {
            float moveSpeed = _statHandler.GetStat(StatType.MoveSpeed);
            Vector2 targetVelocity = _moveInput * moveSpeed;

            bool isAccelerating = _moveInput.sqrMagnitude > 0.01f;
            float lerpRate = isAccelerating ? _acceleration : _deceleration;

            _rigidbody.linearVelocity = Vector2.Lerp(
                _rigidbody.linearVelocity, targetVelocity, lerpRate * Time.fixedDeltaTime);
        }
    }
}
