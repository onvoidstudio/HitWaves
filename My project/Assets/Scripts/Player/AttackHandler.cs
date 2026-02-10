using UnityEngine;
using UnityEngine.InputSystem;
using HitWaves.Core;

namespace HitWaves.Player
{
    [RequireComponent(typeof(StatHandler))]
    public class AttackHandler : MonoBehaviour
    {
        private const string LOG_TAG = "AttackHandler";

        [Header("Input")]
        [SerializeField] private InputActionReference _attackAction;

        [Header("Attack Settings")]
        [SerializeField] private float _range = 1f;
        [SerializeField] private LayerMask _targetLayer;

        private StatHandler _statHandler;
        private float _cooldownTimer;
        private Vector2 _attackInput;

        private void Awake()
        {
            _statHandler = GetComponent<StatHandler>();

            if (_statHandler == null)
            {
                DebugLogger.LogError(LOG_TAG, "StatHandler 컴포넌트를 찾을 수 없음", this);
                return;
            }

            DebugLogger.Log(LOG_TAG, "초기화 완료", this);
        }

        private void OnEnable()
        {
            if (_attackAction != null && _attackAction.action != null)
            {
                _attackAction.action.Enable();
                DebugLogger.Log(LOG_TAG, "Attack 액션 활성화", this);
            }
            else
            {
                DebugLogger.LogWarning(LOG_TAG, "Attack 액션이 할당되지 않음", this);
            }
        }

        private void OnDisable()
        {
            if (_attackAction != null && _attackAction.action != null)
            {
                _attackAction.action.Disable();
                DebugLogger.Log(LOG_TAG, "Attack 액션 비활성화", this);
            }
        }

        private void Update()
        {
            UpdateCooldown();
            ReadInput();
        }

        private void UpdateCooldown()
        {
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.deltaTime;
            }
        }

        private void ReadInput()
        {
            if (_attackAction == null || _attackAction.action == null) return;

            _attackInput = _attackAction.action.ReadValue<Vector2>();

            if (_attackInput.sqrMagnitude > 0.01f)
            {
                TryAttack(_attackInput.normalized);
            }
        }

        private bool CanAttack()
        {
            return _cooldownTimer <= 0f;
        }

        private void TryAttack(Vector2 direction)
        {
            if (!CanAttack()) return;

            ExecuteAttack(direction);

            float attackSpeed = _statHandler.GetStat(StatType.AttackSpeed);
            _cooldownTimer = attackSpeed > 0f ? 1f / attackSpeed : 1f;
        }

        private void ExecuteAttack(Vector2 direction)
        {
            float damage = _statHandler.GetStat(StatType.Damage);
            float strength = _statHandler.GetStat(StatType.Strength);

            Vector2 attackPosition = (Vector2)transform.position + direction * _range;

            Collider2D[] hits = Physics2D.OverlapCircleAll(attackPosition, _range * 0.5f, _targetLayer);

            DebugLogger.Log(LOG_TAG, $"공격 실행 - 방향: {direction}, 데미지: {damage}, 힘: {strength}, 히트 수: {hits.Length}", this);

            foreach (Collider2D hit in hits)
            {
                DebugLogger.Log(LOG_TAG, $"히트: {hit.gameObject.name}", this);
                // TODO: 데미지 처리 (HealthHandler 구현 후)
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_attackInput.sqrMagnitude > 0.01f)
            {
                Vector2 attackPosition = (Vector2)transform.position + _attackInput.normalized * _range;
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(attackPosition, _range * 0.5f);
            }
            else
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, _range);
            }
        }
    }
}
