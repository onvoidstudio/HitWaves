using UnityEngine;
using UnityEngine.InputSystem;
using HitWaves.Core;

namespace HitWaves.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(StatHandler))]
    public class PlayerController : MonoBehaviour
    {
        private const string LOG_TAG = "PlayerController";

        [Header("Input")]
        [SerializeField] private InputActionReference _moveAction;

        private Rigidbody2D _rigidbody;
        private StatHandler _statHandler;
        private Vector2 _moveInput;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _statHandler = GetComponent<StatHandler>();

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
            ReadInput();
        }

        private void FixedUpdate()
        {
            Move();
        }

        private void ReadInput()
        {
            if (_moveAction == null || _moveAction.action == null) return;

            Vector2 newInput = _moveAction.action.ReadValue<Vector2>();

            if (newInput != _moveInput)
            {
                _moveInput = newInput;
                DebugLogger.Log(LOG_TAG, $"이동 입력: {_moveInput}", this);
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
