using System.Collections;
using UnityEngine;

namespace HitWaves.Core
{
    public enum HitEffectMode
    {
        Blink,
        WhiteFlash
    }

    [RequireComponent(typeof(HealthHandler))]
    public class HitEffect : MonoBehaviour
    {
        private const string LOG_TAG = "HitEffect";

        [Header("이펙트 모드")]
        [Tooltip("Blink: 깜빡임 (플레이어용), WhiteFlash: 흰색 플래시 (적용)")]
        [SerializeField] private HitEffectMode _mode = HitEffectMode.Blink;

        [Header("깜빡임 설정 (Blink)")]
        [Tooltip("깜빡이는 횟수")]
        [Min(1)]
        [SerializeField] private int _blinkCount = 3;

        [Tooltip("깜빡임 간격 (초)")]
        [Min(0.01f)]
        [SerializeField] private float _blinkInterval = 0.1f;

        [Header("흰색 플래시 설정 (WhiteFlash)")]
        [Tooltip("흰색 플래시 지속 시간 (초)")]
        [Min(0.01f)]
        [SerializeField] private float _flashDuration = 0.08f;

        private HealthHandler _healthHandler;
        private SpriteRenderer _spriteRenderer;
        private Color _originalColor;
        private Coroutine _effectCoroutine;

        private void Awake()
        {
            _healthHandler = GetComponent<HealthHandler>();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Start()
        {
            if (_spriteRenderer != null)
            {
                _originalColor = _spriteRenderer.color;
            }
        }

        private void OnEnable()
        {
            if (_healthHandler != null)
            {
                _healthHandler.OnDamaged += PlayEffect;
            }
        }

        private void OnDisable()
        {
            if (_healthHandler != null)
            {
                _healthHandler.OnDamaged -= PlayEffect;
            }
        }

        private void PlayEffect()
        {
            if (_spriteRenderer == null) return;

            if (_effectCoroutine != null)
            {
                StopCoroutine(_effectCoroutine);
                RestoreVisuals();
            }

            switch (_mode)
            {
                case HitEffectMode.Blink:
                    _effectCoroutine = StartCoroutine(BlinkCoroutine());
                    break;
                case HitEffectMode.WhiteFlash:
                    _effectCoroutine = StartCoroutine(WhiteFlashCoroutine());
                    break;
            }
        }

        /// <summary>
        /// SpriteRenderer를 on/off 반복하여 깜빡임 연출.
        /// </summary>
        private IEnumerator BlinkCoroutine()
        {
            for (int i = 0; i < _blinkCount; i++)
            {
                _spriteRenderer.enabled = false;
                yield return new WaitForSeconds(_blinkInterval);
                _spriteRenderer.enabled = true;
                yield return new WaitForSeconds(_blinkInterval);
            }

            _effectCoroutine = null;
        }

        /// <summary>
        /// 스프라이트 색상을 흰색으로 바꿨다가 복구.
        /// </summary>
        private IEnumerator WhiteFlashCoroutine()
        {
            _spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(_flashDuration);
            _spriteRenderer.color = _originalColor;

            _effectCoroutine = null;
        }

        private void RestoreVisuals()
        {
            if (_spriteRenderer == null) return;

            _spriteRenderer.enabled = true;
            _spriteRenderer.color = _originalColor;
        }

        private void OnDestroy()
        {
            if (_effectCoroutine != null)
            {
                StopCoroutine(_effectCoroutine);
                RestoreVisuals();
            }
        }
    }
}
