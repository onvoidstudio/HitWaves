using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HitWaves.UI
{
    public class BossIntroUI : MonoBehaviour
    {
        private const string LOG_TAG = "BossIntroUI";

        [Header("UI 참조")]
        [Tooltip("보스 이름 텍스트")]
        [SerializeField] private TextMeshProUGUI _nameText;

        [Tooltip("보스 칭호 텍스트")]
        [SerializeField] private TextMeshProUGUI _titleText;

        [Tooltip("보스 이미지")]
        [SerializeField] private Image _bossImage;

        [Header("배경 설정")]
        [Tooltip("배경 오버레이 색상 (알파로 불투명도 조절)")]
        [SerializeField] private Color _overlayColor = new Color(0f, 0f, 0f, 0.85f);

        [Header("이미지 설정")]
        [Tooltip("보스 이미지 크기 (가로 × 세로, 기준 해상도 1920×1080 기준)")]
        [SerializeField] private Vector2 _imageSize = new Vector2(500f, 500f);

        [Tooltip("보스 이미지 위치 오프셋 (중앙 기준)")]
        [SerializeField] private Vector2 _imageOffset = new Vector2(250f, 0f);

        [Header("텍스트 설정")]
        [Tooltip("텍스트 영역 위치 오프셋 (중앙 기준)")]
        [SerializeField] private Vector2 _textOffset = new Vector2(-250f, -80f);

        [Header("연출 설정")]
        [Tooltip("페이드 인 시간 (초)")]
        [Min(0.01f)]
        [SerializeField] private float _fadeInDuration = 0.5f;

        [Tooltip("표시 유지 시간 (초)")]
        [Min(0.1f)]
        [SerializeField] private float _holdDuration = 1.5f;

        [Tooltip("페이드 아웃 시간 (초)")]
        [Min(0.01f)]
        [SerializeField] private float _fadeOutDuration = 0.5f;

        [Header("슬라이드 설정")]
        [Tooltip("보스 이미지 슬라이드 거리 (오른쪽에서 진입)")]
        [SerializeField] private float _imageSlideDistance = 200f;

        [Tooltip("텍스트 슬라이드 거리 (왼쪽에서 진입)")]
        [SerializeField] private float _textSlideDistance = 150f;

        [Tooltip("텍스트 슬라이드 시작 딜레이 (초)")]
        [Min(0f)]
        [SerializeField] private float _textDelay = 0.15f;

        private CanvasGroup _canvasGroup;
        private Canvas _canvas;
        private Image _overlayImage;

        // 슬라이드 애니메이션용 RectTransform
        private RectTransform _imageRT;
        private RectTransform _textContainerRT;
        private Vector2 _imageRestPos;
        private Vector2 _textRestPos;

        public event Action OnIntroFinished;

        private void Awake()
        {
            if (_nameText == null || _titleText == null || _bossImage == null)
            {
                CreateUI();
            }

            _canvasGroup = GetComponentInChildren<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = _canvas.gameObject.AddComponent<CanvasGroup>();
            }

            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            // 최종 위치 기록
            _imageRestPos = _imageRT.anchoredPosition;
            _textRestPos = _textContainerRT.anchoredPosition;

            gameObject.SetActive(false);
        }

        /// <summary>
        /// 보스 인트로 연출을 시작한다.
        /// 배경 암전 + 이미지 슬라이드 인 + 텍스트 슬라이드 인 → 유지 → 전체 페이드 아웃.
        /// </summary>
        public void Show(string displayName, string title, Sprite introSprite = null)
        {
            _nameText.text = $"{displayName} 출현!";
            _titleText.text = title;

            // 배경색 적용 (Inspector에서 변경 가능)
            if (_overlayImage != null)
            {
                _overlayImage.color = _overlayColor;
            }

            if (introSprite != null)
            {
                _bossImage.sprite = introSprite;
                _bossImage.enabled = true;
            }
            else
            {
                _bossImage.enabled = false;
            }

            gameObject.SetActive(true);
            StartCoroutine(IntroCoroutine());

            DebugLogger.Log(LOG_TAG,
                $"Show — {displayName} / {title}", this);
        }

        private IEnumerator IntroCoroutine()
        {
            // 초기 위치: 슬라이드 시작점 + 투명
            _imageRT.anchoredPosition = _imageRestPos + new Vector2(_imageSlideDistance, 0f);
            _textContainerRT.anchoredPosition = _textRestPos + new Vector2(-_textSlideDistance, 0f);
            _canvasGroup.alpha = 0f;

            // 이미지: 즉시 슬라이드 인 시작
            Coroutine imageSlide = StartCoroutine(
                SlideCoroutine(_imageRT, _imageRT.anchoredPosition, _imageRestPos, _fadeInDuration));

            // 전체 페이드 인
            Coroutine fadeIn = StartCoroutine(FadeCoroutine(0f, 1f, _fadeInDuration));

            // 텍스트: 딜레이 후 슬라이드 인
            yield return new WaitForSeconds(_textDelay);
            Coroutine textSlide = StartCoroutine(
                SlideCoroutine(_textContainerRT, _textContainerRT.anchoredPosition, _textRestPos,
                    _fadeInDuration - _textDelay));

            // 이미지 슬라이드 완료 대기
            yield return imageSlide;
            yield return fadeIn;

            // 유지
            yield return new WaitForSeconds(_holdDuration);

            // 페이드 아웃
            yield return FadeCoroutine(1f, 0f, _fadeOutDuration);

            gameObject.SetActive(false);
            OnIntroFinished?.Invoke();

            DebugLogger.Log(LOG_TAG, "인트로 연출 완료", this);
        }

        private IEnumerator FadeCoroutine(float from, float to, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _canvasGroup.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }

            _canvasGroup.alpha = to;
        }

        /// <summary>
        /// RectTransform을 from에서 to로 EaseOut 보간 이동.
        /// </summary>
        private IEnumerator SlideCoroutine(RectTransform rt, Vector2 from, Vector2 to, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // EaseOutCubic: 빠르게 들어오고 감속
                float eased = 1f - (1f - t) * (1f - t) * (1f - t);
                rt.anchoredPosition = Vector2.Lerp(from, to, eased);
                yield return null;
            }

            rt.anchoredPosition = to;
        }

        /// <summary>
        /// UI를 런타임에 생성한다.
        /// 배경 오버레이 + 보스 이미지 (중앙 오른쪽) + 텍스트 (중앙 왼쪽 하단)
        /// </summary>
        private void CreateUI()
        {
            // Canvas
            GameObject canvasGo = new GameObject("BossIntroCanvas");
            canvasGo.transform.SetParent(transform);

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 900;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            _canvasGroup = canvasGo.AddComponent<CanvasGroup>();

            // === 배경 오버레이 (전체 화면) ===
            GameObject overlayGo = new GameObject("Overlay");
            overlayGo.transform.SetParent(canvasGo.transform);
            _overlayImage = overlayGo.AddComponent<Image>();
            _overlayImage.color = _overlayColor;
            _overlayImage.raycastTarget = false;
            RectTransform overlayRT = _overlayImage.rectTransform;
            overlayRT.anchorMin = Vector2.zero;
            overlayRT.anchorMax = Vector2.one;
            overlayRT.offsetMin = Vector2.zero;
            overlayRT.offsetMax = Vector2.zero;

            // === 보스 이미지 (중앙 오른쪽) ===
            GameObject imageGo = new GameObject("BossImage");
            imageGo.transform.SetParent(canvasGo.transform);
            _bossImage = imageGo.AddComponent<Image>();
            _bossImage.preserveAspect = true;
            _bossImage.raycastTarget = false;

            _imageRT = _bossImage.rectTransform;
            _imageRT.anchorMin = new Vector2(0.5f, 0.5f);
            _imageRT.anchorMax = new Vector2(0.5f, 0.5f);
            _imageRT.anchoredPosition = _imageOffset;
            _imageRT.sizeDelta = _imageSize;

            // === 텍스트 컨테이너 (중앙 왼쪽 하단) ===
            GameObject textContainer = new GameObject("TextContainer");
            textContainer.transform.SetParent(canvasGo.transform);
            _textContainerRT = textContainer.AddComponent<RectTransform>();
            _textContainerRT.anchorMin = new Vector2(0.5f, 0.5f);
            _textContainerRT.anchorMax = new Vector2(0.5f, 0.5f);
            _textContainerRT.anchoredPosition = _textOffset;
            _textContainerRT.sizeDelta = new Vector2(500f, 150f);

            // 칭호 텍스트 (위)
            GameObject titleGo = new GameObject("TitleText");
            titleGo.transform.SetParent(textContainer.transform);
            _titleText = titleGo.AddComponent<TextMeshProUGUI>();
            _titleText.alignment = TextAlignmentOptions.Left;
            _titleText.fontSize = 24f;
            _titleText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            _titleText.raycastTarget = false;
            RectTransform titleRT = _titleText.rectTransform;
            titleRT.anchorMin = new Vector2(0f, 0.5f);
            titleRT.anchorMax = new Vector2(1f, 1f);
            titleRT.offsetMin = Vector2.zero;
            titleRT.offsetMax = Vector2.zero;

            // 이름 텍스트 (아래)
            GameObject nameGo = new GameObject("NameText");
            nameGo.transform.SetParent(textContainer.transform);
            _nameText = nameGo.AddComponent<TextMeshProUGUI>();
            _nameText.alignment = TextAlignmentOptions.Left;
            _nameText.fontSize = 42f;
            _nameText.color = Color.white;
            _nameText.fontStyle = FontStyles.Bold;
            _nameText.raycastTarget = false;
            RectTransform nameRT = _nameText.rectTransform;
            nameRT.anchorMin = new Vector2(0f, 0f);
            nameRT.anchorMax = new Vector2(1f, 0.5f);
            nameRT.offsetMin = Vector2.zero;
            nameRT.offsetMax = Vector2.zero;

            DebugLogger.Log(LOG_TAG, "UI 런타임 생성 완료", this);
        }
    }
}
