using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using HitWaves.Core.Floor;
using HitWaves.Entity.Player;
using HitWaves.UI;

namespace HitWaves.Core.Game
{
    public class RoomTransitionManager : MonoBehaviour
    {
        private const string LOG_TAG = "RoomTransitionManager";

        [Header("참조")]
        [Tooltip("카메라 컨트롤러")]
        [SerializeField] private CameraController _cameraController;

        [Tooltip("플레이어 Transform")]
        [SerializeField] private Transform _playerTransform;

        [Tooltip("적 스포너")]
        [SerializeField] private EnemySpawner _enemySpawner;

        [Tooltip("문 생성기 (잠금/해제용)")]
        [SerializeField] private DoorBuilder _doorBuilder;

        [Tooltip("바닥 렌더러 (방별 표시/숨김)")]
        [SerializeField] private FloorRenderer _floorRenderer;

        [Tooltip("보스 인트로 UI (없으면 연출 생략)")]
        [SerializeField] private BossIntroUI _bossIntroUI;

        [Header("페이드 설정")]
        [Tooltip("페이드 인/아웃 각각의 시간 (초)")]
        [Min(0.01f)]
        [SerializeField] private float _fadeDuration = 0.2f;

        [Header("텔레포트 설정")]
        [Tooltip("문 중심에서 목표 방 방향으로 이동시킬 거리")]
        [Min(0.5f)]
        [SerializeField] private float _teleportOffset = 2f;

        private RoomData _currentRoom;
        private bool _isTransitioning;
        private Image _fadeImage;

        public Transform PlayerTransform => _playerTransform;
        public RoomData CurrentRoom => _currentRoom;

        public event Action OnBossRoomCleared;
        public event Action<RoomData> OnRoomEntered;
        public event Action<RoomData> OnRoomCleared;

        private void Awake()
        {
            CreateFadeUI();
        }

        private void OnEnable()
        {
            if (_enemySpawner != null)
            {
                _enemySpawner.OnRoomCleared += HandleRoomCleared;
            }
        }

        private void OnDisable()
        {
            if (_enemySpawner != null)
            {
                _enemySpawner.OnRoomCleared -= HandleRoomCleared;
            }
        }

        /// <summary>
        /// 게임 시작 시 초기 방을 설정한다. 카메라 바운드 + Snap.
        /// </summary>
        public void SetInitialRoom(RoomData room)
        {
            _currentRoom = room;
            _cameraController.SetTarget(_playerTransform);
            UpdateCameraBounds(room);
            _cameraController.SnapToTarget();

            if (_floorRenderer != null)
            {
                _floorRenderer.ShowRoom(room.Id);
            }

            EnterRoom(room);

            DebugLogger.Log(LOG_TAG, $"초기 방 설정 — #{room.Id} [{room.Label}]", this);
        }

        /// <summary>
        /// 문을 통해 다음 방으로 전환한다.
        /// _isTransitioning 가드로 중복 전환 방지.
        /// </summary>
        public void TransitionTo(DoorData door)
        {
            if (_isTransitioning)
            {
                DebugLogger.Log(LOG_TAG, "TransitionTo — 전환 중 중복 요청 무시", this);
                return;
            }
            if (_currentRoom == null || door == null) return;

            RoomData targetRoom = door.GetOtherRoom(_currentRoom);
            if (targetRoom == null || targetRoom == _currentRoom) return;

            DebugLogger.Log(LOG_TAG,
                $"TransitionTo — 문 #{door.RoomA.Id} ↔ #{door.RoomB.Id}, " +
                $"현재: #{_currentRoom.Id} → 목표: #{targetRoom.Id}", this);
            StartCoroutine(TransitionCoroutine(targetRoom, door));
        }

        /// <summary>
        /// 페이드아웃 → 카메라 전환 → 페이드인 코루틴.
        /// 플레이어 입력/속도는 유지된다 (끊김 없음).
        /// </summary>
        private IEnumerator TransitionCoroutine(RoomData targetRoom, DoorData door)
        {
            _isTransitioning = true;

            DebugLogger.Log(LOG_TAG,
                $"전환 시작 — #{_currentRoom.Id} → #{targetRoom.Id} [{targetRoom.Label}]", this);

            // 페이드 아웃
            yield return FadeCoroutine(0f, 1f);

            // 암전 중: 플레이어 텔레포트 + 카메라 전환 + 바닥 교체
            TeleportPlayerThroughDoor(door, targetRoom);
            _currentRoom = targetRoom;
            UpdateCameraBounds(targetRoom);
            _cameraController.SnapToTarget();

            if (_floorRenderer != null)
            {
                _floorRenderer.ShowRoom(targetRoom.Id);
            }

            // 페이드 인
            yield return FadeCoroutine(1f, 0f);

            _isTransitioning = false;

            EnterRoom(targetRoom);

            DebugLogger.Log(LOG_TAG,
                $"전환 완료 — 현재 방: #{targetRoom.Id} [{targetRoom.Label}]", this);
        }

        /// <summary>
        /// 방 입장 처리: 방문 마킹 → 미니맵 갱신 → 문 잠금 → 적 스폰.
        /// 적 수 0이면 EnemySpawner가 즉시 클리어 이벤트를 발생시킨다.
        /// </summary>
        private void EnterRoom(RoomData room)
        {
            room.IsVisited = true;
            OnRoomEntered?.Invoke(room);

            if (room.IsCleared)
            {
                DebugLogger.Log(LOG_TAG,
                    $"EnterRoom — 방 #{room.Id} 이미 클리어됨, 문 잠금 없음", this);
                return;
            }

            if (_doorBuilder != null)
            {
                _doorBuilder.LockDoorsForRoom(room);
            }

            // 보스방: 인트로 UI 표시 후 스폰
            if (room.Label == RoomLabel.Boss && room.BossData != null
                && _bossIntroUI != null)
            {
                StartCoroutine(BossIntroThenSpawn(room));
                return;
            }

            if (_enemySpawner != null)
            {
                _enemySpawner.SpawnForRoom(room, _playerTransform);
            }
        }

        /// <summary>
        /// 보스 인트로 연출 후 스폰하는 코루틴.
        /// </summary>
        private IEnumerator BossIntroThenSpawn(RoomData room)
        {
            // 인트로 중 플레이어 입력 차단
            PlayerController playerController = _playerTransform != null
                ? _playerTransform.GetComponent<PlayerController>()
                : null;
            playerController?.SetInputEnabled(false);

            bool introFinished = false;

            void HandleIntroFinished()
            {
                introFinished = true;
            }

            _bossIntroUI.OnIntroFinished += HandleIntroFinished;
            _bossIntroUI.Show(room.BossData.DisplayName, room.BossData.Title,
                room.BossData.IntroSprite);

            while (!introFinished)
            {
                yield return null;
            }

            _bossIntroUI.OnIntroFinished -= HandleIntroFinished;

            // 인트로 종료 → 입력 복원 + 보스 스폰
            playerController?.SetInputEnabled(true);

            if (_enemySpawner != null)
            {
                _enemySpawner.SpawnForRoom(room, _playerTransform);
            }

            DebugLogger.Log(LOG_TAG,
                $"BossIntroThenSpawn — 인트로 완료, 보스 스폰: {room.BossData.DisplayName}", this);
        }

        /// <summary>
        /// 적 전멸 시 호출. 현재 방 클리어 처리 + 문 해제.
        /// </summary>
        private void HandleRoomCleared()
        {
            if (_currentRoom == null) return;

            _currentRoom.IsCleared = true;

            if (_doorBuilder != null)
            {
                _doorBuilder.UnlockDoorsForRoom(_currentRoom);
            }

            OnRoomCleared?.Invoke(_currentRoom);

            DebugLogger.Log(LOG_TAG,
                $"HandleRoomCleared — 방 #{_currentRoom.Id} 클리어, 문 해제", this);

            if (_currentRoom.Label == RoomLabel.Boss)
            {
                DebugLogger.Log(LOG_TAG,
                    $"HandleRoomCleared — 보스 방 #{_currentRoom.Id} 클리어!", this);
                OnBossRoomCleared?.Invoke();
            }
        }

        /// <summary>
        /// 문을 통해 플레이어를 목표 방 안쪽으로 텔레포트한다.
        /// 문 중심에서 목표 방 중심 방향으로 _teleportOffset만큼 이동.
        /// </summary>
        private void TeleportPlayerThroughDoor(DoorData door, RoomData targetRoom)
        {
            Vector2 doorPos = door.WorldPosition;
            Vector2 direction = (targetRoom.WorldCenter - doorPos).normalized;
            Vector2 teleportPos = doorPos + direction * _teleportOffset;

            _playerTransform.position = new Vector3(
                teleportPos.x, teleportPos.y, _playerTransform.position.z);

            DebugLogger.Log(LOG_TAG,
                $"TeleportPlayerThroughDoor — door: {doorPos}, " +
                $"target: #{targetRoom.Id}, pos: {teleportPos}", this);
        }

        /// <summary>
        /// 페이드 알파를 from에서 to로 보간한다.
        /// </summary>
        private IEnumerator FadeCoroutine(float from, float to)
        {
            float elapsed = 0f;
            Color color = _fadeImage.color;

            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _fadeDuration);
                color.a = Mathf.Lerp(from, to, t);
                _fadeImage.color = color;
                yield return null;
            }

            color.a = to;
            _fadeImage.color = color;
        }

        /// <summary>
        /// 방 데이터로 카메라 바운드를 갱신한다.
        /// </summary>
        private void UpdateCameraBounds(RoomData room)
        {
            Vector2 halfSize = new Vector2(room.Width * 0.5f, room.Height * 0.5f);
            _cameraController.SetRoomBounds(room.WorldCenter, halfSize);

            DebugLogger.Log(LOG_TAG,
                $"UpdateCameraBounds — 방 #{room.Id}, center: {room.WorldCenter}, " +
                $"halfSize: {halfSize}", this);
        }

        /// <summary>
        /// 페이드용 UI를 런타임에 생성한다.
        /// Canvas(ScreenSpaceOverlay) + 검은 Image(알파 0).
        /// </summary>
        private void CreateFadeUI()
        {
            GameObject canvasGo = new GameObject("FadeCanvas");
            canvasGo.transform.SetParent(transform);

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            GameObject imageGo = new GameObject("FadeImage");
            imageGo.transform.SetParent(canvasGo.transform);

            _fadeImage = imageGo.AddComponent<Image>();
            _fadeImage.color = new Color(0f, 0f, 0f, 0f);
            _fadeImage.raycastTarget = false;

            RectTransform rt = _fadeImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            DebugLogger.Log(LOG_TAG, "페이드 UI 생성 완료", this);
        }
    }
}
