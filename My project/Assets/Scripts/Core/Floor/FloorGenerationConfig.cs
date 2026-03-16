using System;
using UnityEngine;

namespace HitWaves.Core.Floor
{
    [Serializable]
    public struct RoomCountEntry
    {
        [Tooltip("방 종류")]
        public RoomLabel Label;

        [Tooltip("생성 개수 범위 (x=최소, y=최대)")]
        public Vector2Int Count;
    }

    [CreateAssetMenu(fileName = "NewFloorGenerationConfig", menuName = "HitWaves/Floor Generation Config")]
    public class FloorGenerationConfig : ScriptableObject
    {
        private const string LOG_TAG = "FloorGenerationConfig";

        [Header("방 프리셋")]
        [Tooltip("사용할 방 프리셋 목록 (RoomLabel로 분류)")]
        [SerializeField] private RoomConfig[] _roomConfigs;

        [Header("라벨별 방 개수")]
        [Tooltip("각 라벨별 생성 개수 범위")]
        [SerializeField] private RoomCountEntry[] _roomCountSettings;

        [Header("보스 풀")]
        [Tooltip("이 테마에서 등장 가능한 보스 목록 (랜덤 선택)")]
        [SerializeField] private BossData[] _bossPool;

        [Header("배치")]
        [Tooltip("방 사이 간격 (월드 유닛). 나중에 벽/문이 들어갈 공간")]
        [Min(0f)]
        [SerializeField] private float _roomGap = 1f;

        [Tooltip("방 배치 시 겹침 회피 최대 시도 횟수")]
        [Min(1)]
        [SerializeField] private int _maxPlacementAttempts = 50;

        public RoomConfig[] RoomConfigs => _roomConfigs;
        public RoomCountEntry[] RoomCountSettings => _roomCountSettings;
        public BossData[] BossPool => _bossPool;
        public float RoomGap => _roomGap;
        public int MaxPlacementAttempts => _maxPlacementAttempts;

        /// <summary>
        /// 지정한 라벨의 생성 개수 범위를 반환한다. 설정이 없으면 (0, 0).
        /// </summary>
        public Vector2Int GetCountRange(RoomLabel label)
        {
            for (int i = 0; i < _roomCountSettings.Length; i++)
            {
                if (_roomCountSettings[i].Label == label)
                {
                    DebugLogger.Log(LOG_TAG,
                        $"GetCountRange({label}) → {_roomCountSettings[i].Count}", null);
                    return _roomCountSettings[i].Count;
                }
            }

            Debug.LogWarning($"[{LOG_TAG}] GetCountRange: {label} 설정 없음, (0,0) 반환");
            return Vector2Int.zero;
        }

        /// <summary>
        /// 지정한 라벨에 해당하는 프리셋만 필터링하여 반환한다.
        /// </summary>
        public RoomConfig[] GetConfigsByLabel(RoomLabel label)
        {
            int count = 0;
            for (int i = 0; i < _roomConfigs.Length; i++)
            {
                if (_roomConfigs[i].Label == label) count++;
            }

            RoomConfig[] result = new RoomConfig[count];
            int index = 0;
            for (int i = 0; i < _roomConfigs.Length; i++)
            {
                if (_roomConfigs[i].Label == label)
                {
                    result[index] = _roomConfigs[i];
                    index++;
                }
            }

            DebugLogger.Log(LOG_TAG, $"GetConfigsByLabel({label}) → {count}개 찾음", null);
            return result;
        }

        /// <summary>
        /// 가중치 기반으로 프리셋 배열에서 하나를 랜덤 선택한다.
        /// </summary>
        public RoomConfig PickRandomConfig(RoomConfig[] configs, System.Random rng)
        {
            if (configs == null || configs.Length == 0)
            {
                Debug.LogError($"[{LOG_TAG}] PickRandomConfig: 프리셋 배열이 비어 있음");
                return null;
            }

            if (configs.Length == 1)
            {
                DebugLogger.Log(LOG_TAG, $"PickRandomConfig → {configs[0].name} (1개뿐)", null);
                return configs[0];
            }

            float totalWeight = 0f;
            for (int i = 0; i < configs.Length; i++)
            {
                totalWeight += configs[i].Weight;
            }

            if (totalWeight <= 0f)
            {
                Debug.LogWarning($"[{LOG_TAG}] PickRandomConfig: 총 가중치가 0 이하, 첫 번째 반환");
                return configs[0];
            }

            float roll = (float)rng.NextDouble() * totalWeight;
            float cumulative = 0f;

            for (int i = 0; i < configs.Length; i++)
            {
                cumulative += configs[i].Weight;
                if (roll <= cumulative)
                {
                    DebugLogger.Log(LOG_TAG,
                        $"PickRandomConfig → {configs[i].name} (roll: {roll:F2}/{totalWeight:F2})", null);
                    return configs[i];
                }
            }

            return configs[configs.Length - 1];
        }

        /// <summary>
        /// 보스 풀에서 랜덤으로 BossData 하나를 선택한다.
        /// 풀이 비어있으면 null 반환.
        /// </summary>
        public BossData GetRandomBoss(System.Random rng)
        {
            if (_bossPool == null || _bossPool.Length == 0)
            {
                Debug.LogWarning($"[{LOG_TAG}] GetRandomBoss: 보스 풀이 비어있음");
                return null;
            }

            int index = rng.Next(0, _bossPool.Length);
            DebugLogger.Log(LOG_TAG,
                $"GetRandomBoss → {_bossPool[index].DisplayName} ({index}/{_bossPool.Length})", null);
            return _bossPool[index];
        }
    }
}
