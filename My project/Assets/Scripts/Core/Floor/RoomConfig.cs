using UnityEngine;

namespace HitWaves.Core.Floor
{
    [CreateAssetMenu(fileName = "NewRoomConfig", menuName = "HitWaves/Room Config")]
    public class RoomConfig : ScriptableObject
    {
        private const string LOG_TAG = "RoomConfig";

        [Header("분류")]
        [Tooltip("방 종류 라벨")]
        [SerializeField] private RoomLabel _label = RoomLabel.Normal;

        [Header("크기 범위")]
        [Tooltip("최소 가로 (월드 유닛)")]
        [Min(4f)]
        [SerializeField] private float _minWidth = 12f;

        [Tooltip("최대 가로 (월드 유닛)")]
        [Min(4f)]
        [SerializeField] private float _maxWidth = 16f;

        [Tooltip("최소 세로 (월드 유닛)")]
        [Min(4f)]
        [SerializeField] private float _minHeight = 10f;

        [Tooltip("최대 세로 (월드 유닛)")]
        [Min(4f)]
        [SerializeField] private float _maxHeight = 14f;

        [Header("선택 가중치")]
        [Tooltip("이 프리셋이 선택될 확률 가중치 (0이면 선택 안 됨)")]
        [Min(0f)]
        [SerializeField] private float _weight = 1f;

        [Header("적 스폰")]
        [Tooltip("방 입장 시 최소 적 수")]
        [Min(0)]
        [SerializeField] private int _minEnemyCount = 0;

        [Tooltip("방 입장 시 최대 적 수")]
        [Min(0)]
        [SerializeField] private int _maxEnemyCount = 0;

        public RoomLabel Label => _label;
        public float MinWidth => _minWidth;
        public float MaxWidth => _maxWidth;
        public float MinHeight => _minHeight;
        public float MaxHeight => _maxHeight;
        public float Weight => _weight;
        public int MinEnemyCount => _minEnemyCount;
        public int MaxEnemyCount => _maxEnemyCount;

        public Vector2 GetRandomSize(System.Random rng)
        {
            float width = _minWidth + (float)rng.NextDouble() * (_maxWidth - _minWidth);
            float height = _minHeight + (float)rng.NextDouble() * (_maxHeight - _minHeight);

            width = Mathf.Round(width * 2f) * 0.5f;
            height = Mathf.Round(height * 2f) * 0.5f;

            DebugLogger.Log(LOG_TAG,
                $"GetRandomSize [{_label}] → {width}x{height}", null);

            return new Vector2(width, height);
        }
    }
}
