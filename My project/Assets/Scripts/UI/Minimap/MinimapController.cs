using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HitWaves.Core.Floor;
using HitWaves.Core.Game;

namespace HitWaves.UI.Minimap
{
    /// <summary>
    /// 미니맵 컨트롤러.
    /// 층 생성 시 모든 방 아이콘을 미리 생성하고,
    /// 방 전환/클리어 이벤트에 따라 상태만 갱신한다.
    /// </summary>
    public class MinimapController : MonoBehaviour
    {
        private const string LOG_TAG = "MinimapController";

        [Header("참조")]
        [Tooltip("방 전환 매니저 (이벤트 구독용)")]
        [SerializeField] private RoomTransitionManager _roomTransitionManager;

        [Header("미니맵 설정")]
        [Tooltip("미니맵 패널 크기 (픽셀)")]
        [SerializeField] private Vector2 _panelSize = new Vector2(200f, 200f);

        [Tooltip("미니맵 패널 여백 (우상단에서의 오프셋)")]
        [SerializeField] private Vector2 _panelMargin = new Vector2(20f, 20f);

        [Tooltip("미방문 인접방 ? 아이콘 크기 (픽셀)")]
        [Min(4f)]
        [SerializeField] private float _unknownIconSize = 10f;

        [Tooltip("미니맵 배경 색상")]
        [SerializeField] private Color _backgroundColor = new Color(0f, 0f, 0f, 0.5f);

        [Tooltip("월드 좌표 → 미니맵 변환 시 여백 비율 (0~0.5)")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _paddingRatio = 0.1f;

        private Canvas _canvas;
        private RectTransform _panelRect;
        private RectTransform _roomContainer;
        private Dictionary<int, MinimapRoomIcon> _icons = new Dictionary<int, MinimapRoomIcon>();
        private MinimapRoomIcon _currentIcon;
        private float _minimapScale;
        private Vector2 _worldMin;
        private Vector2 _worldMax;

        /// <summary>
        /// 층 생성 후 호출. 모든 방 아이콘을 미리 생성한다.
        /// </summary>
        public void BuildMinimap(IReadOnlyList<RoomData> rooms)
        {
            Clear();

            if (rooms == null || rooms.Count == 0) return;

            if (_canvas == null)
            {
                CreateUI();
            }

            CalculateWorldBounds(rooms);
            CalculateScale();

            for (int i = 0; i < rooms.Count; i++)
            {
                CreateIcon(rooms[i]);
            }

            DebugLogger.Log(LOG_TAG,
                $"BuildMinimap — {rooms.Count}개 아이콘 생성, " +
                $"scale: {_minimapScale:F2}", this);
        }

        /// <summary>
        /// 방 입장 시 호출. 미니맵 상태 갱신.
        /// </summary>
        public void OnRoomEntered(RoomData room)
        {
            if (room == null) return;

            // 이전 현재방 강조 해제
            if (_currentIcon != null)
            {
                _currentIcon.SetCurrent(false);
            }

            // 현재 방 표시
            if (_icons.TryGetValue(room.Id, out MinimapRoomIcon icon))
            {
                icon.ShowAsVisited();
                icon.SetCurrent(true);
                _currentIcon = icon;
            }

            // 인접방 ? 표시
            IReadOnlyList<RoomData> neighbors = room.Neighbors;
            for (int i = 0; i < neighbors.Count; i++)
            {
                RoomData neighbor = neighbors[i];
                if (_icons.TryGetValue(neighbor.Id, out MinimapRoomIcon neighborIcon))
                {
                    if (!neighbor.IsVisited)
                    {
                        neighborIcon.ShowAsUnknown();
                    }
                }
            }
        }

        /// <summary>
        /// 방 클리어 시 호출. 해당 방 아이콘 상태 갱신.
        /// </summary>
        public void OnRoomCleared(RoomData room)
        {
            if (room == null) return;

            if (_icons.TryGetValue(room.Id, out MinimapRoomIcon icon))
            {
                icon.UpdateClearState();
            }
        }

        /// <summary>
        /// 모든 방을 공개한다 (아이템 사용 시).
        /// </summary>
        public void RevealAll()
        {
            foreach (var pair in _icons)
            {
                pair.Value.Reveal();
            }

            // 현재 방 강조 유지
            if (_currentIcon != null)
            {
                _currentIcon.SetCurrent(true);
            }

            DebugLogger.Log(LOG_TAG, "RevealAll — 전체 맵 공개", this);
        }

        /// <summary>
        /// 기존 아이콘을 모두 제거한다 (층 재생성 시).
        /// </summary>
        public void Clear()
        {
            foreach (var pair in _icons)
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value.gameObject);
                }
            }

            _icons.Clear();
            _currentIcon = null;
        }

        private void OnEnable()
        {
            if (_roomTransitionManager != null)
            {
                _roomTransitionManager.OnRoomEntered += OnRoomEntered;
                _roomTransitionManager.OnRoomCleared += OnRoomCleared;
            }
        }

        private void OnDisable()
        {
            if (_roomTransitionManager != null)
            {
                _roomTransitionManager.OnRoomEntered -= OnRoomEntered;
                _roomTransitionManager.OnRoomCleared -= OnRoomCleared;
            }
        }

        private void CalculateWorldBounds(IReadOnlyList<RoomData> rooms)
        {
            _worldMin = new Vector2(float.MaxValue, float.MaxValue);
            _worldMax = new Vector2(float.MinValue, float.MinValue);

            for (int i = 0; i < rooms.Count; i++)
            {
                Rect rect = rooms[i].WorldRect;
                if (rect.xMin < _worldMin.x) _worldMin.x = rect.xMin;
                if (rect.yMin < _worldMin.y) _worldMin.y = rect.yMin;
                if (rect.xMax > _worldMax.x) _worldMax.x = rect.xMax;
                if (rect.yMax > _worldMax.y) _worldMax.y = rect.yMax;
            }
        }

        private void CalculateScale()
        {
            Vector2 worldSize = _worldMax - _worldMin;
            if (worldSize.x <= 0f) worldSize.x = 1f;
            if (worldSize.y <= 0f) worldSize.y = 1f;

            Vector2 usableSize = _panelSize * (1f - _paddingRatio * 2f);

            float scaleX = usableSize.x / worldSize.x;
            float scaleY = usableSize.y / worldSize.y;
            _minimapScale = Mathf.Min(scaleX, scaleY);
        }

        /// <summary>
        /// 월드 좌표를 미니맵 패널 로컬 좌표로 변환.
        /// </summary>
        private Vector2 WorldToMinimap(Vector2 worldCenter)
        {
            Vector2 worldSize = _worldMax - _worldMin;
            Vector2 normalized = new Vector2(
                worldSize.x > 0f ? (worldCenter.x - _worldMin.x) / worldSize.x : 0.5f,
                worldSize.y > 0f ? (worldCenter.y - _worldMin.y) / worldSize.y : 0.5f);

            float padding = _paddingRatio;
            Vector2 mapped = new Vector2(
                Mathf.Lerp(padding, 1f - padding, normalized.x) * _panelSize.x,
                Mathf.Lerp(padding, 1f - padding, normalized.y) * _panelSize.y);

            // 패널 중심 기준 오프셋 (피벗 0.5, 0.5)
            return mapped - _panelSize * 0.5f;
        }

        private void CreateIcon(RoomData room)
        {
            GameObject iconGo = new GameObject($"RoomIcon_{room.Id}");
            iconGo.transform.SetParent(_roomContainer, false);

            Image image = iconGo.AddComponent<Image>();
            image.raycastTarget = false;

            RectTransform rt = iconGo.GetComponent<RectTransform>();
            rt.anchoredPosition = WorldToMinimap(room.WorldCenter);

            MinimapRoomIcon icon = iconGo.AddComponent<MinimapRoomIcon>();
            icon.Initialize(room, _minimapScale, _unknownIconSize);

            _icons[room.Id] = icon;
        }

        private void CreateUI()
        {
            // Canvas
            GameObject canvasGo = new GameObject("MinimapCanvas");
            canvasGo.transform.SetParent(transform);

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Panel (우상단)
            GameObject panelGo = new GameObject("MinimapPanel");
            panelGo.transform.SetParent(canvasGo.transform, false);

            Image panelImage = panelGo.AddComponent<Image>();
            panelImage.color = _backgroundColor;
            panelImage.raycastTarget = false;

            _panelRect = panelGo.GetComponent<RectTransform>();
            _panelRect.anchorMin = new Vector2(1f, 1f);
            _panelRect.anchorMax = new Vector2(1f, 1f);
            _panelRect.pivot = new Vector2(1f, 1f);
            _panelRect.anchoredPosition = new Vector2(-_panelMargin.x, -_panelMargin.y);
            _panelRect.sizeDelta = _panelSize;

            // Room Container (패널 내부, 아이콘 부모)
            GameObject containerGo = new GameObject("RoomContainer");
            containerGo.transform.SetParent(panelGo.transform, false);

            _roomContainer = containerGo.AddComponent<RectTransform>();
            _roomContainer.anchorMin = Vector2.zero;
            _roomContainer.anchorMax = Vector2.one;
            _roomContainer.offsetMin = Vector2.zero;
            _roomContainer.offsetMax = Vector2.zero;
            _roomContainer.pivot = new Vector2(0.5f, 0.5f);

            DebugLogger.Log(LOG_TAG, "미니맵 UI 생성 완료", this);
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}
