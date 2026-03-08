using System.Collections.Generic;
using UnityEngine;

namespace HitWaves.Core.Floor
{
    public class WallBuilder : MonoBehaviour
    {
        private const string LOG_TAG = "WallBuilder";

        [Header("벽 설정")]
        [Tooltip("벽 두께 (월드 유닛)")]
        [Min(0.1f)]
        [SerializeField] private float _wallThickness = 1f;

        [Header("디버그 시각화")]
        [Tooltip("벽을 시각적으로 표시할지 여부 (디버그용)")]
        [SerializeField] private bool _showDebugVisuals = true;

        [Tooltip("벽 디버그 색상")]
        [SerializeField] private Color _wallColor = new Color(0.2f, 0.15f, 0.1f, 1f);

        private Transform _container;
        private Sprite _whiteSprite;

        public float WallThickness => _wallThickness;

        /// <summary>
        /// 모든 방에 벽을 생성한다. 문 위치에는 구멍을 뚫는다.
        /// </summary>
        public void BuildWalls(List<RoomData> rooms, List<DoorData> doors)
        {
            Clear();

            _container = new GameObject("WallContainer").transform;
            _container.SetParent(transform);

            DebugLogger.Log(LOG_TAG, $"BuildWalls 시작 — {rooms.Count}개 방, {doors.Count}개 문", this);

            for (int i = 0; i < rooms.Count; i++)
            {
                BuildRoomWalls(rooms[i], doors);
            }

            DebugLogger.Log(LOG_TAG, "BuildWalls 완료", this);
        }

        /// <summary>
        /// 방 하나의 벽 4면을 생성한다.
        /// 해당 면에 문이 있으면 벽을 분할하여 문 공간을 비운다.
        /// </summary>
        private void BuildRoomWalls(RoomData room, List<DoorData> allDoors)
        {
            float halfWidth = room.Width * 0.5f;
            float halfHeight = room.Height * 0.5f;
            float halfThick = _wallThickness * 0.5f;
            Vector2 center = room.WorldCenter;

            // 이 방에 해당하는 문들을 면별로 수집
            List<DoorData> topDoors = new List<DoorData>();
            List<DoorData> bottomDoors = new List<DoorData>();
            List<DoorData> leftDoors = new List<DoorData>();
            List<DoorData> rightDoors = new List<DoorData>();

            for (int i = 0; i < allDoors.Count; i++)
            {
                DoorData door = allDoors[i];
                if (door.RoomA != room && door.RoomB != room) continue;

                WallSide side = door.GetSideFor(room);
                switch (side)
                {
                    case WallSide.Top: topDoors.Add(door); break;
                    case WallSide.Bottom: bottomDoors.Add(door); break;
                    case WallSide.Left: leftDoors.Add(door); break;
                    case WallSide.Right: rightDoors.Add(door); break;
                }
            }

            // 상 벽 (가로 방향, 문 구멍은 X축 기준)
            BuildHorizontalWall(room.Id, "Top",
                new Vector2(center.x, center.y + halfHeight + halfThick),
                room.Width + _wallThickness * 2f,
                center.x - halfWidth - _wallThickness,
                center.x + halfWidth + _wallThickness,
                topDoors);

            // 하 벽
            BuildHorizontalWall(room.Id, "Bottom",
                new Vector2(center.x, center.y - halfHeight - halfThick),
                room.Width + _wallThickness * 2f,
                center.x - halfWidth - _wallThickness,
                center.x + halfWidth + _wallThickness,
                bottomDoors);

            // 좌 벽 (세로 방향, 문 구멍은 Y축 기준)
            BuildVerticalWall(room.Id, "Left",
                new Vector2(center.x - halfWidth - halfThick, center.y),
                room.Height,
                center.y - halfHeight,
                center.y + halfHeight,
                leftDoors);

            // 우 벽
            BuildVerticalWall(room.Id, "Right",
                new Vector2(center.x + halfWidth + halfThick, center.y),
                room.Height,
                center.y - halfHeight,
                center.y + halfHeight,
                rightDoors);

            DebugLogger.Log(LOG_TAG,
                $"BuildRoomWalls #{room.Id} [{room.Label}] — 완료 " +
                $"(문: T{topDoors.Count} B{bottomDoors.Count} L{leftDoors.Count} R{rightDoors.Count})", this);
        }

        /// <summary>
        /// 가로 방향 벽을 생성한다. 문이 있으면 X축 기준으로 분할.
        /// </summary>
        private void BuildHorizontalWall(int roomId, string sideName,
            Vector2 wallCenter, float totalWidth, float wallLeft, float wallRight,
            List<DoorData> doors)
        {
            if (doors.Count == 0)
            {
                CreateWall($"Wall_{sideName}_{roomId}", wallCenter,
                    new Vector2(totalWidth, _wallThickness));
                return;
            }

            // 문을 X좌표로 정렬
            doors.Sort((a, b) => a.WorldPosition.x.CompareTo(b.WorldPosition.x));

            float cursor = wallLeft;

            for (int i = 0; i < doors.Count; i++)
            {
                float doorLeft = doors[i].WorldPosition.x - doors[i].Width * 0.5f;
                float doorRight = doors[i].WorldPosition.x + doors[i].Width * 0.5f;

                // 문 왼쪽 벽 조각
                if (doorLeft > cursor + 0.01f)
                {
                    float segWidth = doorLeft - cursor;
                    float segCenterX = (cursor + doorLeft) * 0.5f;
                    CreateWall($"Wall_{sideName}_{roomId}_seg{i}L",
                        new Vector2(segCenterX, wallCenter.y),
                        new Vector2(segWidth, _wallThickness));
                }

                cursor = doorRight;
            }

            // 마지막 문 오른쪽 벽 조각
            if (wallRight > cursor + 0.01f)
            {
                float segWidth = wallRight - cursor;
                float segCenterX = (cursor + wallRight) * 0.5f;
                CreateWall($"Wall_{sideName}_{roomId}_segEnd",
                    new Vector2(segCenterX, wallCenter.y),
                    new Vector2(segWidth, _wallThickness));
            }
        }

        /// <summary>
        /// 세로 방향 벽을 생성한다. 문이 있으면 Y축 기준으로 분할.
        /// </summary>
        private void BuildVerticalWall(int roomId, string sideName,
            Vector2 wallCenter, float totalHeight, float wallBottom, float wallTop,
            List<DoorData> doors)
        {
            if (doors.Count == 0)
            {
                CreateWall($"Wall_{sideName}_{roomId}", wallCenter,
                    new Vector2(_wallThickness, totalHeight));
                return;
            }

            // 문을 Y좌표로 정렬
            doors.Sort((a, b) => a.WorldPosition.y.CompareTo(b.WorldPosition.y));

            float cursor = wallBottom;

            for (int i = 0; i < doors.Count; i++)
            {
                float doorBottom = doors[i].WorldPosition.y - doors[i].Width * 0.5f;
                float doorTop = doors[i].WorldPosition.y + doors[i].Width * 0.5f;

                // 문 아래쪽 벽 조각
                if (doorBottom > cursor + 0.01f)
                {
                    float segHeight = doorBottom - cursor;
                    float segCenterY = (cursor + doorBottom) * 0.5f;
                    CreateWall($"Wall_{sideName}_{roomId}_seg{i}B",
                        new Vector2(wallCenter.x, segCenterY),
                        new Vector2(_wallThickness, segHeight));
                }

                cursor = doorTop;
            }

            // 마지막 문 위쪽 벽 조각
            if (wallTop > cursor + 0.01f)
            {
                float segHeight = wallTop - cursor;
                float segCenterY = (cursor + wallTop) * 0.5f;
                CreateWall($"Wall_{sideName}_{roomId}_segEnd",
                    new Vector2(wallCenter.x, segCenterY),
                    new Vector2(_wallThickness, segHeight));
            }
        }

        /// <summary>
        /// 벽 하나를 생성한다. BoxCollider2D + 디버그용 SpriteRenderer.
        /// </summary>
        private void CreateWall(string wallName, Vector2 position, Vector2 size)
        {
            GameObject wallGo = new GameObject(wallName);
            wallGo.transform.SetParent(_container);
            wallGo.transform.position = new Vector3(position.x, position.y, 0f);
            wallGo.transform.localScale = new Vector3(size.x, size.y, 1f);

            BoxCollider2D col = wallGo.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            if (_showDebugVisuals)
            {
                if (_whiteSprite == null)
                {
                    _whiteSprite = CreateWhiteSprite();
                }

                SpriteRenderer sr = wallGo.AddComponent<SpriteRenderer>();
                sr.sprite = _whiteSprite;
                sr.color = _wallColor;
                sr.sortingOrder = -5;
            }
        }

        /// <summary>
        /// 1x1 흰색 스프라이트를 런타임에 생성한다.
        /// </summary>
        private Sprite CreateWhiteSprite()
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        /// <summary>
        /// 기존 벽을 전부 제거한다.
        /// </summary>
        public void Clear()
        {
            if (_container != null)
            {
                Destroy(_container.gameObject);
                _container = null;
                DebugLogger.Log(LOG_TAG, "Clear — 기존 벽 제거", this);
            }
        }
    }
}
