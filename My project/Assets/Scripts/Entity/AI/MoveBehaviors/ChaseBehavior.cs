using UnityEngine;
using HitWaves.Core;

namespace HitWaves.Entity.AI.MoveBehaviors
{
    /// <summary>
    /// 대상 추적 이동 모듈.
    /// 지정된 타겟을 향해 직선 추적.
    /// </summary>
    public class ChaseBehavior : MonoBehaviour, IMoveBehavior
    {
        [Header("추적")]
        [Tooltip("추적 속도 배율 (MoveSpeed 스탯에 곱해짐)")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _chaseSpeedRatio = 1f;

        public bool IsActive { get; set; } = true;

        private Transform _target;

        /// <summary>
        /// 추적 대상을 설정한다.
        /// </summary>
        public void SetTarget(Transform target)
        {
            _target = target;
        }

        public Vector2 GetDesiredVelocity(Rigidbody2D rb, StatHandler statHandler)
        {
            if (!IsActive) return Vector2.zero;
            if (_target == null) return Vector2.zero;

            Vector2 direction = ((Vector2)_target.position - rb.position).normalized;
            float speed = statHandler.GetStat(StatType.MoveSpeed) * _chaseSpeedRatio;

            return direction * speed;
        }
    }
}
