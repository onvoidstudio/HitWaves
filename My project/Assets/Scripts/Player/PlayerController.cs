using UnityEngine;
using UnityEngine.InputSystem;
using HitWaves.Core;
using HitWaves.Core.Attack;

namespace HitWaves.Entity.Player
{
    public class PlayerController : EntityController
    {
        private const string LOG_TAG = "PlayerController";

        [Header("Input")]
        [Tooltip("Player > Move 액션 참조 (WASD / 좌스틱)")]
        [SerializeField] private InputActionReference _moveAction;

        [Tooltip("Player > Attack 액션 참조 (방향키 / 우스틱)")]
        [SerializeField] private InputActionReference _attackAction;

        [Tooltip("Player > SwapHand 액션 참조 (F키)")]
        [SerializeField] private InputActionReference _swapHandAction;

        [Header("Movement Feel")]
        [Tooltip("가속 계수 — 높을수록 빠르게 최고 속도 도달")]
        [Min(0f)]
        [SerializeField] private float _acceleration = 20f;

        [Tooltip("감속 계수 — 높을수록 빠르게 정지 (낮으면 미끄러짐)")]
        [Min(0f)]
        [SerializeField] private float _deceleration = 10f;

        private Vector2 _moveInput;
        private Vector2 _attackInput;
        private AttackHandler _attackHandler;
        private Inventory _inventory;
        private bool _inputEnabled = true;

        protected override void Awake()
        {
            base.Awake();

            _attackHandler = GetComponent<AttackHandler>();
            _inventory = GetComponent<Inventory>();

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

            if (_attackAction != null && _attackAction.action != null)
            {
                _attackAction.action.Enable();
                DebugLogger.Log(LOG_TAG, "Attack 액션 활성화", this);
            }
            else
            {
                DebugLogger.LogWarning(LOG_TAG, "Attack 액션이 할당되지 않음", this);
            }

            if (_swapHandAction != null && _swapHandAction.action != null)
            {
                _swapHandAction.action.Enable();
                _swapHandAction.action.performed += OnSwapHandPerformed;
            }
        }

        private void OnDisable()
        {
            if (_moveAction != null && _moveAction.action != null)
            {
                _moveAction.action.Disable();
                DebugLogger.Log(LOG_TAG, "Move 액션 비활성화", this);
            }

            if (_attackAction != null && _attackAction.action != null)
            {
                _attackAction.action.Disable();
                DebugLogger.Log(LOG_TAG, "Attack 액션 비활성화", this);
            }

            if (_swapHandAction != null && _swapHandAction.action != null)
            {
                _swapHandAction.action.performed -= OnSwapHandPerformed;
            }
        }

        /// <summary>
        /// 입력 활성/비활성. 인트로 연출 등에서 사용.
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;

            if (!enabled)
            {
                _moveInput = Vector2.zero;
                _attackInput = Vector2.zero;
            }
        }

        private void OnSwapHandPerformed(InputAction.CallbackContext ctx)
        {
            if (!_inputEnabled) return;
            if (_inventory == null) return;

            _inventory.SwapSlots(0, 1);
            DebugLogger.Log(LOG_TAG, "양손 아이템 교환 (F키)", this);
        }

        private void Update()
        {
            if (!_inputEnabled) return;

            ReadMoveInput();
            ReadAttackInput();
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

        private void ReadAttackInput()
        {
            if (_attackAction == null || _attackAction.action == null) return;

            _attackInput = _attackAction.action.ReadValue<Vector2>();

            if (_attackInput.sqrMagnitude > 0.01f && _attackHandler != null)
            {
                _attackHandler.Attack(_attackInput);
            }
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
