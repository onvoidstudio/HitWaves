using UnityEngine;

namespace HitWaves.Core
{
    public class CameraController : MonoBehaviour
    {
        private const string LOG_TAG = "CameraController";

        [Header("Target")]
        [Tooltip("카메라가 추적할 대상 Transform")]
        [SerializeField] private Transform _target;

        [Header("Follow Settings")]
        [Tooltip("대상으로부터의 카메라 오프셋 (Z는 반드시 음수)")]
        [SerializeField] private Vector3 _offset = new Vector3(0f, 0f, -10f);

        [Header("Zone Transition")]
        [Tooltip("구역 전환 시 카메라가 목표에 도달하는 대략적 시간 (초). 높을수록 느리게 이동")]
        [Min(0.05f)]
        [SerializeField] private float _smoothTime = 0.35f;

        [Tooltip("카메라 최대 속도 = 캐릭터 속도 × 이 배율")]
        [Min(1f)]
        [SerializeField] private float _speedMultiplier = 3f;

        [Tooltip("캐릭터가 멈춰 있을 때 최소 카메라 속도")]
        [Min(0.5f)]
        [SerializeField] private float _minSpeed = 2f;

        [Header("Micro Follow")]
        [Tooltip("구역 내에서 플레이어를 미세하게 따라가는 비율 (0=고정, 0.2=살짝 따라감)")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _microFollowRatio = 0.15f;

        [Header("Room Fit")]
        [Tooltip("기본 카메라 orthographic size (방이 이보다 크면 구역 분할)")]
        [Min(1f)]
        [SerializeField] private float _baseOrthoSize = 3.5f;

        [Tooltip("방 가장자리 여백 (벽이 보이도록). 월드 유닛")]
        [Min(0f)]
        [SerializeField] private float _roomPadding = 0.5f;

        private bool _hasBounds;
        private float[] _zoneCentersX;
        private float[] _zoneCentersY;
        private float[] _zoneBoundsX;
        private float[] _zoneBoundsY;

        private int _currentZoneX;
        private int _currentZoneY;
        private Vector3 _velocity;
        private Rigidbody2D _targetRb;

        private void LateUpdate()
        {
            if (_target == null) return;

            Vector3 targetPos;

            if (_hasBounds && _zoneCentersX != null && _zoneCentersY != null)
            {
                int zx = FindZone(_target.position.x, _zoneBoundsX);
                int zy = FindZone(_target.position.y, _zoneBoundsY);

                if (zx != _currentZoneX || zy != _currentZoneY)
                {
                    _currentZoneX = zx;
                    _currentZoneY = zy;
                    DebugLogger.Log(LOG_TAG,
                        $"구역 변경 — zone: ({zx}, {zy})", this);
                }

                // 구역 중심 + 마이크로 팔로우 (플레이어 방향으로 살짝 편향)
                float followX = _zoneCentersX[zx]
                    + (_target.position.x - _zoneCentersX[zx]) * _microFollowRatio;
                float followY = _zoneCentersY[zy]
                    + (_target.position.y - _zoneCentersY[zy]) * _microFollowRatio;

                targetPos = new Vector3(
                    followX + _offset.x,
                    followY + _offset.y,
                    _target.position.z + _offset.z);
            }
            else
            {
                targetPos = _target.position + _offset;
            }

            float playerSpeed = _targetRb != null ? _targetRb.linearVelocity.magnitude : 0f;
            float maxSpeed = Mathf.Max(playerSpeed * _speedMultiplier, _minSpeed);

            transform.position = Vector3.SmoothDamp(
                transform.position, targetPos, ref _velocity, _smoothTime, maxSpeed);
        }

        public void SetTarget(Transform target)
        {
            _target = target;
            _targetRb = target != null ? target.GetComponent<Rigidbody2D>() : null;
            DebugLogger.Log(LOG_TAG,
                $"추적 대상 변경: {(target != null ? target.name : "null")}, " +
                $"Rigidbody2D: {(_targetRb != null ? "있음" : "없음")}", this);
        }

        public void SnapToTarget()
        {
            if (_target == null) return;

            _velocity = Vector3.zero;

            Vector3 pos;

            if (_hasBounds && _zoneCentersX != null && _zoneCentersY != null)
            {
                _currentZoneX = FindZone(_target.position.x, _zoneBoundsX);
                _currentZoneY = FindZone(_target.position.y, _zoneBoundsY);

                float followX = _zoneCentersX[_currentZoneX]
                    + (_target.position.x - _zoneCentersX[_currentZoneX]) * _microFollowRatio;
                float followY = _zoneCentersY[_currentZoneY]
                    + (_target.position.y - _zoneCentersY[_currentZoneY]) * _microFollowRatio;

                pos = new Vector3(
                    followX + _offset.x,
                    followY + _offset.y,
                    _target.position.z + _offset.z);
            }
            else
            {
                pos = _target.position + _offset;
            }

            transform.position = pos;
            DebugLogger.Log(LOG_TAG, $"SnapToTarget → {pos}", this);
        }

        /// <summary>
        /// 방 크기에 따라 카메라 구역을 계산한다.
        /// 가로에 맞춰 줌 → 방이 화면 폭을 꽉 채움.
        /// 세로가 뷰포트를 넘으면 구역 분할로 처리.
        /// </summary>
        public void SetRoomBounds(Vector2 center, Vector2 halfSize)
        {
            _hasBounds = true;
            Camera cam = Camera.main;

            float paddedHalfW = halfSize.x + _roomPadding;
            float paddedHalfH = halfSize.y + _roomPadding;

            float fitOrthoW = paddedHalfW / cam.aspect;
            float fitOrthoH = paddedHalfH;
            float fitOrthoSize = Mathf.Min(fitOrthoW, fitOrthoH);
            float newOrthoSize = Mathf.Min(fitOrthoSize, _baseOrthoSize);
            cam.orthographicSize = newOrthoSize;

            float camHH = newOrthoSize;
            float camHW = camHH * cam.aspect;

            float effectiveMinX = center.x - halfSize.x - _roomPadding;
            float effectiveMaxX = center.x + halfSize.x + _roomPadding;
            float effectiveMinY = center.y - halfSize.y - _roomPadding;
            float effectiveMaxY = center.y + halfSize.y + _roomPadding;

            _zoneCentersX = CalcZoneCenters(effectiveMinX, effectiveMaxX, camHW, out int zonesX);
            _zoneCentersY = CalcZoneCenters(effectiveMinY, effectiveMaxY, camHH, out int zonesY);
            _zoneBoundsX = CalcZoneBounds(_zoneCentersX);
            _zoneBoundsY = CalcZoneBounds(_zoneCentersY);

            DebugLogger.Log(LOG_TAG,
                $"SetRoomBounds — center: {center}, halfSize: {halfSize}, " +
                $"orthoSize: {newOrthoSize:F2} (base: {_baseOrthoSize}), " +
                $"zones: ({zonesX}x{zonesY}), " +
                $"camHalf: ({camHW:F2}, {camHH:F2})", this);
        }

        public void ClearBounds()
        {
            _hasBounds = false;
            _velocity = Vector3.zero;
            _zoneCentersX = null;
            _zoneCentersY = null;
            _zoneBoundsX = null;
            _zoneBoundsY = null;
            DebugLogger.Log(LOG_TAG, "ClearBounds — 바운드 해제", this);
        }

        private int FindZone(float pos, float[] bounds)
        {
            for (int i = 0; i < bounds.Length; i++)
            {
                if (pos < bounds[i]) return i;
            }
            return bounds.Length;
        }

        private float[] CalcZoneCenters(float roomMin, float roomMax,
            float camHalf, out int count)
        {
            float roomSize = roomMax - roomMin;
            float viewSize = camHalf * 2f;

            if (roomSize <= viewSize)
            {
                count = 1;
                return new float[] { (roomMin + roomMax) * 0.5f };
            }

            float first = roomMin + camHalf;
            float last = roomMax - camHalf;
            count = Mathf.Max(2, Mathf.CeilToInt(roomSize / viewSize));

            float[] centers = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / (count - 1);
                centers[i] = Mathf.Lerp(first, last, t);
            }
            return centers;
        }

        private float[] CalcZoneBounds(float[] centers)
        {
            if (centers.Length <= 1) return new float[0];

            float[] bounds = new float[centers.Length - 1];
            for (int i = 0; i < bounds.Length; i++)
            {
                bounds[i] = (centers[i] + centers[i + 1]) * 0.5f;
            }
            return bounds;
        }
    }
}
