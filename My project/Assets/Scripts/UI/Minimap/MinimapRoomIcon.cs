using UnityEngine;
using UnityEngine.UI;
using HitWaves.Core.Floor;

namespace HitWaves.UI.Minimap
{
    /// <summary>
    /// 미니맵 개별 방 아이콘.
    /// 방문 상태에 따라 표시 방식이 달라진다.
    /// - 숨김: 미방문 + 비인접
    /// - ? 아이콘: 미방문 + 인접 (고정 크기, 방 정보 비공개)
    /// - 실제 크기/색상: 방문한 방
    /// </summary>
    public class MinimapRoomIcon : MonoBehaviour
    {
        private RoomData _roomData;
        private RectTransform _rectTransform;
        private Image _image;

        private Vector2 _realSize;
        private Vector2 _unknownSize;
        private bool _isRevealed;

        private static readonly Color COLOR_START = new Color(0.3f, 0.8f, 0.3f, 1f);
        private static readonly Color COLOR_NORMAL = new Color(0.6f, 0.6f, 0.6f, 1f);
        private static readonly Color COLOR_BOSS = new Color(0.9f, 0.2f, 0.2f, 1f);
        private static readonly Color COLOR_CURRENT = new Color(1f, 1f, 1f, 1f);
        private static readonly Color COLOR_UNKNOWN = new Color(0.4f, 0.4f, 0.4f, 0.6f);
        private static readonly Color COLOR_CLEARED = new Color(0.5f, 0.7f, 0.5f, 0.8f);

        private Color _labelColor;
        private bool _isCurrent;

        public RoomData RoomData => _roomData;

        /// <summary>
        /// 아이콘 초기화. 미니맵 스케일에 맞춰 실제 크기와 ? 크기를 설정.
        /// </summary>
        public void Initialize(RoomData roomData, float minimapScale, float unknownIconSize)
        {
            _roomData = roomData;
            _rectTransform = GetComponent<RectTransform>();
            _image = GetComponent<Image>();

            _realSize = new Vector2(
                roomData.Width * minimapScale,
                roomData.Height * minimapScale);

            // 최소 크기 보장
            _realSize = Vector2.Max(_realSize, new Vector2(6f, 6f));

            _unknownSize = new Vector2(unknownIconSize, unknownIconSize);

            _labelColor = GetColorByLabel(roomData.Label);

            // 초기 상태: 숨김
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 미방문 인접방으로 표시 (? 아이콘, 고정 크기).
        /// </summary>
        public void ShowAsUnknown()
        {
            if (_roomData.IsVisited || _isRevealed) return;

            gameObject.SetActive(true);
            _rectTransform.sizeDelta = _unknownSize;
            _image.color = COLOR_UNKNOWN;
        }

        /// <summary>
        /// 방문한 방으로 표시 (실제 크기 + 라벨 색상).
        /// </summary>
        public void ShowAsVisited()
        {
            gameObject.SetActive(true);
            _rectTransform.sizeDelta = _realSize;
            _image.color = _roomData.IsCleared ? COLOR_CLEARED : _labelColor;
        }

        /// <summary>
        /// 현재 방 강조 표시.
        /// </summary>
        public void SetCurrent(bool isCurrent)
        {
            _isCurrent = isCurrent;

            if (!gameObject.activeSelf) return;

            if (_isCurrent)
            {
                _image.color = COLOR_CURRENT;
            }
            else if (_roomData.IsVisited || _isRevealed)
            {
                _image.color = _roomData.IsCleared ? COLOR_CLEARED : _labelColor;
            }
        }

        /// <summary>
        /// RevealAll 아이템 사용 시 모든 방을 공개.
        /// </summary>
        public void Reveal()
        {
            _isRevealed = true;
            gameObject.SetActive(true);
            _rectTransform.sizeDelta = _realSize;
            _image.color = _labelColor;
        }

        /// <summary>
        /// 클리어 상태 갱신.
        /// </summary>
        public void UpdateClearState()
        {
            if (!gameObject.activeSelf) return;
            if (_isCurrent) return;

            if (_roomData.IsCleared && (_roomData.IsVisited || _isRevealed))
            {
                _image.color = COLOR_CLEARED;
            }
        }

        private Color GetColorByLabel(RoomLabel label)
        {
            switch (label)
            {
                case RoomLabel.Start: return COLOR_START;
                case RoomLabel.Boss: return COLOR_BOSS;
                default: return COLOR_NORMAL;
            }
        }
    }
}
