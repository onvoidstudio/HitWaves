using System.Collections;
using UnityEngine;

namespace HitWaves.Core.Attack.Behaviors
{
    public class StrikeBehavior : IAttackBehavior
    {
        private const string LOG_TAG = "StrikeBehavior";
        private const string INTERACTABLE_TAG = "Interactable";
        private const int SWING_MESH_SEGMENTS = 20;
        private const float SWING_DURATION = 0.15f;

        private AttackHandler _handler;
        private GameObject _swingVisual;
        private MeshFilter _swingMeshFilter;
        private MeshRenderer _swingMeshRenderer;
        private Coroutine _swingCoroutine;
        private WaitForSeconds _swingWait;

        public bool CooldownOnExecute => true;

        public void Initialize(AttackHandler handler)
        {
            _handler = handler;
            _swingWait = new WaitForSeconds(SWING_DURATION);
            InitializeSwingVisual();
        }

        public int Execute(AttackHandler handler, Vector2 direction)
        {
            float damage = handler.StatHandler.GetStat(StatType.Damage);
            float strength = handler.StatHandler.GetStat(StatType.Strength);
            int maxHitCount = Mathf.Max(1, (int)handler.StatHandler.GetStat(StatType.MaxHitCount));
            float halfAngle = handler.SwingAngle * 0.5f;

            int candidateCount = Physics2D.OverlapCircleNonAlloc(
                handler.transform.position, handler.Range, handler.HitBuffer, handler.TargetLayer);

            int validCount = 0;

            for (int i = 0; i < candidateCount; i++)
            {
                Collider2D candidate = handler.HitBuffer[i];
                if (candidate.gameObject == handler.gameObject) continue;
                if (!candidate.CompareTag(INTERACTABLE_TAG)) continue;

                Vector2 toTarget = (Vector2)candidate.transform.position - (Vector2)handler.transform.position;
                float angle = Vector2.Angle(direction, toTarget.normalized);

                if (angle <= halfAngle)
                {
                    float distSqr = toTarget.sqrMagnitude;

                    if (validCount < handler.HitBuffer.Length)
                    {
                        handler.HitBuffer[validCount] = candidate;
                        handler.HitDistances[validCount] = distSqr;
                        validCount++;
                    }
                }
            }

            SortByDistance(handler, validCount);

            int hitCount = 0;
            int hitLimit = Mathf.Min(validCount, maxHitCount);

            for (int i = 0; i < hitLimit; i++)
            {
                IDamageable damageable = handler.HitBuffer[i].GetComponent<IDamageable>();
                if (damageable != null && damageable.IsAlive)
                {
                    damageable.TakeDamage(damage, handler.gameObject);
                    hitCount++;
                    DebugLogger.Log(LOG_TAG, $"히트: {handler.HitBuffer[i].gameObject.name}, 데미지: {damage}", handler);
                }
            }

            ShowSwingVisual(direction);
            return hitCount;
        }

        public void Cleanup()
        {
            if (_swingCoroutine != null && _handler != null)
            {
                _handler.StopCoroutine(_swingCoroutine);
                _swingCoroutine = null;
            }

            if (_swingMeshRenderer != null && _swingMeshRenderer.material != null)
            {
                Object.Destroy(_swingMeshRenderer.material);
            }

            if (_swingMeshFilter != null && _swingMeshFilter.mesh != null)
            {
                Object.Destroy(_swingMeshFilter.mesh);
            }

            if (_swingVisual != null)
            {
                Object.Destroy(_swingVisual);
            }
        }

        private void SortByDistance(AttackHandler handler, int count)
        {
            for (int i = 0; i < count - 1; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    if (handler.HitDistances[j] < handler.HitDistances[i])
                    {
                        (handler.HitBuffer[i], handler.HitBuffer[j]) = (handler.HitBuffer[j], handler.HitBuffer[i]);
                        (handler.HitDistances[i], handler.HitDistances[j]) = (handler.HitDistances[j], handler.HitDistances[i]);
                    }
                }
            }
        }

        #region Swing Visual

        private void InitializeSwingVisual()
        {
            _swingVisual = new GameObject("SwingVisual");
            _swingVisual.transform.SetParent(_handler.transform);
            _swingVisual.transform.localPosition = Vector3.zero;

            _swingMeshFilter = _swingVisual.AddComponent<MeshFilter>();
            _swingMeshRenderer = _swingVisual.AddComponent<MeshRenderer>();

            _swingMeshFilter.mesh = CreateSwingMesh(_handler.SwingAngle, _handler.Range, SWING_MESH_SEGMENTS);

            Shader spriteShader = Shader.Find("Sprites/Default");
            if (spriteShader == null)
            {
                DebugLogger.LogError(LOG_TAG, "Sprites/Default 셰이더를 찾을 수 없음", _handler);
                return;
            }

            Material material = new Material(spriteShader);
            material.color = new Color(1f, 0f, 0f, 0.3f);
            _swingMeshRenderer.material = material;
            _swingMeshRenderer.sortingOrder = 10;

            _swingVisual.SetActive(false);
        }

        private static Mesh CreateSwingMesh(float angle, float radius, int segments)
        {
            Mesh mesh = new Mesh();

            int vertexCount = segments + 2;
            Vector3[] vertices = new Vector3[vertexCount];

            vertices[0] = Vector3.zero;

            float halfAngle = angle * 0.5f;
            float angleStep = angle / segments;

            for (int i = 0; i <= segments; i++)
            {
                float currentAngle = -halfAngle + angleStep * i;
                float rad = currentAngle * Mathf.Deg2Rad;
                vertices[i + 1] = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * radius;
            }

            int[] triangles = new int[segments * 3];

            for (int i = 0; i < segments; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            return mesh;
        }

        private void ShowSwingVisual(Vector2 direction)
        {
            if (_swingCoroutine != null)
            {
                _handler.StopCoroutine(_swingCoroutine);
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _swingVisual.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            _swingVisual.SetActive(true);

            _swingCoroutine = _handler.StartCoroutine(HideSwingVisualAfterDelay());
        }

        private IEnumerator HideSwingVisualAfterDelay()
        {
            yield return _swingWait;
            _swingVisual.SetActive(false);
            _swingCoroutine = null;
        }

        #endregion
    }
}
