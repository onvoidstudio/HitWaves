using UnityEngine;
using HitWaves.Core;

namespace HitWaves.Entity.AI.MoveBehaviors
{
    /// <summary>
    /// 부유 이동 모듈.
    /// 펄린 노이즈 기반으로 제자리에서 부드럽게 떠다님.
    /// 타겟이 설정되면 아주 느리게 타겟 쪽으로 끌려감.
    /// </summary>
    public class DriftBehavior : MonoBehaviour, IMoveBehavior
    {
        [Header("부유 이동")]
        [Tooltip("부유 이동 속도 배율 (MoveSpeed 스탯에 곱해짐)")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _driftSpeedRatio = 0.35f;

        [Tooltip("펄린 노이즈 변화 속도 (낮을수록 느긋하게 방향 변화)")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _noiseSpeed = 0.3f;

        [Header("타겟 편향")]
        [Tooltip("타겟 쪽으로 끌리는 힘 (0 = 순수 부유, 높을수록 접근 빠름)")]
        [Range(0f, 1f)]
        [SerializeField] private float _targetPull = 0.15f;

        public bool IsActive { get; set; } = true;

        private Transform _target;
        private Rigidbody2D _rb;
        private float _noiseOffsetX;
        private float _noiseOffsetY;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();

            // 개체마다 다른 노이즈 패턴을 갖도록 랜덤 오프셋
            _noiseOffsetX = Random.Range(0f, 1000f);
            _noiseOffsetY = Random.Range(0f, 1000f);
        }

        /// <summary>
        /// 은근한 추적 대상 설정.
        /// </summary>
        public void SetTarget(Transform target)
        {
            _target = target;
        }

        public Vector2 GetDesiredVelocity(Rigidbody2D rb, StatHandler statHandler)
        {
            if (!IsActive) return Vector2.zero;

            float time = Time.time * _noiseSpeed;

            // 펄린 노이즈로 -1 ~ 1 범위의 부드러운 방향 생성
            float nx = Mathf.PerlinNoise(_noiseOffsetX + time, 0f) * 2f - 1f;
            float ny = Mathf.PerlinNoise(0f, _noiseOffsetY + time) * 2f - 1f;
            Vector2 driftDir = new Vector2(nx, ny);

            // 타겟 쪽으로 아주 약하게 끌어당김
            if (_target != null && _targetPull > 0f)
            {
                Vector2 toTarget = ((Vector2)_target.position - rb.position).normalized;
                driftDir += toTarget * _targetPull;
            }

            // 속도 크기를 노이즈 벡터 크기에 비례 (멈칫거리는 느낌)
            float magnitude = Mathf.Clamp01(driftDir.magnitude);
            if (magnitude > 0.01f)
            {
                driftDir = driftDir.normalized * magnitude;
            }

            float speed = statHandler.GetStat(StatType.MoveSpeed) * _driftSpeedRatio;
            return driftDir * speed;
        }

        /// <summary>
        /// 벽 충돌 시 노이즈 오프셋을 점프시켜 자연스럽게 방향 전환.
        /// </summary>
        public void Reflect(Vector2 normal)
        {
            _noiseOffsetX += Random.Range(50f, 150f);
            _noiseOffsetY += Random.Range(50f, 150f);
        }
    }
}
