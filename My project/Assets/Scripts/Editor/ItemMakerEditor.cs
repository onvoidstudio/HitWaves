using UnityEditor;
using UnityEngine;
using HitWaves.Core.Item;

namespace HitWaves.Editor
{
    [CustomEditor(typeof(ItemMaker))]
    public class ItemMakerEditor : UnityEditor.Editor
    {
        private SerializedProperty _itemName;
        private SerializedProperty _description;
        private SerializedProperty _itemType;
        private SerializedProperty _icon;
        private SerializedProperty _positionType;
        private SerializedProperty _physicsType;
        private SerializedProperty _isDamageable;
        private SerializedProperty _durability;
        private SerializedProperty _toughness;
        private SerializedProperty _statModifiers;
        private SerializedProperty _instantEffects;
        private SerializedProperty _slotType;
        private SerializedProperty _weight;

        private void OnEnable()
        {
            _itemName = serializedObject.FindProperty("_itemName");
            _description = serializedObject.FindProperty("_description");
            _itemType = serializedObject.FindProperty("_itemType");
            _icon = serializedObject.FindProperty("_icon");
            _positionType = serializedObject.FindProperty("_positionType");
            _physicsType = serializedObject.FindProperty("_physicsType");
            _isDamageable = serializedObject.FindProperty("_isDamageable");
            _durability = serializedObject.FindProperty("_durability");
            _toughness = serializedObject.FindProperty("_toughness");
            _statModifiers = serializedObject.FindProperty("_statModifiers");
            _instantEffects = serializedObject.FindProperty("_instantEffects");
            _slotType = serializedObject.FindProperty("_slotType");
            _weight = serializedObject.FindProperty("_weight");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("공통", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_itemName, new GUIContent("아이템 이름", "월드/UI에 표시되는 아이템 이름"));
            EditorGUILayout.PropertyField(_description, new GUIContent("설명", "아이템 상세 설명"));
            EditorGUILayout.PropertyField(_itemType, new GUIContent("아이템 타입", "Absorb=흡수, Equipment=장착, Consumable=소모, Carry=소지"));
            EditorGUILayout.PropertyField(_icon, new GUIContent("아이콘", "월드/UI에 표시되는 스프라이트"));
            EditorGUILayout.PropertyField(_positionType, new GUIContent("위치 분류", "아이템을 흡수/장착할 수 있는 위치. Ground=지상, Air=공중, Hybrid=양쪽, Special=특수"));
            EditorGUILayout.PropertyField(_physicsType, new GUIContent("물리 상호작용", "공격 시 물리가 적용되는 조건. Ground=지상 입력, Air=공중 입력, Both=양쪽, Fixed=물리 없음"));
            EditorGUILayout.PropertyField(_isDamageable, new GUIContent("피격 가능", "공격 시 내구도가 감소하는지 여부"));
            EditorGUILayout.PropertyField(_durability, new GUIContent("내구도", "아이템의 체력. 0이 되면 파괴"));
            EditorGUILayout.PropertyField(_toughness, new GUIContent("강도", "받는 데미지 감소량. 높을수록 내구도가 덜 깎임"));

            EditorGUILayout.Space();

            ItemType type = (ItemType)_itemType.enumValueIndex;

            switch (type)
            {
                case ItemType.Absorb:
                    EditorGUILayout.LabelField("흡수형 설정", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(_statModifiers, new GUIContent("스탯 변경 (영구)", "흡수 시 영구적으로 적용되는 스탯 변경"));
                    EditorGUILayout.PropertyField(_instantEffects, new GUIContent("즉시 효과", "흡수 시 즉시 발동하는 효과 (회복 등)"));
                    break;

                case ItemType.Equipment:
                    EditorGUILayout.LabelField("장착형 설정", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(_slotType, new GUIContent("장착 부위", "장비가 장착되는 슬롯 부위"));
                    EditorGUILayout.PropertyField(_weight, new GUIContent("무게", "장비 무게. Strength 기반 최대 무게 초과 시 장착 불가"));
                    EditorGUILayout.PropertyField(_statModifiers, new GUIContent("스탯 변경 (장착 중)", "장착 중에만 적용되는 스탯 변경. 해제 시 제거"));
                    break;

                case ItemType.Consumable:
                    EditorGUILayout.LabelField("소모형 설정", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("소모형 시스템은 아직 미구현입니다.", MessageType.Info);
                    break;

                case ItemType.Carry:
                    EditorGUILayout.LabelField("소지형 설정", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("소지형 시스템은 아직 미구현입니다.", MessageType.Info);
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
