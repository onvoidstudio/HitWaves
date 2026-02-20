using System.Collections;
using UnityEngine;

namespace HitWaves.Core.Attack.Behaviors
{
    public class SweepBehavior : IAttackBehavior
    {
        private const string LOG_TAG = "SweepBehavior";
        private const float DEFAULT_WEAPON_WIDTH = 0.3f;

        public bool CooldownOnExecute => true;

        private Transform _weaponPivot;
        private SweepHitbox _sweepHitbox;
        private Coroutine _sweepCoroutine;
        private AttackHandler _handler;

        public void Initialize(AttackHandler handler)
        {
            _handler = handler;

            GameObject pivotObj = new GameObject("WeaponPivot");
            pivotObj.transform.SetParent(handler.transform);
            pivotObj.transform.localPosition = Vector3.zero;
            _weaponPivot = pivotObj.transform;

            GameObject hitboxObj = new GameObject("SweepHitbox");
            hitboxObj.transform.SetParent(_weaponPivot);
            hitboxObj.transform.localPosition = Vector3.zero;
            hitboxObj.layer = handler.gameObject.layer;

            hitboxObj.AddComponent<CapsuleCollider2D>();
            _sweepHitbox = hitboxObj.AddComponent<SweepHitbox>();
            _sweepHitbox.SetColliderSize(DEFAULT_WEAPON_WIDTH, handler.Range);

            Rigidbody2D hitboxRb = hitboxObj.AddComponent<Rigidbody2D>();
            hitboxRb.bodyType = RigidbodyType2D.Kinematic;

            DebugLogger.Log(LOG_TAG, $"초기화 완료 - Range: {handler.Range}, SwingAngle: {handler.SwingAngle}", handler);
        }

        public int Execute(AttackHandler handler, Vector2 direction)
        {
            if (_sweepHitbox == null) return 0;

            float damage = handler.StatHandler.GetStat(StatType.Damage);
            int maxHitCount = Mathf.Max(1, (int)handler.StatHandler.GetStat(StatType.MaxHitCount));
            float sweepDuration = handler.StatHandler.GetStat(StatType.SweepDuration);

            if (sweepDuration <= 0f) sweepDuration = 0.2f;

            float knockbackForce = handler.StatHandler.GetStat(StatType.KnockbackForce);
            _sweepHitbox.Configure(damage, knockbackForce, handler.gameObject, handler.TargetLayer, maxHitCount);

            if (_sweepCoroutine != null)
            {
                handler.StopCoroutine(_sweepCoroutine);
                _sweepHitbox.Deactivate();
            }

            _sweepCoroutine = handler.StartCoroutine(SweepRoutine(direction, sweepDuration, handler.SwingAngle));

            return 0;
        }

        public void Cleanup()
        {
            if (_sweepCoroutine != null && _handler != null)
            {
                _handler.StopCoroutine(_sweepCoroutine);
            }

            if (_weaponPivot != null)
            {
                Object.Destroy(_weaponPivot.gameObject);
            }
        }

        private IEnumerator SweepRoutine(Vector2 direction, float duration, float swingAngle)
        {
            float dirAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float startAngle = dirAngle + swingAngle * 0.5f;
            float endAngle = dirAngle - swingAngle * 0.5f;

            _weaponPivot.rotation = Quaternion.Euler(0f, 0f, startAngle - 90f);
            _sweepHitbox.Activate();

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float currentAngle = Mathf.Lerp(startAngle, endAngle, t);
                _weaponPivot.rotation = Quaternion.Euler(0f, 0f, currentAngle - 90f);
                yield return null;
            }

            _weaponPivot.rotation = Quaternion.Euler(0f, 0f, endAngle - 90f);
            _sweepHitbox.Deactivate();
            _sweepCoroutine = null;
        }
    }
}
