using UnityEngine;

namespace HitWaves.Core.Attack.Behaviors
{
    /// <summary>
    /// 투사체. 직선 이동 → 적 충돌 시 데미지 + 파괴. 사거리 초과 시 자동 파괴.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        private const string LOG_TAG = "Projectile";

        private const float SPAWN_IGNORE_DURATION = 0.05f;

        private float _damage;
        private float _knockbackForce;
        private float _maxDistance;
        private Faction _ownerFaction;
        private int _pierceCount;
        private int _currentPierceCount;
        private Vector2 _startPosition;
        private Rigidbody2D _rigidbody;
        private Collider2D _shooterCollider;
        private float _spawnTime;

        /// <summary>
        /// 투사체 초기화. ShootBehavior에서 스폰 직후 호출.
        /// </summary>
        public void Initialize(Vector2 direction, float speed, float damage,
            float knockbackForce, float maxDistance, Faction ownerFaction,
            int pierceCount = 0, Collider2D shooterCollider = null)
        {
            _damage = damage;
            _knockbackForce = knockbackForce;
            _maxDistance = maxDistance;
            _ownerFaction = ownerFaction;
            _pierceCount = pierceCount;
            _currentPierceCount = 0;
            _startPosition = transform.position;
            _spawnTime = Time.time;

            // 스폰 직후 발사자와 충돌 방지 (0.05초)
            _shooterCollider = shooterCollider;
            if (_shooterCollider != null)
            {
                Collider2D projCollider = GetComponent<Collider2D>();
                if (projCollider != null)
                {
                    Physics2D.IgnoreCollision(projCollider, _shooterCollider, true);
                }
            }

            _rigidbody = GetComponent<Rigidbody2D>();
            _rigidbody.gravityScale = 0f;
            _rigidbody.freezeRotation = true;
            _rigidbody.WakeUp();
            _rigidbody.linearVelocity = direction.normalized * speed;

            DebugLogger.Log(LOG_TAG,
                $"Initialize — dir:{direction}, spd:{speed}, velocity:{_rigidbody.linearVelocity}");

            // 방향에 맞게 회전 (스프라이트 기본 방향 = 위(↑), 0도 = 북쪽)
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void Update()
        {
            // 스폰 보호 시간 경과 → 발사자 충돌 복원
            if (_shooterCollider != null && Time.time >= _spawnTime + SPAWN_IGNORE_DURATION)
            {
                Collider2D projCollider = GetComponent<Collider2D>();
                if (projCollider != null)
                {
                    Physics2D.IgnoreCollision(projCollider, _shooterCollider, false);
                }
                _shooterCollider = null;
            }

            // 사거리 초과 시 파괴
            float traveled = ((Vector2)transform.position - _startPosition).sqrMagnitude;
            if (traveled >= _maxDistance * _maxDistance)
            {
                Destroy(gameObject);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            Collider2D other = collision.collider;

            // StatHandler 없으면 벽/환경 → 파괴
            StatHandler targetStats = other.GetComponent<StatHandler>();
            if (targetStats == null)
            {
                DebugLogger.Log(LOG_TAG, "벽 충돌 — 파괴");
                Destroy(gameObject);
                return;
            }

            // 같은 진영 무시
            if (targetStats.Faction == _ownerFaction) return;

            // 데미지 적용
            HealthHandler targetHealth = other.GetComponent<HealthHandler>();
            if (targetHealth == null || targetHealth.IsDead) return;

            bool damageApplied = targetHealth.TakeDamage(_damage);
            if (!damageApplied)
            {
                if (!CanPierce())
                {
                    Destroy(gameObject);
                }
                return;
            }

            DebugLogger.Log(LOG_TAG,
                $"히트 — {other.gameObject.name}, 데미지: {_damage:F1}");

            // 넉백
            if (_knockbackForce > 0f)
            {
                Rigidbody2D targetRb = other.GetComponent<Rigidbody2D>();
                if (targetRb != null)
                {
                    float resistance = targetStats.GetStat(StatType.KnockbackResistance);
                    float actualForce = _knockbackForce / (1f + resistance);
                    Vector2 knockDir = _rigidbody.linearVelocity.normalized;
                    targetRb.AddForce(knockDir * actualForce, ForceMode2D.Impulse);
                }
            }

            // 관통 처리
            if (!CanPierce())
            {
                Destroy(gameObject);
            }
            else
            {
                _currentPierceCount++;
            }
        }

        /// <summary>
        /// 관통 가능 여부. -1=무한, 아니면 현재 관통 횟수가 최대 미만일 때.
        /// </summary>
        private bool CanPierce()
        {
            if (_pierceCount < 0) return true; // 무한 관통
            return _currentPierceCount < _pierceCount;
        }
    }
}
