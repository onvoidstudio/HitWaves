using System.Collections.Generic;
using UnityEngine;

namespace HitWaves.Core.Floor
{
    public class DoorBuilder : MonoBehaviour
    {
        private const string LOG_TAG = "DoorBuilder";

        [Header("문 설정")]
        [Tooltip("문 너비 (월드 유닛). 플레이어가 통과할 수 있는 크기")]
        [Min(1f)]
        [SerializeField] private float _doorWidth = 3f;

        [Tooltip("문 생성에 필요한 최소 접촉 길이 (월드 유닛)")]
        [Min(1f)]
        [SerializeField] private float _minOverlapLength = 3f;

        [Tooltip("방 사이 간격 (WallBuilder의 wallThickness와 동일해야 함)")]
        [Min(0.1f)]
        [SerializeField] private float _roomGap = 1f;

        [Header("문 차단 (잠금 시 물리 벽)")]
        [Tooltip("잠긴 문 디버그 색상")]
        [SerializeField] private Color _blockerColor = new Color(0.6f, 0.1f, 0.1f, 1f);

        [Tooltip("차단 벽 디버그 시각화")]
        [SerializeField] private bool _showBlockerVisuals = true;

        private List<DoorData> _doors;
        private List<DoorTrigger> _triggers;
        private Transform _triggerContainer;

        public IReadOnlyList<DoorData> Doors => _doors;

        /// <summary>
        /// 모든 인접 방 쌍에서 문 위치를 계산한다. 외부에서 호출하는 진입점.
        /// 중복 쌍 방지: A-B와 B-A를 한 번만 처리한다.
        /// </summary>
        public List<DoorData> BuildDoors(List<RoomData> rooms)
        {
            _doors = new List<DoorData>();
            HashSet<long> processedPairs = new HashSet<long>();

            DebugLogger.Log(LOG_TAG, $"BuildDoors 시작 — {rooms.Count}개 방", this);

            for (int i = 0; i < rooms.Count; i++)
            {
                RoomData roomA = rooms[i];
                IReadOnlyList<RoomData> neighbors = roomA.Neighbors;

                for (int j = 0; j < neighbors.Count; j++)
                {
                    RoomData roomB = neighbors[j];

                    long pairKey = GetPairKey(roomA.Id, roomB.Id);
                    if (processedPairs.Contains(pairKey)) continue;
                    processedPairs.Add(pairKey);

                    DoorData door = CalculateDoor(roomA, roomB);
                    if (door != null)
                    {
                        _doors.Add(door);
                    }
                }
            }

            DebugLogger.Log(LOG_TAG, $"BuildDoors 완료 — {_doors.Count}개 문 생성", this);
            return _doors;
        }

        /// <summary>
        /// 두 방 사이의 문 위치를 계산한다.
        /// 접촉면이 최소 길이 미만이면 null 반환.
        /// </summary>
        private DoorData CalculateDoor(RoomData roomA, RoomData roomB)
        {
            Rect rectA = roomA.WorldRect;
            Rect rectB = roomB.WorldRect;

            Vector2 delta = roomB.WorldCenter - roomA.WorldCenter;
            bool horizontal = Mathf.Abs(delta.x) > Mathf.Abs(delta.y);

            if (horizontal)
            {
                return CalculateHorizontalDoor(roomA, roomB, rectA, rectB, delta.x > 0);
            }
            else
            {
                return CalculateVerticalDoor(roomA, roomB, rectA, rectB, delta.y > 0);
            }
        }

        /// <summary>
        /// 좌우 방향 접촉면에서 문을 계산한다.
        /// B가 A의 오른쪽이면 A의 Right벽, B의 Left벽에 문 생성.
        /// </summary>
        private DoorData CalculateHorizontalDoor(RoomData roomA, RoomData roomB,
            Rect rectA, Rect rectB, bool bIsRight)
        {
            // Y축 겹침 범위 계산
            float overlapMin = Mathf.Max(rectA.yMin, rectB.yMin);
            float overlapMax = Mathf.Min(rectA.yMax, rectB.yMax);
            float overlapLength = overlapMax - overlapMin;

            if (overlapLength < _minOverlapLength)
            {
                DebugLogger.Log(LOG_TAG,
                    $"#{roomA.Id} ↔ #{roomB.Id} 수평 접촉면 부족 ({overlapLength:F1} < {_minOverlapLength})", this);
                return null;
            }

            float doorY = (overlapMin + overlapMax) * 0.5f;
            float doorX;
            WallSide sideA;
            WallSide sideB;

            if (bIsRight)
            {
                doorX = (rectA.xMax + rectB.xMin) * 0.5f;
                sideA = WallSide.Right;
                sideB = WallSide.Left;
            }
            else
            {
                doorX = (rectB.xMax + rectA.xMin) * 0.5f;
                sideA = WallSide.Left;
                sideB = WallSide.Right;
            }

            float actualWidth = Mathf.Min(_doorWidth, overlapLength);

            return new DoorData(roomA, roomB, new Vector2(doorX, doorY),
                sideA, sideB, actualWidth);
        }

        /// <summary>
        /// 상하 방향 접촉면에서 문을 계산한다.
        /// B가 A의 위쪽이면 A의 Top벽, B의 Bottom벽에 문 생성.
        /// </summary>
        private DoorData CalculateVerticalDoor(RoomData roomA, RoomData roomB,
            Rect rectA, Rect rectB, bool bIsAbove)
        {
            // X축 겹침 범위 계산
            float overlapMin = Mathf.Max(rectA.xMin, rectB.xMin);
            float overlapMax = Mathf.Min(rectA.xMax, rectB.xMax);
            float overlapLength = overlapMax - overlapMin;

            if (overlapLength < _minOverlapLength)
            {
                DebugLogger.Log(LOG_TAG,
                    $"#{roomA.Id} ↔ #{roomB.Id} 수직 접촉면 부족 ({overlapLength:F1} < {_minOverlapLength})", this);
                return null;
            }

            float doorX = (overlapMin + overlapMax) * 0.5f;
            float doorY;
            WallSide sideA;
            WallSide sideB;

            if (bIsAbove)
            {
                doorY = (rectA.yMax + rectB.yMin) * 0.5f;
                sideA = WallSide.Top;
                sideB = WallSide.Bottom;
            }
            else
            {
                doorY = (rectB.yMax + rectA.yMin) * 0.5f;
                sideA = WallSide.Bottom;
                sideB = WallSide.Top;
            }

            float actualWidth = Mathf.Min(_doorWidth, overlapLength);

            return new DoorData(roomA, roomB, new Vector2(doorX, doorY),
                sideA, sideB, actualWidth);
        }

        /// <summary>
        /// 문 위치에 트리거 콜라이더를 생성한다.
        /// 플레이어가 진입하면 onPlayerEnter 콜백이 호출된다.
        /// </summary>
        public void BuildDoorTriggers(List<DoorData> doors, Transform playerTransform,
            System.Action<DoorData> onPlayerEnter, float wallThickness)
        {
            ClearTriggers();

            _triggers = new List<DoorTrigger>();
            _triggerContainer = new GameObject("DoorTriggerContainer").transform;
            _triggerContainer.SetParent(transform);

            DebugLogger.Log(LOG_TAG,
                $"BuildDoorTriggers 시작 — {doors.Count}개 문", this);

            for (int i = 0; i < doors.Count; i++)
            {
                CreateDoorTrigger(doors[i], playerTransform, onPlayerEnter, wallThickness);
            }

            DebugLogger.Log(LOG_TAG,
                $"BuildDoorTriggers 완료 — {doors.Count}개 트리거 생성", this);
        }

        /// <summary>
        /// 문 하나에 대한 트리거 GameObject를 생성한다.
        /// </summary>
        private void CreateDoorTrigger(DoorData door, Transform playerTransform,
            System.Action<DoorData> onPlayerEnter, float wallThickness)
        {
            GameObject triggerGo = new GameObject(
                $"DoorTrigger_{door.RoomA.Id}_{door.RoomB.Id}");
            triggerGo.transform.SetParent(_triggerContainer);
            triggerGo.transform.position = new Vector3(
                door.WorldPosition.x, door.WorldPosition.y, 0f);

            bool isHorizontal = door.SideInA == WallSide.Top
                || door.SideInA == WallSide.Bottom;

            float triggerWidth = door.Width * 0.7f;

            Vector2 triggerSize = isHorizontal
                ? new Vector2(triggerWidth, wallThickness)
                : new Vector2(wallThickness, triggerWidth);

            BoxCollider2D col = triggerGo.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = triggerSize;

            DoorTrigger trigger = triggerGo.AddComponent<DoorTrigger>();
            trigger.Initialize(door, playerTransform, onPlayerEnter);
            _triggers.Add(trigger);

            // 잠금 시 물리 차단용 오브젝트 생성 (기본 비활성)
            GameObject blockerGo = CreateDoorBlocker(door, wallThickness);
            blockerGo.transform.SetParent(triggerGo.transform);
            trigger.SetBlocker(blockerGo);

            DebugLogger.Log(LOG_TAG,
                $"트리거 생성 — #{door.RoomA.Id} ↔ #{door.RoomB.Id}, " +
                $"pos: {door.WorldPosition}, size: {triggerSize}", this);
        }

        /// <summary>
        /// 문 위치에 물리 차단 오브젝트를 생성한다.
        /// 잠금 시 활성화되어 플레이어 통과를 막는다.
        /// </summary>
        private GameObject CreateDoorBlocker(DoorData door, float wallThickness)
        {
            GameObject blockerGo = new GameObject(
                $"DoorBlocker_{door.RoomA.Id}_{door.RoomB.Id}");
            blockerGo.transform.position = new Vector3(
                door.WorldPosition.x, door.WorldPosition.y, 0f);

            bool isHorizontal = door.SideInA == WallSide.Top
                || door.SideInA == WallSide.Bottom;

            Vector2 blockerSize = isHorizontal
                ? new Vector2(door.Width, wallThickness)
                : new Vector2(wallThickness, door.Width);

            blockerGo.transform.localScale = new Vector3(blockerSize.x, blockerSize.y, 1f);

            BoxCollider2D col = blockerGo.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            if (_showBlockerVisuals)
            {
                SpriteRenderer sr = blockerGo.AddComponent<SpriteRenderer>();
                sr.sprite = CreateWhiteSprite();
                sr.color = _blockerColor;
                sr.sortingOrder = -4;
            }

            blockerGo.SetActive(false);
            return blockerGo;
        }

        private Sprite _whiteSprite;

        private Sprite CreateWhiteSprite()
        {
            if (_whiteSprite != null) return _whiteSprite;

            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f), 1f);
            return _whiteSprite;
        }

        /// <summary>
        /// 지정 방에 연결된 모든 문 트리거를 잠근다.
        /// </summary>
        public void LockDoorsForRoom(RoomData room)
        {
            if (_triggers == null) return;

            int count = 0;
            for (int i = 0; i < _triggers.Count; i++)
            {
                DoorTrigger trigger = _triggers[i];
                if (trigger == null) continue;

                DoorData door = trigger.DoorData;
                if (door.RoomA == room || door.RoomB == room)
                {
                    trigger.Lock();
                    count++;
                }
            }

            DebugLogger.Log(LOG_TAG,
                $"LockDoorsForRoom — 방 #{room.Id}, {count}개 문 잠금", this);
        }

        /// <summary>
        /// 지정 방에 연결된 모든 문 트리거를 해제한다.
        /// </summary>
        public void UnlockDoorsForRoom(RoomData room)
        {
            if (_triggers == null) return;

            int count = 0;
            for (int i = 0; i < _triggers.Count; i++)
            {
                DoorTrigger trigger = _triggers[i];
                if (trigger == null) continue;

                DoorData door = trigger.DoorData;
                if (door.RoomA == room || door.RoomB == room)
                {
                    trigger.Unlock();
                    count++;
                }
            }

            DebugLogger.Log(LOG_TAG,
                $"UnlockDoorsForRoom — 방 #{room.Id}, {count}개 문 해제", this);
        }

        /// <summary>
        /// 기존 트리거를 전부 제거한다.
        /// </summary>
        private void ClearTriggers()
        {
            if (_triggerContainer != null)
            {
                Destroy(_triggerContainer.gameObject);
                _triggerContainer = null;
                DebugLogger.Log(LOG_TAG, "ClearTriggers — 기존 트리거 제거", this);
            }
        }

        /// <summary>
        /// 두 방 ID로 중복 방지용 고유 키를 생성한다.
        /// 작은 ID를 상위 32비트, 큰 ID를 하위 32비트에 배치.
        /// </summary>
        private long GetPairKey(int idA, int idB)
        {
            int min = Mathf.Min(idA, idB);
            int max = Mathf.Max(idA, idB);
            return ((long)min << 32) | (uint)max;
        }
    }
}
