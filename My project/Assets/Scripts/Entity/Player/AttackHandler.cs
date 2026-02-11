using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using HitWaves.Core;

namespace HitWaves.Entity.Player
{
    [RequireComponent(typeof(StatHandler))]
    public class AttackHandler : MonoBehaviour
    {
        private const string LOG_TAG = "AttackHandler";
        private const string INTERACTABLE_TAG = "Interactable";
        private const int SWING_MESH_SEGMENTS = 20;
        private const int HIT_BUFFER_SIZE = 32;

        [Header("Input")]
        [SerializeField] private InputActionReference _attackAction;

        [Header("Attack Settings")]
        [SerializeField] private float _range = 1f;
        [SerializeField] private float _swingAngle = 90f;
        [SerializeField] private LayerMask _targetLayer;

        [Header("Swing Visual")]
        [SerializeField] private float _swingDuration = 0.15f;
        [SerializeField] private Color _swingColor = new Color(1f, 0f, 0f, 0.3f);

        private StatHandler _statHandler;
        private float _cooldownTimer;
        private Vector2 _attackInput;

        private Collider2D[] _hitBuffer;
        private float[] _hitDistances;

        private GameObject _swingVisual;
        private MeshFilter _swingMeshFilter;
        private MeshRenderer _swingMeshRenderer;
        private Coroutine _swingCoroutine;
        private WaitForSeconds _swingWait;

        private void Awake()
        {
            _statHandler = GetComponent<StatHandler>();

            if (_statHandler == null)
            {
                DebugLogger.LogError(LOG_TAG, "StatHandler 컴포넌트를 찾을 수 없음", this);
                return;
            }

            _hitBuffer = new Collider2D[HIT_BUFFER_SIZE];
            _hitDistances = new float[HIT_BUFFER_SIZE];
            _swingWait = new WaitForSeconds(_swingDuration);
            InitializeSwingVisual();
            DebugLogger.Log(LOG_TAG, "초기화 완료", this);
        }

        private void OnEnable()
        {
            if (_attackAction != null && _attackAction.action != null)
            {
                _attackAction.action.Enable();
                DebugLogger.Log(LOG_TAG, "Attack 액션 활성화", this);
            }
            else
            {
                DebugLogger.LogWarning(LOG_TAG, "Attack 액션이 할당되지 않음", this);
            }
        }

        private void OnDisable()
        {
            if (_attackAction != null && _attackAction.action != null)
            {
                _attackAction.action.Disable();
                DebugLogger.Log(LOG_TAG, "Attack 액션 비활성화", this);
            }
        }

        private void Update()
        {
            UpdateCooldown();
            ReadInput();
        }

        private void OnDestroy()
        {
            if (_swingMeshRenderer != null && _swingMeshRenderer.material != null)
            {
                Destroy(_swingMeshRenderer.material);
            }

            if (_swingMeshFilter != null && _swingMeshFilter.mesh != null)
            {
                Destroy(_swingMeshFilter.mesh);
            }
        }

        private void UpdateCooldown()
        {
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.deltaTime;
            }
        }

        private void ReadInput()
        {
            if (_attackAction == null || _attackAction.action == null) return;

            _attackInput = _attackAction.action.ReadValue<Vector2>();

            if (_attackInput.sqrMagnitude > 0.01f)
            {
                TryAttack(_attackInput.normalized);
            }
        }

        private bool CanAttack()
        {
            return _cooldownTimer <= 0f;
        }

        private void TryAttack(Vector2 direction)
        {
            if (!CanAttack()) return;

            ExecuteAttack(direction);

            float attackSpeed = _statHandler.GetStat(StatType.AttackSpeed);
            _cooldownTimer = attackSpeed > 0f ? 1f / attackSpeed : 1f;
        }

        private void ExecuteAttack(Vector2 direction)
        {
            float damage = _statHandler.GetStat(StatType.Damage);
            float strength = _statHandler.GetStat(StatType.Strength);
            int maxHitCount = Mathf.Max(1, (int)_statHandler.GetStat(StatType.MaxHitCount));
            float halfAngle = _swingAngle * 0.5f;

            Collider2D[] candidates = Physics2D.OverlapCircleAll(transform.position, _range, _targetLayer);

            int validCount = 0;

            foreach (Collider2D candidate in candidates)
            {
                if (candidate.gameObject == gameObject) continue;
                if (!candidate.CompareTag(INTERACTABLE_TAG)) continue;

                Vector2 toTarget = (Vector2)candidate.transform.position - (Vector2)transform.position;
                float angle = Vector2.Angle(direction, toTarget.normalized);

                if (angle <= halfAngle)
                {
                    float distSqr = toTarget.sqrMagnitude;

                    if (validCount < _hitBuffer.Length)
                    {
                        _hitBuffer[validCount] = candidate;
                        _hitDistances[validCount] = distSqr;
                        validCount++;
                    }
                }
            }

            SortByDistance(validCount);

            int hitCount = 0;
            int hitLimit = Mathf.Min(validCount, maxHitCount);

            for (int i = 0; i < hitLimit; i++)
            {
                IDamageable damageable = _hitBuffer[i].GetComponent<IDamageable>();
                if (damageable != null && damageable.IsAlive)
                {
                    damageable.TakeDamage(damage, gameObject);
                    hitCount++;
                    DebugLogger.Log(LOG_TAG, $"히트: {_hitBuffer[i].gameObject.name}, 데미지: {damage}", this);
                }
            }

            DebugLogger.Log(LOG_TAG, $"공격 실행 - 방향: {direction}, 데미지: {damage}, 힘: {strength}, 히트 수: {hitCount}/{maxHitCount}", this);

            ShowSwingVisual(direction);
        }

        private void SortByDistance(int count)
        {
            for (int i = 0; i < count - 1; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    if (_hitDistances[j] < _hitDistances[i])
                    {
                        (_hitBuffer[i], _hitBuffer[j]) = (_hitBuffer[j], _hitBuffer[i]);
                        (_hitDistances[i], _hitDistances[j]) = (_hitDistances[j], _hitDistances[i]);
                    }
                }
            }
        }

        #region Swing Visual

        private void InitializeSwingVisual()
        {
            _swingVisual = new GameObject("SwingVisual");
            _swingVisual.transform.SetParent(transform);
            _swingVisual.transform.localPosition = Vector3.zero;

            _swingMeshFilter = _swingVisual.AddComponent<MeshFilter>();
            _swingMeshRenderer = _swingVisual.AddComponent<MeshRenderer>();

            _swingMeshFilter.mesh = CreateSwingMesh(_swingAngle, _range, SWING_MESH_SEGMENTS);

            Shader spriteShader = Shader.Find("Sprites/Default");
            if (spriteShader == null)
            {
                DebugLogger.LogError(LOG_TAG, "Sprites/Default 셰이더를 찾을 수 없음", this);
                return;
            }

            Material material = new Material(spriteShader);
            material.color = _swingColor;
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
                StopCoroutine(_swingCoroutine);
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _swingVisual.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            _swingVisual.SetActive(true);

            _swingCoroutine = StartCoroutine(HideSwingVisualAfterDelay());
        }

        private IEnumerator HideSwingVisualAfterDelay()
        {
            yield return _swingWait;
            _swingVisual.SetActive(false);
            _swingCoroutine = null;
        }

        #endregion

        private void OnDrawGizmosSelected()
        {
            Vector2 direction = _attackInput.sqrMagnitude > 0.01f ? _attackInput.normalized : Vector2.right;
            float halfAngle = _swingAngle * 0.5f;
            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float angleStep = _swingAngle / SWING_MESH_SEGMENTS;

            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);

            Vector3 prevPoint = transform.position;

            for (int i = 0; i <= SWING_MESH_SEGMENTS; i++)
            {
                float currentAngle = baseAngle - halfAngle + angleStep * i;
                float rad = currentAngle * Mathf.Deg2Rad;
                Vector3 point = transform.position + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * _range;

                if (i == 0)
                {
                    Gizmos.DrawLine(transform.position, point);
                }
                else
                {
                    Gizmos.DrawLine(prevPoint, point);
                }

                if (i == SWING_MESH_SEGMENTS)
                {
                    Gizmos.DrawLine(transform.position, point);
                }

                prevPoint = point;
            }
        }
    }
}
