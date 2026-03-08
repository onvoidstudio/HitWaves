using System.Collections.Generic;
using UnityEngine;

namespace HitWaves.Core.Floor
{
    public class RoomData
    {
        private const string LOG_TAG = "RoomData";

        private readonly List<RoomData> _neighbors = new List<RoomData>();

        public int Id { get; }
        public Vector2 WorldCenter { get; }
        public float Width { get; }
        public float Height { get; }
        public RoomLabel Label { get; }
        public int MinEnemyCount { get; }
        public int MaxEnemyCount { get; }
        public bool IsCleared { get; set; }
        public IReadOnlyList<RoomData> Neighbors => _neighbors;

        public Rect WorldRect => new Rect(
            WorldCenter.x - Width * 0.5f,
            WorldCenter.y - Height * 0.5f,
            Width,
            Height);

        public RoomData(int id, Vector2 worldCenter, float width, float height,
            RoomLabel label, int minEnemyCount = 0, int maxEnemyCount = 0)
        {
            Id = id;
            WorldCenter = worldCenter;
            Width = width;
            Height = height;
            Label = label;
            MinEnemyCount = minEnemyCount;
            MaxEnemyCount = maxEnemyCount;
            IsCleared = false;

            DebugLogger.Log(LOG_TAG,
                $"생성 — id: {id}, center: {worldCenter}, size: {width}x{height}, " +
                $"label: {label}, enemies: {minEnemyCount}~{maxEnemyCount}", null);
        }

        /// <summary>
        /// 양방향 인접 관계를 등록한다. A.AddNeighbor(B) → A↔B 서로 연결.
        /// </summary>
        public void AddNeighbor(RoomData other)
        {
            if (other == null || other == this) return;
            if (_neighbors.Contains(other)) return;

            _neighbors.Add(other);
            other._neighbors.Add(this);

            DebugLogger.Log(LOG_TAG,
                $"인접 등록 — #{Id} ↔ #{other.Id}", null);
        }
    }
}
