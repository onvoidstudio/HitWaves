using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using HitWaves.Core.Floor;

namespace HitWaves.Core.Game
{
    public class GameManager : MonoBehaviour
    {
        private const string LOG_TAG = "GameManager";

        [Header("참조")]
        [Tooltip("층 생성기")]
        [SerializeField] private FloorGenerator _floorGenerator;

        [Tooltip("층 렌더러")]
        [SerializeField] private FloorRenderer _floorRenderer;

        [Tooltip("벽 생성기")]
        [SerializeField] private WallBuilder _wallBuilder;

        [Tooltip("문 생성기")]
        [SerializeField] private DoorBuilder _doorBuilder;

        [Tooltip("방 전환 매니저")]
        [SerializeField] private RoomTransitionManager _roomTransitionManager;

        [Header("플레이어")]
        [Tooltip("플레이어의 HealthHandler")]
        [SerializeField] private HealthHandler _playerHealth;

        [Header("게임 루프")]
        [Tooltip("플레이어 사망 후 재시작 대기 시간 (초)")]
        [Min(0f)]
        [SerializeField] private float _deathRestartDelay = 1.5f;

        [Tooltip("보스 클리어 후 재시작 대기 시간 (초)")]
        [Min(0f)]
        [SerializeField] private float _bossRestartDelay = 2f;

        private List<RoomData> _currentRooms;
        private List<DoorData> _currentDoors;
        private bool _isRestarting;

        private void OnEnable()
        {
            if (_playerHealth != null)
            {
                _playerHealth.OnDeath += HandlePlayerDeath;
            }

            if (_roomTransitionManager != null)
            {
                _roomTransitionManager.OnBossRoomCleared += HandleBossCleared;
            }
        }

        private void OnDisable()
        {
            if (_playerHealth != null)
            {
                _playerHealth.OnDeath -= HandlePlayerDeath;
            }

            if (_roomTransitionManager != null)
            {
                _roomTransitionManager.OnBossRoomCleared -= HandleBossCleared;
            }
        }

        /// <summary>
        /// 게임 시작 시 층을 생성하고 렌더링한다.
        /// </summary>
        private void Start()
        {
            GenerateFloor();
        }

        /// <summary>
        /// 층 생성 + 렌더링. 외부에서도 호출 가능 (재생성 용도).
        /// </summary>
        public void GenerateFloor()
        {
            DebugLogger.Log(LOG_TAG, "GenerateFloor 시작", this);

            _currentRooms = _floorGenerator.Generate();
            _currentDoors = _doorBuilder.BuildDoors(_currentRooms);
            _floorRenderer.RenderAll(_currentRooms);
            _wallBuilder.BuildWalls(_currentRooms, _currentDoors);
            _doorBuilder.BuildDoorTriggers(_currentDoors,
                _roomTransitionManager.PlayerTransform,
                _roomTransitionManager.TransitionTo,
                _wallBuilder.WallThickness);

            RoomData startRoom = FindStartRoom();
            if (startRoom != null)
            {
                _roomTransitionManager.SetInitialRoom(startRoom);
            }

            DebugLogger.Log(LOG_TAG,
                $"GenerateFloor 완료 — {_currentRooms.Count}개 방, 시드: {_floorGenerator.Seed}", this);
        }

        /// <summary>
        /// Start 라벨을 가진 첫 번째 방을 찾는다.
        /// 없으면 첫 번째 방을 반환한다.
        /// </summary>
        private RoomData FindStartRoom()
        {
            for (int i = 0; i < _currentRooms.Count; i++)
            {
                if (_currentRooms[i].Label == RoomLabel.Start)
                {
                    DebugLogger.Log(LOG_TAG,
                        $"FindStartRoom — Start 방 #{_currentRooms[i].Id} 발견", this);
                    return _currentRooms[i];
                }
            }

            DebugLogger.LogWarning(LOG_TAG, "FindStartRoom — Start 방을 찾을 수 없음, 첫 번째 방 사용", this);
            return _currentRooms.Count > 0 ? _currentRooms[0] : null;
        }

        /// <summary>
        /// 플레이어 사망 시 호출. 딜레이 후 씬 리로드.
        /// </summary>
        private void HandlePlayerDeath()
        {
            if (_isRestarting) return;

            DebugLogger.Log(LOG_TAG, "HandlePlayerDeath — 플레이어 사망, 재시작 예정", this);
            StartCoroutine(RestartAfterDelay(_deathRestartDelay));
        }

        /// <summary>
        /// 보스 방 클리어 시 호출. 딜레이 후 씬 리로드 (추후 다음 층 전환으로 변경).
        /// </summary>
        private void HandleBossCleared()
        {
            if (_isRestarting) return;

            DebugLogger.Log(LOG_TAG, "HandleBossCleared — 보스 클리어, 재시작 예정", this);
            StartCoroutine(RestartAfterDelay(_bossRestartDelay));
        }

        private IEnumerator RestartAfterDelay(float delay)
        {
            _isRestarting = true;

            DebugLogger.Log(LOG_TAG,
                $"RestartAfterDelay — {delay}초 후 씬 리로드", this);

            yield return new WaitForSeconds(delay);

            string sceneName = SceneManager.GetActiveScene().name;
            DebugLogger.Log(LOG_TAG,
                $"씬 리로드 — {sceneName}", this);
            SceneManager.LoadScene(sceneName);
        }
    }
}
