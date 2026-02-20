using System.Collections.Generic;
using UnityEngine;
using HitWaves.Core.AI;

namespace HitWaves.Core.Attack
{
    [RequireComponent(typeof(CapsuleCollider2D))]
    public class SweepHitbox : MonoBehaviour
    {
        private const string LOG_TAG = "SweepHitbox";
        private const float KNOCKBACK_DURATION = 0.2f;

        private CapsuleCollider2D _collider;
        private float _damage;
        private float _knockbackForce;
        private GameObject _attacker;
        private LayerMask _targetLayer;
        private int _maxHitCount;
        private int _hitCount;
        private HashSet<Collider2D> _alreadyHit;

        private void Awake()
        {
            _collider = GetComponent<CapsuleCollider2D>();
            _collider.isTrigger = true;
            _collider.enabled = false;
            _alreadyHit = new HashSet<Collider2D>();
        }

        public void Configure(float damage, float knockbackForce, GameObject attacker, LayerMask targetLayer, int maxHitCount)
        {
            _damage = damage;
            _knockbackForce = knockbackForce;
            _attacker = attacker;
            _targetLayer = targetLayer;
            _maxHitCount = maxHitCount;
        }

        public void Activate()
        {
            _hitCount = 0;
            _alreadyHit.Clear();
            _collider.enabled = true;
        }

        public void Deactivate()
        {
            _collider.enabled = false;
        }

        public void SetColliderSize(float width, float height)
        {
            _collider.size = new Vector2(width, height);
            _collider.offset = new Vector2(0f, height * 0.5f);
            _collider.direction = CapsuleDirection2D.Vertical;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject == _attacker) return;
            if (_alreadyHit.Contains(other)) return;

            _alreadyHit.Add(other);

            Rigidbody2D rb = other.attachedRigidbody;
            if (rb != null && _knockbackForce > 0f)
            {
                float resistance = 0f;
                StatHandler targetStat = other.GetComponent<StatHandler>();
                if (targetStat != null)
                {
                    resistance = Mathf.Max(0f, targetStat.GetStat(StatType.KnockbackResistance));
                }

                float actualForce = _knockbackForce / (1f + resistance);
                Vector2 forceDir = ((Vector2)other.transform.position - (Vector2)_attacker.transform.position).normalized;
                rb.AddForce(forceDir * actualForce, ForceMode2D.Impulse);

                EntityAI entityAI = other.GetComponent<EntityAI>();
                if (entityAI != null)
                {
                    entityAI.ApplyKnockback(KNOCKBACK_DURATION / (1f + resistance));
                }
            }

            if (_hitCount >= _maxHitCount) return;
            if (((1 << other.gameObject.layer) & _targetLayer) == 0) return;
            if (!other.CompareTag("Interactable")) return;

            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable == null || !damageable.IsAlive) return;

            damageable.TakeDamage(_damage, _attacker);
            _hitCount++;

            DebugLogger.Log(LOG_TAG, $"스윕 타격: {other.gameObject.name} (데미지: {_damage}, 히트: {_hitCount}/{_maxHitCount})", this);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_collider == null || !_collider.enabled) return;

            Gizmos.color = new Color(1f, 0.3f, 0f, 0.5f);
            Vector2 size = _collider.size;
            Vector2 offset = _collider.offset;

            Vector3 center = transform.TransformPoint(offset);
            Vector3 up = transform.up;
            Vector3 right = transform.right;

            float halfW = size.x * 0.5f;
            float halfH = size.y * 0.5f;

            Vector3 tl = center - right * halfW + up * halfH;
            Vector3 tr = center + right * halfW + up * halfH;
            Vector3 br = center + right * halfW - up * halfH;
            Vector3 bl = center - right * halfW - up * halfH;

            Gizmos.DrawLine(tl, tr);
            Gizmos.DrawLine(tr, br);
            Gizmos.DrawLine(br, bl);
            Gizmos.DrawLine(bl, tl);
            Gizmos.DrawLine(tl, br);
        }
#endif
    }
}
