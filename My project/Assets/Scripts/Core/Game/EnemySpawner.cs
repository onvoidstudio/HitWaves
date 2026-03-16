using System;
using System.Collections.Generic;
using UnityEngine;
using HitWaves.Core.Floor;
using HitWaves.Entity.AI;
using HitWaves.Entity.AI.Boss;

namespace HitWaves.Core.Game
{
    /// <summary>
    /// 스폰 풀 항목. 프리팹 + 가중치로 출현 확률 결정.
    /// </summary>
    [Serializable]
    public struct SpawnEntry
    {
        [Tooltip("적 프리팹 (EntityBrain + StatHandler 필요)")]
        public GameObject Prefab;

        [Tooltip("출현 가중치 (높을수록 자주 등장)")]
        [Min(1)]
        public int Weight;

        [Tooltip("방당 최대 스폰 수 (0 = 무제한)")]
        [Min(0)]
        public int MaxPerRoom;
    }

    public class EnemySpawner : MonoBehaviour
    {
        private const string LOG_TAG = "EnemySpawner";

        [Header("적 스폰 풀")]
        [Tooltip("스폰할 적 목록 (가중치 기반 랜덤 선택)")]
        [SerializeField] private SpawnEntry[] _spawnPool;

        [Header("스폰 설정")]
        [Tooltip("벽에서 최소 거리 (월드 유닛)")]
        [Min(0.5f)]
        [SerializeField] private float _wallMargin = 2f;

        [Tooltip("플레이어로부터 최소 거리 (월드 유닛)")]
        [Min(1f)]
        [SerializeField] private float _playerMinDistance = 3f;

        [Tooltip("스폰 위치 결정 최대 시도 횟수")]
        [Min(5)]
        [SerializeField] private int _maxPlacementAttempts = 30;

        private List<GameObject> _spawnedEnemies = new List<GameObject>();
        private Transform _enemyContainer;
        private bool _roomActive;
        private int _totalWeight;

        public event Action OnRoomCleared;

        private void Awake()
        {
            _totalWeight = 0;
            if (_spawnPool != null)
            {
                for (int i = 0; i < _spawnPool.Length; i++)
                {
                    _totalWeight += _spawnPool[i].Weight;
                }
            }
        }

        /// <summary>
        /// 방에 적을 스폰한다. 이미 클리어된 방이면 스킵.
        /// 적 수가 0이면 즉시 클리어 처리.
        /// </summary>
        public void SpawnForRoom(RoomData room, Transform playerTransform)
        {
            ClearEnemies();

            if (room.IsCleared)
            {
                DebugLogger.Log(LOG_TAG,
                    $"SpawnForRoom — 방 #{room.Id} 이미 클리어됨, 스킵", this);
                return;
            }

            // 보스방: BossData 프리팹을 방 중심에 스폰
            if (room.Label == RoomLabel.Boss && room.BossData != null)
            {
                SpawnBoss(room, playerTransform);
                return;
            }

            if (_spawnPool == null || _spawnPool.Length == 0 || _totalWeight <= 0)
            {
                DebugLogger.LogWarning(LOG_TAG,
                    "SpawnForRoom — 스폰 풀이 비어있음", this);
                room.IsCleared = true;
                OnRoomCleared?.Invoke();
                return;
            }

            int enemyCount = 0;
            if (room.MaxEnemyCount > 0)
            {
                enemyCount = UnityEngine.Random.Range(
                    room.MinEnemyCount, room.MaxEnemyCount + 1);
            }

            if (enemyCount <= 0)
            {
                DebugLogger.Log(LOG_TAG,
                    $"SpawnForRoom — 방 #{room.Id} [{room.Label}] 적 수 0, 즉시 클리어", this);
                room.IsCleared = true;
                OnRoomCleared?.Invoke();
                return;
            }

            _enemyContainer = new GameObject($"Enemies_Room{room.Id}").transform;
            _roomActive = true;

            Vector2 playerPos = playerTransform.position;

            int[] spawnCounts = new int[_spawnPool.Length];

            for (int i = 0; i < enemyCount; i++)
            {
                int index = PickRandomIndex(spawnCounts);
                if (index < 0) break; // 모든 종류가 최대치 도달

                spawnCounts[index]++;
                GameObject prefab = _spawnPool[index].Prefab;
                Vector2 spawnPos = FindSpawnPosition(room, playerPos);
                GameObject enemy = Instantiate(prefab, spawnPos,
                    Quaternion.identity, _enemyContainer);
                enemy.name = $"{prefab.name}_{room.Id}_{i}";

                EntityBrain brain = enemy.GetComponent<EntityBrain>();
                if (brain != null)
                {
                    brain.SetTarget(playerTransform);
                }

                _spawnedEnemies.Add(enemy);
            }

            DebugLogger.Log(LOG_TAG,
                $"SpawnForRoom — 방 #{room.Id} [{room.Label}], {enemyCount}마리 스폰", this);
        }

        /// <summary>
        /// 외부에서 스폰된 적을 등록한다. (예: SummonBehavior)
        /// 등록된 적도 전멸 감지 대상에 포함된다.
        /// </summary>
        public void RegisterEnemy(GameObject enemy)
        {
            if (enemy == null) return;

            _spawnedEnemies.Add(enemy);
            _roomActive = true;

            DebugLogger.Log(LOG_TAG,
                $"RegisterEnemy — {enemy.name}, 현재 {_spawnedEnemies.Count}마리 추적 중", this);
        }

        /// <summary>
        /// 보스를 방 중심에 스폰한다.
        /// </summary>
        private void SpawnBoss(RoomData room, Transform playerTransform)
        {
            _enemyContainer = new GameObject($"Boss_Room{room.Id}").transform;
            _roomActive = true;

            GameObject prefab = room.BossData.Prefab;
            Vector2 spawnPos = room.WorldCenter;

            GameObject boss = Instantiate(prefab, spawnPos,
                Quaternion.identity, _enemyContainer);
            boss.name = $"{prefab.name}_Boss_{room.Id}";

            EntityBrain brain = boss.GetComponent<EntityBrain>();
            if (brain != null)
            {
                brain.SetTarget(playerTransform);
            }

            SummonBehavior summon = boss.GetComponent<SummonBehavior>();
            if (summon != null)
            {
                summon.SetRoomBounds(room.WorldRect);
            }

            _spawnedEnemies.Add(boss);

            DebugLogger.Log(LOG_TAG,
                $"SpawnBoss — 방 #{room.Id}, 보스: {room.BossData.DisplayName}", this);
        }

        /// <summary>
        /// 가중치 기반으로 스폰 풀에서 인덱스를 랜덤 선택한다.
        /// MaxPerRoom에 도달한 종류는 제외. 모두 소진되면 -1 반환.
        /// </summary>
        private int PickRandomIndex(int[] spawnCounts)
        {
            int availableWeight = 0;
            for (int i = 0; i < _spawnPool.Length; i++)
            {
                if (_spawnPool[i].MaxPerRoom > 0 &&
                    spawnCounts[i] >= _spawnPool[i].MaxPerRoom)
                    continue;

                availableWeight += _spawnPool[i].Weight;
            }

            if (availableWeight <= 0) return -1;

            int roll = UnityEngine.Random.Range(0, availableWeight);
            int cumulative = 0;

            for (int i = 0; i < _spawnPool.Length; i++)
            {
                if (_spawnPool[i].MaxPerRoom > 0 &&
                    spawnCounts[i] >= _spawnPool[i].MaxPerRoom)
                    continue;

                cumulative += _spawnPool[i].Weight;
                if (roll < cumulative)
                {
                    return i;
                }
            }

            return -1;
        }

        private void Update()
        {
            if (!_roomActive) return;

            CheckEnemiesAlive();
        }

        /// <summary>
        /// 살아있는 적이 있는지 확인한다. 전멸 시 OnRoomCleared 발생.
        /// </summary>
        private void CheckEnemiesAlive()
        {
            for (int i = _spawnedEnemies.Count - 1; i >= 0; i--)
            {
                if (_spawnedEnemies[i] == null)
                {
                    _spawnedEnemies.RemoveAt(i);
                }
            }

            if (_spawnedEnemies.Count == 0)
            {
                _roomActive = false;

                DebugLogger.Log(LOG_TAG, "적 전멸 — 방 클리어!", this);
                OnRoomCleared?.Invoke();
            }
        }

        /// <summary>
        /// 방 내부에서 벽/플레이어와 거리를 두고 스폰 위치를 결정한다.
        /// </summary>
        private Vector2 FindSpawnPosition(RoomData room, Vector2 playerPos)
        {
            Rect rect = room.WorldRect;
            float minX = rect.xMin + _wallMargin;
            float maxX = rect.xMax - _wallMargin;
            float minY = rect.yMin + _wallMargin;
            float maxY = rect.yMax - _wallMargin;

            for (int attempt = 0; attempt < _maxPlacementAttempts; attempt++)
            {
                float x = UnityEngine.Random.Range(minX, maxX);
                float y = UnityEngine.Random.Range(minY, maxY);
                Vector2 candidate = new Vector2(x, y);

                if (Vector2.Distance(candidate, playerPos) >= _playerMinDistance)
                {
                    return candidate;
                }
            }

            // 실패 시 방 중심에서 플레이어 반대쪽에 배치 (방 경계 내로 클램핑)
            Vector2 awayFromPlayer = (room.WorldCenter - playerPos).normalized;
            Vector2 fallback = room.WorldCenter + awayFromPlayer * _playerMinDistance;
            fallback.x = Mathf.Clamp(fallback.x, minX, maxX);
            fallback.y = Mathf.Clamp(fallback.y, minY, maxY);
            return fallback;
        }

        /// <summary>
        /// 현재 스폰된 적을 모두 제거한다.
        /// </summary>
        public void ClearEnemies()
        {
            _roomActive = false;

            for (int i = 0; i < _spawnedEnemies.Count; i++)
            {
                if (_spawnedEnemies[i] != null)
                {
                    Destroy(_spawnedEnemies[i]);
                }
            }

            _spawnedEnemies.Clear();

            if (_enemyContainer != null)
            {
                Destroy(_enemyContainer.gameObject);
                _enemyContainer = null;
            }
        }
    }
}
