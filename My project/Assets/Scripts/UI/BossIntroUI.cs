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

        private CanvasGroup _canvasGroup;
        private Canvas _canvas;

        public event Action OnIntroFinished;

        private void Awake()
        {
            if (_nameText == null || _titleText == null)
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

            gameObject.SetActive(false);
        }

        /// <summary>
        /// 보스 인트로 연출을 시작한다.
        /// 페이드 인 → 유지 → 페이드 아웃 → OnIntroFinished 이벤트.
        /// </summary>
        public void Show(string displayName, string title)
        {
            _nameText.text = displayName;
            _titleText.text = title;

            gameObject.SetActive(true);
            StartCoroutine(IntroCoroutine());

            DebugLogger.Log(LOG_TAG,
                $"Show — {displayName} / {title}", this);
        }

        private IEnumerator IntroCoroutine()
        {
            // 페이드 인
            yield return FadeCoroutine(0f, 1f, _fadeInDuration);

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
        /// UI를 런타임에 생성한다.
        /// </summary>
        private void CreateUI()
        {
            // Canvas
            GameObject canvasGo = new GameObject("BossIntroCanvas");
            canvasGo.transform.SetParent(transform);

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 900;

            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            _canvasGroup = canvasGo.AddComponent<CanvasGroup>();

            // 컨테이너 (화면 중앙)
            GameObject container = new GameObject("Container");
            container.transform.SetParent(canvasGo.transform);
            RectTransform containerRT = container.AddComponent<RectTransform>();
            containerRT.anchorMin = new Vector2(0.5f, 0.5f);
            containerRT.anchorMax = new Vector2(0.5f, 0.5f);
            containerRT.sizeDelta = new Vector2(600f, 200f);
            containerRT.anchoredPosition = Vector2.zero;

            // 칭호 텍스트 (위)
            GameObject titleGo = new GameObject("TitleText");
            titleGo.transform.SetParent(container.transform);
            _titleText = titleGo.AddComponent<TextMeshProUGUI>();
            _titleText.alignment = TextAlignmentOptions.Center;
            _titleText.fontSize = 24f;
            _titleText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            RectTransform titleRT = _titleText.rectTransform;
            titleRT.anchorMin = new Vector2(0f, 0.5f);
            titleRT.anchorMax = new Vector2(1f, 1f);
            titleRT.offsetMin = Vector2.zero;
            titleRT.offsetMax = Vector2.zero;

            // 이름 텍스트 (아래)
            GameObject nameGo = new GameObject("NameText");
            nameGo.transform.SetParent(container.transform);
            _nameText = nameGo.AddComponent<TextMeshProUGUI>();
            _nameText.alignment = TextAlignmentOptions.Center;
            _nameText.fontSize = 48f;
            _nameText.color = Color.white;
            _nameText.fontStyle = FontStyles.Bold;
            RectTransform nameRT = _nameText.rectTransform;
            nameRT.anchorMin = new Vector2(0f, 0f);
            nameRT.anchorMax = new Vector2(1f, 0.5f);
            nameRT.offsetMin = Vector2.zero;
            nameRT.offsetMax = Vector2.zero;

            DebugLogger.Log(LOG_TAG, "UI 런타임 생성 완료", this);
        }
    }
}
