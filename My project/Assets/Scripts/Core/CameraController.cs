using UnityEngine;

namespace HitWaves.Core
{
    public class CameraController : MonoBehaviour
    {
        private const string LOG_TAG = "CameraController";

        [Header("Target")]
        [SerializeField] private Transform _target;

        [Header("Follow Settings")]
        [SerializeField] private float _smoothSpeed = 5f;
        [SerializeField] private Vector3 _offset = new Vector3(0f, 0f, -10f);

        private void LateUpdate()
        {
            if (_target == null) return;

            Vector3 targetPosition = _target.position + _offset;
            Vector3 smoothed = Vector3.Lerp(transform.position, targetPosition, _smoothSpeed * Time.deltaTime);
            transform.position = smoothed;
        }

        public void SetTarget(Transform target)
        {
            _target = target;
            DebugLogger.Log(LOG_TAG, $"추적 대상 변경: {(target != null ? target.name : "null")}", this);
        }

        public void SnapToTarget()
        {
            if (_target == null) return;

            transform.position = _target.position + _offset;
        }
    }
}
