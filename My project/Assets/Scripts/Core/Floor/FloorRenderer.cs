using System.Collections.Generic;
using UnityEngine;

namespace HitWaves.Core.Floor
{
    public class FloorRenderer : MonoBehaviour
    {
        private const string LOG_TAG = "FloorRenderer";

        [Header("라벨별 바닥 색상")]
        [Tooltip("Start 방 색상")]
        [SerializeField] private Color _startColor = new Color(0.3f, 0.8f, 0.3f, 1f);

        [Tooltip("Normal 방 색상")]
        [SerializeField] private Color _normalColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        [Tooltip("Boss 방 색상")]
        [SerializeField] private Color _bossColor = new Color(0.8f, 0.2f, 0.2f, 1f);

        [Header("정렬")]
        [Tooltip("바닥 스프라이트의 Sorting Layer 이름")]
        [SerializeField] private string _sortingLayerName = "Default";

        [Tooltip("바닥 스프라이트의 Order in Layer")]
        [SerializeField] private int _orderInLayer = -10;

        private Sprite _whiteSprite;
        private Transform _container;

        /// <summary>
        /// 1x1 흰색 스프라이트를 런타임에 생성한다.
        /// color tinting으로 원하는 색상을 표현하기 위한 베이스 스프라이트.
        /// pixelsPerUnit=1이므로 localScale이 곧 월드 크기.
        /// </summary>
        private Sprite CreateWhiteSprite()
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            tex.filterMode = FilterMode.Point;

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            DebugLogger.Log(LOG_TAG, "1x1 흰색 스프라이트 생성 완료", this);
            return sprite;
        }

        /// <summary>
        /// RoomLabel에 대응하는 색상을 반환한다.
        /// 새 라벨 추가 시 여기에 case를 추가하면 된다.
        /// </summary>
        private Color GetColorByLabel(RoomLabel label)
        {
            switch (label)
            {
                case RoomLabel.Start:
                    return _startColor;
                case RoomLabel.Boss:
                    return _bossColor;
                case RoomLabel.Normal:
                default:
                    return _normalColor;
            }
        }

        /// <summary>
        /// 방 하나의 바닥을 렌더링한다.
        /// SpriteRenderer를 가진 GameObject를 생성하고 방 크기에 맞게 스케일 조정.
        /// </summary>
        private GameObject RenderRoom(RoomData room)
        {
            GameObject go = new GameObject($"Floor_{room.Label}_{room.Id}");
            go.transform.SetParent(_container);
            go.transform.position = new Vector3(room.WorldCenter.x, room.WorldCenter.y, 0f);
            go.transform.localScale = new Vector3(room.Width, room.Height, 1f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _whiteSprite;
            sr.color = GetColorByLabel(room.Label);
            sr.sortingLayerName = _sortingLayerName;
            sr.sortingOrder = _orderInLayer;

            DebugLogger.Log(LOG_TAG,
                $"RenderRoom #{room.Id} [{room.Label}] — pos: {room.WorldCenter}, size: {room.Width}x{room.Height}", this);

            return go;
        }

        /// <summary>
        /// 모든 방의 바닥을 한 번에 렌더링한다. 외부에서 호출하는 진입점.
        /// 기존 바닥이 있으면 먼저 제거한다.
        /// </summary>
        public void RenderAll(List<RoomData> rooms)
        {
            Clear();

            if (_whiteSprite == null)
            {
                _whiteSprite = CreateWhiteSprite();
            }

            _container = new GameObject("FloorContainer").transform;
            _container.SetParent(transform);

            DebugLogger.Log(LOG_TAG, $"RenderAll 시작 — {rooms.Count}개 방", this);

            for (int i = 0; i < rooms.Count; i++)
            {
                RenderRoom(rooms[i]);
            }

            DebugLogger.Log(LOG_TAG, "RenderAll 완료", this);
        }

        /// <summary>
        /// 기존 렌더링된 바닥을 전부 제거한다.
        /// </summary>
        public void Clear()
        {
            if (_container != null)
            {
                Destroy(_container.gameObject);
                _container = null;
                DebugLogger.Log(LOG_TAG, "Clear — 기존 바닥 제거", this);
            }
        }
    }
}
