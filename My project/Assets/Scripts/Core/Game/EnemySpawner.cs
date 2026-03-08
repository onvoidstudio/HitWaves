using System;
using System.Collections.Generic;
using UnityEngine;
using HitWaves.Core.Floor;
using HitWaves.Entity.Enemy;

namespace HitWaves.Core.Game
{
    public class EnemySpawner : MonoBehaviour
    {
        private const string LOG_TAG = "EnemySpawner";

        [Header("적 프리팹")]
        [Tooltip("스폰할 적 프리팹 (EnemyController + StatHandler 필요)")]
        [SerializeField] private GameObject _enemyPrefab;

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

        public event Action OnRoomCleared;

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

            if (_enemyPrefab == null)
            {
                DebugLogger.LogWarning(LOG_TAG,
                    "SpawnForRoom — 적 프리팹이 할당되지 않음", this);
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

            for (int i = 0; i < enemyCount; i++)
            {
                Vector2 spawnPos = FindSpawnPosition(room, playerPos);
                GameObject enemy = Instantiate(_enemyPrefab, spawnPos,
                    Quaternion.identity, _enemyContainer);
                enemy.name = $"Enemy_{room.Id}_{i}";

                EnemyController controller = enemy.GetComponent<EnemyController>();
                if (controller != null)
                {
                    controller.Initialize(playerTransform);
                }

                _spawnedEnemies.Add(enemy);
            }

            DebugLogger.Log(LOG_TAG,
                $"SpawnForRoom — 방 #{room.Id} [{room.Label}], {enemyCount}마리 스폰", this);
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

            // 실패 시 방 중심에서 플레이어 반대쪽에 배치
            Vector2 awayFromPlayer = (room.WorldCenter - playerPos).normalized;
            return room.WorldCenter + awayFromPlayer * _playerMinDistance;
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
