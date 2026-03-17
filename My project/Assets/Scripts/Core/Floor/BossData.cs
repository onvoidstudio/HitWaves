using UnityEngine;

namespace HitWaves.Core.Floor
{
    [CreateAssetMenu(fileName = "NewBossData", menuName = "HitWaves/Boss Data")]
    public class BossData : ScriptableObject
    {
        [Header("보스 정보")]
        [Tooltip("보스 프리팹 (EntityBrain + KingDullFlyBrain 등)")]
        [SerializeField] private GameObject _prefab;

        [Tooltip("보스 표시 이름 (예: King Dull Fly)")]
        [SerializeField] private string _displayName;

        [Tooltip("보스 칭호 (예: 둔한 파리의 왕)")]
        [SerializeField] private string _title;

        [Tooltip("보스 인트로 연출용 이미지")]
        [SerializeField] private Sprite _introSprite;

        public GameObject Prefab => _prefab;
        public string DisplayName => _displayName;
        public string Title => _title;
        public Sprite IntroSprite => _introSprite;
    }
}
