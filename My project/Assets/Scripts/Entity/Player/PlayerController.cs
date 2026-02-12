using UnityEngine;
using UnityEngine.InputSystem;
using HitWaves.Core;
using HitWaves.Core.Attack;

namespace HitWaves.Entity.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(StatHandler))]
    [RequireComponent(typeof(AttackHandler))]
    public class PlayerController : MonoBehaviour
    {
        private const string LOG_TAG = "PlayerController";

        [Header("Input")]
        [SerializeField] private InputActionReference _moveAction;
        [SerializeField] private InputActionReference _attackAction;

        private Rigidbody2D _rigidbody;
        private StatHandler _statHandler;
        private AttackHandler _attackHandler;
        private Vector2 _moveInput;

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

            _rigidbody.gravityScale = 0f;
            _rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;

            DebugLogger.Log(LOG_TAG, "초기화 완료", this);
        }

        private void OnEnable()
        {
            if (_moveAction != null && _moveAction.action != null)
            {
                _moveAction.action.Enable();
            }
            else
            {
                DebugLogger.LogWarning(LOG_TAG, "Move 액션이 할당되지 않음", this);
            }

            if (_attackAction != null && _attackAction.action != null)
            {
                _attackAction.action.Enable();
            }
            else
            {
                DebugLogger.LogWarning(LOG_TAG, "Attack 액션이 할당되지 않음", this);
            }
        }

        private void OnDisable()
        {
            if (_moveAction != null && _moveAction.action != null)
            {
                _moveAction.action.Disable();
            }

            if (_attackAction != null && _attackAction.action != null)
            {
                _attackAction.action.Disable();
            }
        }

        private void Update()
        {
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
            if (_attackHandler == null) return;

            Vector2 attackInput = _attackAction.action.ReadValue<Vector2>();

            if (attackInput.sqrMagnitude > 0.01f)
            {
                _attackHandler.Attack(attackInput.normalized);
            }
        }

        private void Move()
        {
            if (_statHandler == null) return;

            float moveSpeed = _statHandler.GetStat(StatType.MoveSpeed);
            Vector2 velocity = _moveInput * moveSpeed;
            _rigidbody.linearVelocity = velocity;
        }
    }
}
