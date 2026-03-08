using UnityEngine;

namespace HitWaves.Core.Floor
{
    public class DoorTrigger : MonoBehaviour
    {
        private const string LOG_TAG = "DoorTrigger";

        private DoorData _doorData;
        private Transform _playerTransform;
        private System.Action<DoorData> _onPlayerEnter;
        private bool _armed = true;
        private bool _locked;
        private GameObject _blocker;

        public DoorData DoorData => _doorData;

        /// <summary>
        /// 문 트리거를 초기화한다.
        /// </summary>
        public void Initialize(DoorData doorData, Transform playerTransform,
            System.Action<DoorData> onPlayerEnter)
        {
            _doorData = doorData;
            _playerTransform = playerTransform;
            _onPlayerEnter = onPlayerEnter;

            DebugLogger.Log(LOG_TAG,
                $"Initialize — 문 #{doorData.RoomA.Id} ↔ #{doorData.RoomB.Id}, " +
                $"player: {playerTransform.name}", this);
        }

        /// <summary>
        /// 문 차단 오브젝트를 설정한다. Lock/Unlock 시 활성화/비활성화된다.
        /// </summary>
        public void SetBlocker(GameObject blocker)
        {
            _blocker = blocker;
            if (_blocker != null)
            {
                _blocker.SetActive(_locked);
            }
        }

        public void Lock()
        {
            _locked = true;
            if (_blocker != null) _blocker.SetActive(true);

            DebugLogger.Log(LOG_TAG,
                $"Lock — 문 #{_doorData.RoomA.Id} ↔ #{_doorData.RoomB.Id}", this);
        }

        public void Unlock()
        {
            _locked = false;
            if (_blocker != null) _blocker.SetActive(false);

            DebugLogger.Log(LOG_TAG,
                $"Unlock — 문 #{_doorData.RoomA.Id} ↔ #{_doorData.RoomB.Id}", this);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_armed || _locked) return;
            if (_doorData == null || _playerTransform == null || _onPlayerEnter == null) return;

            Rigidbody2D rb = other.attachedRigidbody;
            if (rb != null && rb.transform == _playerTransform)
            {
                _armed = false;

                DebugLogger.Log(LOG_TAG,
                    $"플레이어 감지 (disarm) — 문 #{_doorData.RoomA.Id} ↔ #{_doorData.RoomB.Id}", this);
                _onPlayerEnter.Invoke(_doorData);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (_doorData == null || _playerTransform == null) return;

            Rigidbody2D rb = other.attachedRigidbody;
            if (rb != null && rb.transform == _playerTransform)
            {
                _armed = true;

                DebugLogger.Log(LOG_TAG,
                    $"플레이어 이탈 (re-arm) — 문 #{_doorData.RoomA.Id} ↔ #{_doorData.RoomB.Id}", this);
            }
        }
    }
}
