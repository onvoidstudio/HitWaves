using UnityEngine;
using Spine.Unity;

namespace HitWaves.Entity
{
    /// <summary>
    /// Spine 애니메이션 트랙 믹싱 초기화.
    /// 여러 애니메이션을 트랙별로 동시 재생한다.
    /// 랜덤 간격 재생 모드 지원.
    /// </summary>
    [RequireComponent(typeof(SkeletonAnimation))]
    public class SpineAnimationSetup : MonoBehaviour
    {
        private const string LOG_TAG = "SpineAnimationSetup";

        [System.Serializable]
        public struct TrackEntry
        {
            [Tooltip("트랙 인덱스 (0부터 시작)")]
            public int trackIndex;

            [Tooltip("애니메이션 이름 (Spine에서 정의한 이름)")]
            [SpineAnimation]
            public string animationName;

            [Tooltip("루프 여부 (랜덤 간격 모드에서는 무시됨)")]
            public bool loop;

            [Tooltip("랜덤 간격으로 한 번씩 재생")]
            public bool randomInterval;

            [Tooltip("랜덤 간격 최소 (초)")]
            [Min(0.1f)]
            public float minInterval;

            [Tooltip("랜덤 간격 최대 (초)")]
            [Min(0.1f)]
            public float maxInterval;
        }

        [Header("트랙 설정")]
        [Tooltip("재생할 애니메이션 트랙 목록")]
        [SerializeField] private TrackEntry[] _tracks;

        private SkeletonAnimation _skeletonAnimation;
        private float[] _timers;
        private float[] _nextTriggerTimes;

        private void Awake()
        {
            _skeletonAnimation = GetComponent<SkeletonAnimation>();

            if (_skeletonAnimation == null)
            {
                DebugLogger.LogWarning(LOG_TAG,
                    "SkeletonAnimation 컴포넌트를 찾을 수 없음", this);
                return;
            }

            if (_tracks == null || _tracks.Length == 0)
            {
                DebugLogger.LogWarning(LOG_TAG,
                    "트랙 설정이 비어있음", this);
                return;
            }

            _timers = new float[_tracks.Length];
            _nextTriggerTimes = new float[_tracks.Length];

            for (int i = 0; i < _tracks.Length; i++)
            {
                TrackEntry track = _tracks[i];
                if (string.IsNullOrEmpty(track.animationName)) continue;

                if (track.randomInterval)
                {
                    _nextTriggerTimes[i] = Random.Range(track.minInterval, track.maxInterval);
                }
                else
                {
                    _skeletonAnimation.AnimationState.SetAnimation(
                        track.trackIndex, track.animationName, track.loop);
                }
            }

            DebugLogger.Log(LOG_TAG,
                $"{_tracks.Length}개 트랙 애니메이션 설정 완료", this);
        }

private void Update()
        {
            if (_tracks == null) return;

            for (int i = 0; i < _tracks.Length; i++)
            {
                if (string.IsNullOrEmpty(_tracks[i].animationName)) continue;

                if (_tracks[i].randomInterval)
                {
                    _timers[i] += Time.deltaTime;

                    if (_timers[i] >= _nextTriggerTimes[i])
                    {
                        _skeletonAnimation.AnimationState.SetAnimation(
                            _tracks[i].trackIndex, _tracks[i].animationName, false);

                        _timers[i] = 0f;
                        _nextTriggerTimes[i] = Random.Range(
                            _tracks[i].minInterval, _tracks[i].maxInterval);
                    }
                }
                else if (_tracks[i].loop)
                {
                    // 루프 트랙이 꺼져있으면 즉시 재생 — 날개짓 등 항상 유지
                    var current = _skeletonAnimation.AnimationState.GetCurrent(_tracks[i].trackIndex);
                    if (current == null || current.Animation.Name != _tracks[i].animationName)
                    {
                        _skeletonAnimation.AnimationState.SetAnimation(
                            _tracks[i].trackIndex, _tracks[i].animationName, true);
                    }
                }
            }
        }
    }
}
