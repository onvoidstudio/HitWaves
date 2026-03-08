using UnityEngine;
using HitWaves.Core;

namespace HitWaves.Entity
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(StatHandler))]
    public abstract class EntityController : MonoBehaviour
    {
        private const string LOG_TAG = "EntityController";

        protected Rigidbody2D _rigidbody;
        protected StatHandler _statHandler;

        protected virtual void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _statHandler = GetComponent<StatHandler>();

            if (_rigidbody == null)
            {
                DebugLogger.LogError(LOG_TAG, "Rigidbody2D 컴포넌트를 찾을 수 없음", this);
                return;
            }

            if (_statHandler == null)
            {
                DebugLogger.LogError(LOG_TAG, "StatHandler 컴포넌트를 찾을 수 없음", this);
                return;
            }

            _rigidbody.gravityScale = 0f;
            _rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
            _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;

            DebugLogger.Log(LOG_TAG,
                $"{gameObject.name}: EntityController 초기화 완료", this);
        }
    }
}
