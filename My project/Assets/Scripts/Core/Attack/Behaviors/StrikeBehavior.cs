using System.Collections.Generic;
using UnityEngine;

namespace HitWaves.Core.Attack.Behaviors
{
    /// <summary>
    /// 기본 근접 공격 — 전방 부채꼴 범위 내 적 탐지 + 데미지 + 넉백.
    /// 캐릭터 모션 없이 이펙트로 처리.
    /// </summary>
    public class StrikeBehavior : IAttackBehavior
    {
        private const string LOG_TAG = "StrikeBehavior";
        private const int FAN_SEGMENTS = 12;

        public bool CooldownOnExecute => true;

        public void Initialize(AttackHandler handler)
        {
            DebugLogger.Log(LOG_TAG, "StrikeBehavior 초기화", handler);
        }

        public int Execute(AttackHandler handler, Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.01f) return 0;

            direction = direction.normalized;
            Vector2 origin = (Vector2)handler.transform.position;
            float range = handler.Range;
            float halfAngle = handler.SwingAngle * 0.5f;
            LayerMask targetLayer = handler.TargetLayer;

            // 원형 범위 내 후보 탐색
            Collider2D[] hits = Physics2D.OverlapCircleAll(
                origin, range, targetLayer);

            if (hits.Length == 0)
            {
                ShowStrikeEffect(direction, range, halfAngle, handler);
                return 0;
            }

            StatHandler attackerStats = handler.GetComponent<StatHandler>();
            if (attackerStats == null) return 0;

            float baseDamage = attackerStats.GetStat(StatType.Damage);
            float damageCoeff = handler.DamageCoefficient;
            float finalDamage = baseDamage * damageCoeff;
            float knockbackForce = attackerStats.GetStat(StatType.KnockbackForce);

            // 부채꼴 각도 내 + 거리순 정렬
            List<Collider2D> inArc = new List<Collider2D>();
            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D col = hits[i];
                if (col.gameObject == handler.gameObject) continue;

                Vector2 toTarget = (Vector2)col.transform.position - origin;
                float angle = Vector2.Angle(direction, toTarget);
                if (angle <= halfAngle)
                {
                    inArc.Add(col);
                }
            }

            if (inArc.Count == 0)
            {
                ShowStrikeEffect(direction, range, halfAngle, handler);
                return 0;
            }

            inArc.Sort((a, b) =>
            {
                float distA = ((Vector2)a.transform.position - origin).sqrMagnitude;
                float distB = ((Vector2)b.transform.position - origin).sqrMagnitude;
                return distA.CompareTo(distB);
            });

            int hitCount = 0;
            int maxHits = handler.MaxHitCount;

            for (int i = 0; i < inArc.Count && hitCount < maxHits; i++)
            {
                Collider2D col = inArc[i];

                // 진영 체크
                StatHandler targetStats = col.GetComponent<StatHandler>();
                if (targetStats == null) continue;
                if (targetStats.Faction == attackerStats.Faction) continue;

                // 데미지 적용
                HealthHandler targetHealth = col.GetComponent<HealthHandler>();
                if (targetHealth == null || targetHealth.IsDead) continue;

                bool damageApplied = targetHealth.TakeDamage(finalDamage);

                if (!damageApplied) continue;

                hitCount++;

                DebugLogger.Log(LOG_TAG,
                    $"히트 — {col.gameObject.name}, " +
                    $"데미지: {finalDamage:F1} (base:{baseDamage} × coeff:{damageCoeff})", handler);

                // 넉백 적용 (데미지가 통했을 때만)
                if (knockbackForce > 0f)
                {
                    Rigidbody2D targetRb = col.GetComponent<Rigidbody2D>();
                    if (targetRb != null)
                    {
                        float resistance = targetStats.GetStat(StatType.KnockbackResistance);
                        float actualForce = knockbackForce / (1f + resistance);
                        Vector2 knockDir = ((Vector2)col.transform.position - origin).normalized;
                        targetRb.AddForce(knockDir * actualForce, ForceMode2D.Impulse);

                        DebugLogger.Log(LOG_TAG,
                            $"넉백 — {col.gameObject.name}, " +
                            $"force: {actualForce:F1} (base:{knockbackForce} / resist:{resistance})", handler);
                    }
                }
            }

            // 이펙트 표시
            ShowStrikeEffect(direction, range, halfAngle, handler);

            DebugLogger.Log(LOG_TAG,
                $"Strike 실행 — 방향: {direction}, 히트: {hitCount}/{maxHits}", handler);

            return hitCount;
        }

        public void Cleanup() { }

        /// <summary>
        /// 타격 범위 시각화 이펙트 (부채꼴 메시, 잠시 후 소멸).
        /// </summary>
        private void ShowStrikeEffect(Vector2 direction,
            float range, float halfAngle, AttackHandler handler)
        {
            GameObject effectGo = new GameObject("StrikeEffect");
            effectGo.transform.SetParent(handler.transform, false);

            MeshFilter meshFilter = effectGo.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = effectGo.AddComponent<MeshRenderer>();

            meshRenderer.material = new Material(Shader.Find("Sprites/Default"));
            meshRenderer.material.color = new Color(1f, 1f, 1f, 0.3f);
            meshRenderer.sortingOrder = 100;

            meshFilter.mesh = CreateFanMesh(range, halfAngle, direction);

            Object.Destroy(effectGo, handler.EffectDuration);
        }

        private Mesh CreateFanMesh(float range, float halfAngle, Vector2 direction)
        {
            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float startAngle = baseAngle - halfAngle;
            float angleStep = (halfAngle * 2f) / FAN_SEGMENTS;

            // 꼭짓점: 중심(0) + 호 위의 점들(1~segments+1)
            Vector3[] vertices = new Vector3[FAN_SEGMENTS + 2];
            vertices[0] = Vector3.zero; // 중심 (로컬 원점)

            for (int i = 0; i <= FAN_SEGMENTS; i++)
            {
                float a = (startAngle + angleStep * i) * Mathf.Deg2Rad;
                vertices[i + 1] = new Vector3(
                    Mathf.Cos(a) * range,
                    Mathf.Sin(a) * range,
                    0f);
            }

            // 삼각형
            int[] triangles = new int[FAN_SEGMENTS * 3];
            for (int i = 0; i < FAN_SEGMENTS; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            return mesh;
        }
    }
}
