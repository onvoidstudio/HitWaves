using System;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;
using HitWaves.Core;

namespace HitWaves.Entity.AI.Boss
{
    /// <summary>
    /// King Dull Fly 돌진 패턴.
    /// 돌진 경로상의 모든 대상(플레이어 + 아군 Dull Fly)에게 대미지.
    /// </summary>
    public class ChargeBehavior : MonoBehaviour
    {
        private const string LOG_TAG = "ChargeBehavior";

        [Header("돌진 설정")]
        [Tooltip("돌진 속도")]
        [Min(1f)]
        [SerializeField] private float _chargeSpeed = 15f;

        [Tooltip("돌진 거리")]
        [Min(1f)]
        [SerializeField] private float _chargeDistance = 8f;

        [Tooltip("돌진 대미지")]
        [Min(0f)]
        [SerializeField] private float _chargeDamage = 50f;

        [Tooltip("돌진 히트 반경")]
        [Min(0.5f)]
        [SerializeField] private float _hitRadius = 2f;

        [Header("애니메이션")]
        [Tooltip("패턴 애니메이션 트랙 인덱스 (기본 트랙과 겹치지 않게)")]
        [SerializeField] private int _animTrack = 5;

        [Tooltip("돌진 시그널 애니메이션 (windUp)")]
        [SpineAnimation]
        [SerializeField] private string _signalAnim = "before-strike";

        [Tooltip("돌진 시작 애니메이션")]
        [SpineAnimation]
        [SerializeField] private string _startAnim = "start-strike";

        [Tooltip("돌진 루프 애니메이션")]
        [SpineAnimation]
        [SerializeField] private string _loopAnim = "strike-loop";

        private Transform _target;
        private Rigidbody2D _rigidbody;
        private StatHandler _statHandler;
        private SkeletonAnimation _skeletonAnimation;

        private bool _isCharging;
        private bool _isWindingUp;
        private bool _isStartingUp;

        public bool IsActive => _isCharging || _isWindingUp || _isStartingUp;
        private Vector2 _chargeDirection;
        private Vector2 _chargeStartPos;
        private HashSet<int> _hitTargets = new HashSet<int>();

        public event Action OnChargeFinished;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _statHandler = GetComponent<StatHandler>();
            _skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();
        }

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        public void Execute()
        {
            if (_target == null)
            {
                OnChargeFinished?.Invoke();
                return;
            }

            _chargeDirection = ((Vector2)_target.position - (Vector2)transform.position).normalized;
            _isWindingUp = true;
            _hitTargets.Clear();

            // before-strike 재생, 완료 시 다음 단계로
            var entry = PlayAnim(_signalAnim, false);
            if (entry != null)
            {
                entry.Complete += _ =>
                {
                    if (_isWindingUp)
                    {
                        TransitionToStartUp();
                    }
                };
            }
            else
            {
                TransitionToStartUp();
            }

            DebugLogger.Log(LOG_TAG,
                $"돌진 예비 동작 시작 — 방향: {_chargeDirection}", this);
        }

        private void TransitionToStartUp()
        {
            _isWindingUp = false;
            _isStartingUp = true;

            // start-strike 재생, 완료 후 실제 돌진 시작
            var entry = PlayAnim(_startAnim, false);
            if (entry != null)
            {
                entry.Complete += _ =>
                {
                    if (_isStartingUp)
                    {
                        BeginCharge();
                    }
                };
            }
            else
            {
                BeginCharge();
            }

            DebugLogger.Log(LOG_TAG, "돌진 준비 — start-strike 재생", this);
        }

        private void FixedUpdate()
        {
            if (_isWindingUp)
            {
                _rigidbody.linearVelocity = Vector2.zero;
                return;
            }

            if (_isStartingUp)
            {
                _rigidbody.linearVelocity = Vector2.zero;
                return;
            }

            if (!_isCharging) return;

            // 돌진 이동
            _rigidbody.linearVelocity = _chargeDirection * _chargeSpeed;

            // 경로상 타격 판정
            CheckChargeHits();

            // 거리 도달 시 종료
            float traveled = Vector2.Distance(_chargeStartPos, _rigidbody.position);
            if (traveled >= _chargeDistance)
            {
                FinishCharge();
            }
        }

        /// <summary>
        /// 돌진 경로 주변의 대상에게 대미지.
        /// 같은 진영(아군 Dull Fly) + 다른 진영(플레이어) 모두 대상.
        /// </summary>
        private void CheckChargeHits()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(
                _rigidbody.position, _hitRadius);

            for (int i = 0; i < hits.Length; i++)
            {
                GameObject hitGo = hits[i].gameObject;
                if (hitGo == gameObject) continue;

                int instanceId = hitGo.GetInstanceID();
                if (_hitTargets.Contains(instanceId)) continue;

                HealthHandler health = hitGo.GetComponent<HealthHandler>();
                if (health == null || health.IsDead) continue;

                bool damageApplied = health.TakeDamage(_chargeDamage);
                if (damageApplied)
                {
                    _hitTargets.Add(instanceId);

                    DebugLogger.Log(LOG_TAG,
                        $"돌진 히트 — {hitGo.name}, 데미지: {_chargeDamage}", this);
                }
            }
        }

        private void BeginCharge()
        {
            _isStartingUp = false;
            _isCharging = true;
            _chargeStartPos = _rigidbody.position;

            PlayAnim(_loopAnim, true);

            DebugLogger.Log(LOG_TAG,
                $"돌진 시작 — 방향: {_chargeDirection}, 속도: {_chargeSpeed}", this);
        }

        private void FinishCharge()
        {
            _isCharging = false;
            _isStartingUp = false;
            _rigidbody.linearVelocity = Vector2.zero;

            ClearAnim();

            DebugLogger.Log(LOG_TAG,
                $"돌진 종료 — {_hitTargets.Count}개 대상 타격", this);

            OnChargeFinished?.Invoke();
        }

        private Spine.TrackEntry PlayAnim(string animName, bool loop)
        {
            if (_skeletonAnimation == null || string.IsNullOrEmpty(animName)) return null;
            return _skeletonAnimation.AnimationState.SetAnimation(_animTrack, animName, loop);
        }

        private void ClearAnim()
        {
            if (_skeletonAnimation == null) return;
            _skeletonAnimation.AnimationState.SetEmptyAnimation(_animTrack, 0.2f);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // 돌진 중 벽 충돌 시 강제 종료
            if (_isCharging)
            {
                DebugLogger.Log(LOG_TAG, "돌진 중 벽 충돌 → 강제 종료", this);
                FinishCharge();
            }
        }
    }
}
