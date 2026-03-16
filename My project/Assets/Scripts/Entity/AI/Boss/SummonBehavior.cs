using System;
using UnityEngine;
using Spine.Unity;
using HitWaves.Entity.AI;
using HitWaves.Core.Game;

namespace HitWaves.Entity.AI.Boss
{
    /// <summary>
    /// Dull Fly 소환 패턴.
    /// 1~5마리를 보스 주변에 생성한다.
    /// </summary>
    public class SummonBehavior : MonoBehaviour
    {
        private const string LOG_TAG = "SummonBehavior";

        [Header("소환 설정")]
        [Tooltip("소환할 프리팹 목록 (랜덤 선택)")]
        [SerializeField] private GameObject[] _summonPrefabs;

        [Tooltip("최소 소환 수")]
        [Min(1)]
        [SerializeField] private int _minCount = 1;

        [Tooltip("최대 소환 수")]
        [Min(1)]
        [SerializeField] private int _maxCount = 5;

        [Tooltip("보스 중심에서 스폰 반경")]
        [Min(0.5f)]
        [SerializeField] private float _spawnRadius = 3f;

        [Tooltip("소환 연출 시간 (초, 소환 전 대기)")]
        [Min(0f)]
        [SerializeField] private float _castDuration = 0.5f;

        [Tooltip("소환 간격 (초, 한 마리씩 순차 소환)")]
        [Min(0.1f)]
        [SerializeField] private float _spawnInterval = 0.35f;

        [Header("애니메이션")]
        [Tooltip("패턴 애니메이션 트랙 인덱스")]
        [SerializeField] private int _animTrack = 5;

        [Tooltip("소환 시그널 애니메이션")]
        [SpineAnimation]
        [SerializeField] private string _signalAnim = "body-summon";

        private const float WALL_MARGIN = 1.5f;

        private Transform _target;
        private EnemySpawner _enemySpawner;
        private SkeletonAnimation _skeletonAnimation;
        private Rect _roomBounds;
        private bool _hasRoomBounds;
        private bool _isCasting;
        private bool _isSpawning;
        private float _castTimer;
        private float _spawnTimer;
        private int _pendingCount;
        private int _spawnedCount;

        public bool IsActive => _isCasting || _isSpawning;
        public event Action OnSummonFinished;

        private void Awake()
        {
            _enemySpawner = FindObjectOfType<EnemySpawner>();
            _skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();
        }

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        public void SetRoomBounds(Rect bounds)
        {
            _roomBounds = bounds;
            _hasRoomBounds = true;
        }

        public void Execute()
        {
            if (_summonPrefabs == null || _summonPrefabs.Length == 0)
            {
                DebugLogger.LogWarning(LOG_TAG,
                    "소환 프리팹이 설정되지 않음", this);
                OnSummonFinished?.Invoke();
                return;
            }

            _pendingCount = UnityEngine.Random.Range(_minCount, _maxCount + 1);
            _castTimer = 0f;
            _isCasting = true;

            if (_skeletonAnimation != null && !string.IsNullOrEmpty(_signalAnim))
            {
                _skeletonAnimation.AnimationState.SetAnimation(
                    _animTrack, _signalAnim, false);
            }

            DebugLogger.Log(LOG_TAG,
                $"소환 시전 시작 — {_pendingCount}마리 예정", this);
        }

        private void Update()
        {
            if (_isCasting)
            {
                _castTimer += Time.deltaTime;

                if (_castTimer >= _castDuration)
                {
                    _isCasting = false;
                    _isSpawning = true;
                    _spawnTimer = _spawnInterval; // 즉시 첫 마리 소환
                    _spawnedCount = 0;

                    // 시그널 → 루프로 전환 (소환 중 계속 재생)
                    if (_skeletonAnimation != null && !string.IsNullOrEmpty(_signalAnim))
                    {
                        _skeletonAnimation.AnimationState.SetAnimation(
                            _animTrack, _signalAnim, true);
                    }
                }

                return;
            }

            if (_isSpawning)
            {
                _spawnTimer += Time.deltaTime;

                if (_spawnTimer >= _spawnInterval)
                {
                    _spawnTimer -= _spawnInterval;
                    SpawnOneMinion();
                    _spawnedCount++;

                    if (_spawnedCount >= _pendingCount)
                    {
                        FinishSummon();
                    }
                }
            }
        }

        private void SpawnOneMinion()
        {
            GameObject prefab = _summonPrefabs[
                UnityEngine.Random.Range(0, _summonPrefabs.Length)];
            if (prefab == null) return;

            Vector2 offset = UnityEngine.Random.insideUnitCircle * _spawnRadius;
            Vector2 spawnPos = (Vector2)transform.position + offset;

            if (_hasRoomBounds)
            {
                spawnPos.x = Mathf.Clamp(spawnPos.x,
                    _roomBounds.xMin + WALL_MARGIN, _roomBounds.xMax - WALL_MARGIN);
                spawnPos.y = Mathf.Clamp(spawnPos.y,
                    _roomBounds.yMin + WALL_MARGIN, _roomBounds.yMax - WALL_MARGIN);
            }

            GameObject minion = Instantiate(prefab, spawnPos,
                Quaternion.identity);
            minion.name = $"{prefab.name}_Summoned_{_spawnedCount}";

            EntityBrain brain = minion.GetComponent<EntityBrain>();
            if (brain != null && _target != null)
            {
                brain.SetTarget(_target);
            }

            if (_enemySpawner != null)
            {
                _enemySpawner.RegisterEnemy(minion);
            }

            DebugLogger.Log(LOG_TAG,
                $"소환 {_spawnedCount + 1}/{_pendingCount} — {minion.name}", this);
        }

        private void FinishSummon()
        {
            _isSpawning = false;

            if (_skeletonAnimation != null)
            {
                _skeletonAnimation.AnimationState.SetEmptyAnimation(_animTrack, 0.2f);
            }

            DebugLogger.Log(LOG_TAG,
                $"소환 완료 — {_pendingCount}마리 생성", this);

            OnSummonFinished?.Invoke();
        }
    }
}
