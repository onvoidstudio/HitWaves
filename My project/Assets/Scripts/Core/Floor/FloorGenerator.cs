using System.Collections.Generic;
using UnityEngine;

namespace HitWaves.Core.Floor
{
    public class FloorGenerator : MonoBehaviour
    {
        private const string LOG_TAG = "FloorGenerator";

        [Header("설정")]
        [Tooltip("층 생성 설정 SO")]
        [SerializeField] private FloorGenerationConfig _config;

        [Header("시드")]
        [Tooltip("0이면 매번 랜덤 시드 생성, 값을 넣으면 동일한 맵 재현")]
        [SerializeField] private int _seed;

        private System.Random _rng;
        private List<RoomData> _rooms;
        private int _nextRoomId;

        public IReadOnlyList<RoomData> Rooms => _rooms;
        public int Seed => _seed;

        /// <summary>
        /// 층 전체를 생성한다. 외부에서 호출하는 진입점.
        /// 생성 순서: Start → Normal → Boss (Start에서 가장 먼 곳)
        /// </summary>
        public List<RoomData> Generate()
        {
            if (_seed == 0)
            {
                _seed = Random.Range(1, int.MaxValue);
            }

            _rng = new System.Random(_seed);
            _rooms = new List<RoomData>();
            _nextRoomId = 0;

            DebugLogger.Log(LOG_TAG, $"Generate 시작 — 시드: {_seed}", this);

            // 1단계: Start 배치
            GenerateByLabel(RoomLabel.Start);

            // 2단계: Normal 배치
            GenerateByLabel(RoomLabel.Normal);

            // 3단계: Boss 배치 (Start에서 그래프 상 가장 먼 방 옆에)
            PlaceBossRooms();

            DebugLogger.Log(LOG_TAG, $"Generate 완료 — 총 {_rooms.Count}개 방 생성", this);
            return _rooms;
        }

        /// <summary>
        /// 지정 라벨의 방을 RoomCountSettings에 설정된 개수만큼 생성한다.
        /// Boss는 별도 처리(PlaceBossRooms)하므로 여기서 스킵.
        /// </summary>
        private void GenerateByLabel(RoomLabel label)
        {
            Vector2Int range = _config.GetCountRange(label);
            if (range == Vector2Int.zero) return;

            int count = _rng.Next(range.x, range.y + 1);

            DebugLogger.Log(LOG_TAG,
                $"GenerateByLabel [{label}] — 범위: {range.x}~{range.y}, 결정: {count}개", this);

            RoomConfig[] configs = _config.GetConfigsByLabel(label);
            if (configs.Length == 0)
            {
                Debug.LogWarning($"[{LOG_TAG}] 라벨 [{label}]에 해당하는 RoomConfig 없음, 스킵");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                RoomConfig picked = _config.PickRandomConfig(configs, _rng);
                Vector2 size = picked.GetRandomSize(_rng);
                PlaceRoom(size.x, size.y, label,
                    picked.MinEnemyCount, picked.MaxEnemyCount);
            }
        }

        /// <summary>
        /// Boss 방을 배치한다.
        /// BFS로 Start에서 가장 먼 방을 찾고, 그 방에 붙여서 배치한다.
        /// </summary>
        private void PlaceBossRooms()
        {
            Vector2Int range = _config.GetCountRange(RoomLabel.Boss);
            if (range == Vector2Int.zero) return;

            int count = _rng.Next(range.x, range.y + 1);

            RoomConfig[] configs = _config.GetConfigsByLabel(RoomLabel.Boss);
            if (configs.Length == 0)
            {
                Debug.LogWarning($"[{LOG_TAG}] Boss RoomConfig 없음, 스킵");
                return;
            }

            DebugLogger.Log(LOG_TAG, $"PlaceBossRooms — {count}개 배치 시작", this);

            for (int i = 0; i < count; i++)
            {
                RoomData startRoom = FindRoomByLabel(RoomLabel.Start);
                if (startRoom == null)
                {
                    Debug.LogError($"[{LOG_TAG}] Start 방을 찾을 수 없음, Boss 배치 불가");
                    return;
                }

                RoomData farthest = FindFarthestByBFS(startRoom);

                RoomConfig picked = _config.PickRandomConfig(configs, _rng);
                Vector2 size = picked.GetRandomSize(_rng);
                PlaceRoomNextTo(size.x, size.y, RoomLabel.Boss, farthest,
                    picked.MinEnemyCount, picked.MaxEnemyCount);
            }
        }

        /// <summary>
        /// 지정 라벨의 첫 번째 방을 찾아 반환한다.
        /// </summary>
        private RoomData FindRoomByLabel(RoomLabel label)
        {
            for (int i = 0; i < _rooms.Count; i++)
            {
                if (_rooms[i].Label == label) return _rooms[i];
            }
            return null;
        }

        /// <summary>
        /// BFS로 시작 방에서 그래프 상 가장 먼 방을 찾는다.
        /// 거리 = 거쳐야 하는 방의 수 (hop count).
        /// </summary>
        private RoomData FindFarthestByBFS(RoomData start)
        {
            Queue<RoomData> queue = new Queue<RoomData>();
            HashSet<int> visited = new HashSet<int>();

            queue.Enqueue(start);
            visited.Add(start.Id);

            RoomData farthest = start;

            while (queue.Count > 0)
            {
                RoomData current = queue.Dequeue();
                farthest = current;

                IReadOnlyList<RoomData> neighbors = current.Neighbors;
                for (int i = 0; i < neighbors.Count; i++)
                {
                    if (!visited.Contains(neighbors[i].Id))
                    {
                        visited.Add(neighbors[i].Id);
                        queue.Enqueue(neighbors[i]);
                    }
                }
            }

            DebugLogger.Log(LOG_TAG,
                $"FindFarthestByBFS — Start #{start.Id} → 가장 먼 방 #{farthest.Id} " +
                $"(center: {farthest.WorldCenter})", this);

            return farthest;
        }

        /// <summary>
        /// 특정 방(target) 옆에 방을 배치한다.
        /// 일반 PlaceRoom과 다르게 anchor가 고정되어 있다.
        /// </summary>
        private void PlaceRoomNextTo(float width, float height, RoomLabel label, RoomData target,
            int minEnemyCount, int maxEnemyCount)
        {
            int id = _nextRoomId++;
            float gap = _config.RoomGap;
            int maxAttempts = _config.MaxPlacementAttempts;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                int direction = _rng.Next(0, 4);
                Vector2 center = CalculateAdjacentCenter(target, width, height, gap, direction);

                Rect candidate = new Rect(
                    center.x - width * 0.5f,
                    center.y - height * 0.5f,
                    width, height);

                if (!OverlapsAny(candidate, gap))
                {
                    RoomData room = new RoomData(id, center, width, height, label,
                        minEnemyCount, maxEnemyCount);
                    room.AddNeighbor(target);
                    _rooms.Add(room);
                    DebugLogger.Log(LOG_TAG,
                        $"방 #{id} [{label}] → #{target.Id} 옆 배치 성공 (시도: {attempt + 1})", this);
                    return;
                }
            }

            Debug.LogWarning($"[{LOG_TAG}] 방 #{id} [{label}] → #{target.Id} 옆 배치 실패, 스킵");
        }

        /// <summary>
        /// 방 하나를 배치한다.
        /// - 첫 번째 방: 원점(0,0)에 배치
        /// - 이후: 기존 방 중 하나를 랜덤으로 골라 붙인다.
        ///   배치 성공 시 anchor와 인접 관계를 등록한다.
        /// </summary>
        private void PlaceRoom(float width, float height, RoomLabel label,
            int minEnemyCount, int maxEnemyCount)
        {
            int id = _nextRoomId++;

            if (_rooms.Count == 0)
            {
                RoomData first = new RoomData(id, Vector2.zero, width, height, label,
                    minEnemyCount, maxEnemyCount);
                _rooms.Add(first);
                DebugLogger.Log(LOG_TAG, $"방 #{id} [{label}] 원점 배치 ({width}x{height})", this);
                return;
            }

            float gap = _config.RoomGap;
            int maxAttempts = _config.MaxPlacementAttempts;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                RoomData anchor = _rooms[_rng.Next(0, _rooms.Count)];
                int direction = _rng.Next(0, 4);
                Vector2 center = CalculateAdjacentCenter(anchor, width, height, gap, direction);

                Rect candidate = new Rect(
                    center.x - width * 0.5f,
                    center.y - height * 0.5f,
                    width, height);

                if (!OverlapsAny(candidate, gap))
                {
                    RoomData room = new RoomData(id, center, width, height, label,
                        minEnemyCount, maxEnemyCount);
                    room.AddNeighbor(anchor);
                    _rooms.Add(room);
                    DebugLogger.Log(LOG_TAG,
                        $"방 #{id} [{label}] 배치 성공 — center: {center}, size: {width}x{height} (시도: {attempt + 1})", this);
                    return;
                }
            }

            Debug.LogWarning($"[{LOG_TAG}] 방 #{id} [{label}] 배치 실패 — {maxAttempts}회 시도 초과, 스킵");
        }

        /// <summary>
        /// 기준 방(anchor)의 지정 방향에 새 방 중심 좌표를 계산한다.
        /// </summary>
        private Vector2 CalculateAdjacentCenter(RoomData anchor, float width, float height, float gap, int direction)
        {
            Rect aRect = anchor.WorldRect;
            float cx, cy;

            switch (direction)
            {
                case 0: // 위
                    cx = aRect.center.x + RandomOffset(anchor.Width, width);
                    cy = aRect.yMax + gap + height * 0.5f;
                    break;
                case 1: // 아래
                    cx = aRect.center.x + RandomOffset(anchor.Width, width);
                    cy = aRect.yMin - gap - height * 0.5f;
                    break;
                case 2: // 왼쪽
                    cx = aRect.xMin - gap - width * 0.5f;
                    cy = aRect.center.y + RandomOffset(anchor.Height, height);
                    break;
                default: // 오른쪽
                    cx = aRect.xMax + gap + width * 0.5f;
                    cy = aRect.center.y + RandomOffset(anchor.Height, height);
                    break;
            }

            return new Vector2(cx, cy);
        }

        /// <summary>
        /// 비정렬 축 랜덤 오프셋. 두 방이 최소 절반 겹치도록 범위 제한.
        /// </summary>
        private float RandomOffset(float anchorLength, float newLength)
        {
            float maxShift = (anchorLength + newLength) * 0.25f;
            return ((float)_rng.NextDouble() * 2f - 1f) * maxShift;
        }

        /// <summary>
        /// 후보 영역이 기존 방과 겹치는지 검사한다.
        /// </summary>
        private bool OverlapsAny(Rect candidate, float gap)
        {
            for (int i = 0; i < _rooms.Count; i++)
            {
                Rect existing = _rooms[i].WorldRect;

                Rect expanded = new Rect(
                    existing.x - gap,
                    existing.y - gap,
                    existing.width + gap * 2f,
                    existing.height + gap * 2f);

                if (candidate.Overlaps(expanded))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
