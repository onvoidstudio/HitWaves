using System.Collections;
using UnityEngine;

namespace HitWaves.Core
{
    public enum HitEffectMode
    {
        Blink,
        WhiteFlash,
        RedFlash
    }

    [RequireComponent(typeof(HealthHandler))]
    public class HitEffect : MonoBehaviour
    {
        private const string LOG_TAG = "HitEffect";

        private static readonly int FLASH_AMOUNT = Shader.PropertyToID("_FlashAmount");
        private static readonly int FILL_PHASE = Shader.PropertyToID("_FillPhase");
        private static readonly int FILL_COLOR = Shader.PropertyToID("_FillColor");

        [Header("이펙트 모드")]
        [Tooltip("Blink: 깜빡임, WhiteFlash: 흰색 플래시, RedFlash: 빨간 플래시")]
        [SerializeField] private HitEffectMode _mode = HitEffectMode.Blink;

        [Header("깜빡임 설정 (Blink)")]
        [Tooltip("깜빡임 간격 (초)")]
        [Min(0.01f)]
        [SerializeField] private float _blinkInterval = 0.1f;

        [Header("플래시 설정 (WhiteFlash / RedFlash)")]
        [Tooltip("플래시 지속 시간 (초)")]
        [Min(0.01f)]
        [SerializeField] private float _flashDuration = 0.08f;

        [Tooltip("플래시 반복 횟수")]
        [Min(1)]
        [SerializeField] private int _flashCount = 2;

        [Tooltip("플래시 사이 간격 (초)")]
        [Min(0.01f)]
        [SerializeField] private float _flashInterval = 0.06f;

        private HealthHandler _healthHandler;
        private ContactDamage _contactDamage;
        private Renderer _renderer;
        private Coroutine _effectCoroutine;
        private bool _useSpineFill;

        // Spine용 MaterialPropertyBlock (머티리얼 교체에 영향받지 않음)
        private MaterialPropertyBlock _mpb;
        // SpriteRenderer용 머티리얼 인스턴스
        private Material _materialInstance;

        private void Awake()
        {
            _healthHandler = GetComponent<HealthHandler>();
            _contactDamage = GetComponent<ContactDamage>();

            // SpriteRenderer 우선, 없으면 MeshRenderer(Spine 등) 사용
            _renderer = GetComponentInChildren<SpriteRenderer>();
            if (_renderer == null)
            {
                _renderer = GetComponentInChildren<MeshRenderer>();
            }
        }

        private void Start()
        {
            if (_renderer == null) return;

            // Spine(MeshRenderer) → MaterialPropertyBlock 사용
            // SpriteRenderer → 머티리얼 인스턴스 사용
            if (_renderer is MeshRenderer)
            {
                Material sharedMat = _renderer.sharedMaterial;
                if (sharedMat != null && sharedMat.HasProperty(FILL_PHASE))
                {
                    _useSpineFill = true;
                    _mpb = new MaterialPropertyBlock();
                }
            }
            else
            {
                _materialInstance = _renderer.material;
                _useSpineFill = false;
            }
        }

        private void OnEnable()
        {
            if (_healthHandler == null)
            {
                _healthHandler = GetComponent<HealthHandler>();
            }

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
            if (_renderer == null) return;

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
                    _effectCoroutine = StartCoroutine(FlashCoroutine(Color.white));
                    break;
                case HitEffectMode.RedFlash:
                    _effectCoroutine = StartCoroutine(FlashCoroutine(Color.red));
                    break;
            }
        }

        /// <summary>
        /// 무적 시간 동안 SpriteRenderer를 on/off 반복하여 깜빡임 연출.
        /// ContactDamage가 없으면 기본 0.5초간 깜빡임.
        /// </summary>
        private IEnumerator BlinkCoroutine()
        {
            float duration = _contactDamage != null
                ? _contactDamage.InvincibilityDuration
                : 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                _renderer.enabled = false;
                yield return new WaitForSeconds(_blinkInterval);
                elapsed += _blinkInterval;

                _renderer.enabled = true;
                yield return new WaitForSeconds(_blinkInterval);
                elapsed += _blinkInterval;
            }

            _renderer.enabled = true;
            _effectCoroutine = null;
        }

        /// <summary>
        /// 지정 색상으로 플래시 연출.
        /// Spine → MaterialPropertyBlock(_FillColor + _FillPhase),
        /// SpriteFlash → _FlashAmount (흰색 고정).
        /// </summary>
        private IEnumerator FlashCoroutine(Color flashColor)
        {
            if (_useSpineFill)
            {
                SetFillColor(flashColor);
            }

            for (int i = 0; i < _flashCount; i++)
            {
                SetFlash(true);

                yield return new WaitForSeconds(_flashDuration);

                SetFlash(false);

                if (i < _flashCount - 1)
                {
                    yield return new WaitForSeconds(_flashInterval);
                }
            }

            _effectCoroutine = null;
        }

        private void SetFlash(bool on)
        {
            if (_useSpineFill && _mpb != null)
            {
                _renderer.GetPropertyBlock(_mpb);
                _mpb.SetFloat(FILL_PHASE, on ? 1f : 0f);
                _renderer.SetPropertyBlock(_mpb);
            }
            else if (_materialInstance != null)
            {
                _materialInstance.SetFloat(FLASH_AMOUNT, on ? 1f : 0f);
            }
        }

        private void SetFillColor(Color color)
        {
            if (_mpb == null || _renderer == null) return;

            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(FILL_COLOR, color);
            _renderer.SetPropertyBlock(_mpb);
        }

        private void RestoreVisuals()
        {
            if (_renderer != null)
            {
                _renderer.enabled = true;
            }

            SetFlash(false);
        }

        private void OnDestroy()
        {
            if (_effectCoroutine != null)
            {
                StopCoroutine(_effectCoroutine);
                RestoreVisuals();
            }

            if (_materialInstance != null)
            {
                Destroy(_materialInstance);
            }
        }
    }
}
