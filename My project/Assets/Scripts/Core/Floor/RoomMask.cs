using UnityEngine;

namespace HitWaves.Core.Floor
{
    /// <summary>
    /// 현재 방 바깥을 검정 패널로 가린다.
    /// 상/하/좌/우 4개의 큰 검정 스프라이트를 배치하여 인접 방이 보이지 않게 한다.
    /// </summary>
    public class RoomMask : MonoBehaviour
    {
        private const string LOG_TAG = "RoomMask";
        private const float PANEL_SIZE = 100f;

        [Header("정렬")]
        [Tooltip("마스크의 Sorting Layer 이름")]
        [SerializeField] private string _sortingLayerName = "Default";

        [Tooltip("마스크의 Order in Layer (바닥보다 높고 벽보다 낮아야 함)")]
        [SerializeField] private int _orderInLayer = -5;

        private GameObject _maskRoot;
        private Transform _top;
        private Transform _bottom;
        private Transform _left;
        private Transform _right;

        private void Awake()
        {
            CreatePanels();
        }

        /// <summary>
        /// 현재 방 영역에 맞게 마스크 패널을 재배치한다.
        /// </summary>
        public void SetRoom(RoomData room)
        {
            if (room == null || _maskRoot == null) return;

            Rect rect = room.WorldRect;

            // 상단 패널: 방 위쪽 전체
            _top.position = new Vector3(rect.center.x, rect.yMax + PANEL_SIZE * 0.5f, 0f);
            _top.localScale = new Vector3(PANEL_SIZE, PANEL_SIZE, 1f);

            // 하단 패널: 방 아래쪽 전체
            _bottom.position = new Vector3(rect.center.x, rect.yMin - PANEL_SIZE * 0.5f, 0f);
            _bottom.localScale = new Vector3(PANEL_SIZE, PANEL_SIZE, 1f);

            // 좌측 패널: 방 왼쪽 전체
            _left.position = new Vector3(rect.xMin - PANEL_SIZE * 0.5f, rect.center.y, 0f);
            _left.localScale = new Vector3(PANEL_SIZE, PANEL_SIZE, 1f);

            // 우측 패널: 방 오른쪽 전체
            _right.position = new Vector3(rect.xMax + PANEL_SIZE * 0.5f, rect.center.y, 0f);
            _right.localScale = new Vector3(PANEL_SIZE, PANEL_SIZE, 1f);

            DebugLogger.Log(LOG_TAG,
                $"SetRoom #{room.Id} — rect: {rect}", this);
        }

        /// <summary>
        /// 4개의 검정 패널을 생성한다.
        /// </summary>
        private void CreatePanels()
        {
            _maskRoot = new GameObject("RoomMask");
            _maskRoot.transform.SetParent(transform);

            Sprite whiteSprite = CreateWhiteSprite();

            _top = CreatePanel("MaskTop", whiteSprite).transform;
            _bottom = CreatePanel("MaskBottom", whiteSprite).transform;
            _left = CreatePanel("MaskLeft", whiteSprite).transform;
            _right = CreatePanel("MaskRight", whiteSprite).transform;

            DebugLogger.Log(LOG_TAG, "마스크 패널 생성 완료", this);
        }

        private GameObject CreatePanel(string panelName, Sprite sprite)
        {
            GameObject go = new GameObject(panelName);
            go.transform.SetParent(_maskRoot.transform);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.black;
            sr.sortingLayerName = _sortingLayerName;
            sr.sortingOrder = _orderInLayer;

            return go;
        }

        private Sprite CreateWhiteSprite()
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
