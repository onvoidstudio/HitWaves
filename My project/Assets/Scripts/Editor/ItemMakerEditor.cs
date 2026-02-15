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
            _statModifiers = serializedObject.FindProperty("_statModifiers");
            _instantEffects = serializedObject.FindProperty("_instantEffects");
            _slotType = serializedObject.FindProperty("_slotType");
            _weight = serializedObject.FindProperty("_weight");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("공통", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_itemName);
            EditorGUILayout.PropertyField(_description);
            EditorGUILayout.PropertyField(_itemType);
            EditorGUILayout.PropertyField(_icon);

            EditorGUILayout.Space();

            ItemType type = (ItemType)_itemType.enumValueIndex;

            switch (type)
            {
                case ItemType.Absorb:
                    EditorGUILayout.LabelField("흡수형 설정", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(_statModifiers, new GUIContent("스탯 변경 (영구)"));
                    EditorGUILayout.PropertyField(_instantEffects, new GUIContent("즉시 효과"));
                    break;

                case ItemType.Equipment:
                    EditorGUILayout.LabelField("장착형 설정", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(_slotType, new GUIContent("장착 부위"));
                    EditorGUILayout.PropertyField(_weight, new GUIContent("무게"));
                    EditorGUILayout.PropertyField(_statModifiers, new GUIContent("스탯 변경 (장착 중)"));
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
