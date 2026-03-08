using UnityEngine;
using HitWaves.Core;

namespace HitWaves.Entity.Enemy
{
    public class EnemyController : EntityController
    {
        private const string LOG_TAG = "EnemyController";

        private Transform _target;
        private HealthHandler _healthHandler;

        protected override void Awake()
        {
            base.Awake();

            _healthHandler = GetComponent<HealthHandler>();
            if (_healthHandler != null)
            {
                _healthHandler.OnDeath += HandleDeath;
            }
        }

        /// <summary>
        /// 추적 대상(플레이어)을 설정한다. EnemySpawner에서 호출.
        /// </summary>
        public void Initialize(Transform target)
        {
            _target = target;

            DebugLogger.Log(LOG_TAG,
                $"{gameObject.name}: Initialize — target: {(target != null ? target.name : "null")}", this);
        }

        private void FixedUpdate()
        {
            Chase();
        }

        private void Chase()
        {
            if (_target == null) return;
            if (_healthHandler != null && _healthHandler.IsDead) return;

            Vector2 direction = ((Vector2)_target.position - _rigidbody.position).normalized;
            float speed = _statHandler.GetStat(StatType.MoveSpeed);

            _rigidbody.linearVelocity = direction * speed;
        }

        private void HandleDeath()
        {
            _rigidbody.linearVelocity = Vector2.zero;

            DebugLogger.Log(LOG_TAG,
                $"{gameObject.name}: 사망 처리 — Destroy", this);

            Destroy(gameObject);
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
