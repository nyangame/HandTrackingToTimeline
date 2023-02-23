using UnityEditor;
using UnityEngine;

namespace Vox.Hands
{
    [CustomPropertyDrawer(typeof(HandControllerPlayableBehaviour))]
    public class HandControllerPlayableDrawer : PropertyDrawer
    {
        private HandPoseDataEditorUtility m_utility;
        private SerializedProperty m_prop_presets;
        private SerializedProperty m_isInit;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int fieldCount = 36;
            return fieldCount * EditorGUIUtility.singleLineHeight + 200f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (m_utility == null)
            {
                m_utility = new HandPoseDataEditorUtility(property, "handPose.");
            }

            if (m_prop_presets == null)
            {
                m_prop_presets = property.FindPropertyRelative("presets");
            }

            if (m_isInit == null)
            {
                m_isInit = property.FindPropertyRelative("isInitPose");
            }

            var presets = m_prop_presets.objectReferenceValue as HandPosePresetsAsset;
            if (presets == null)
            {
                presets = HandPosePresetsAsset.GetDefaultGetPresetsAsset();
                m_prop_presets.objectReferenceValue = presets;
            }
            
            var singleFieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(singleFieldRect, m_prop_presets);
            position.y += EditorGUIUtility.singleLineHeight;
            position.height -= EditorGUIUtility.singleLineHeight;

            /*
            m_isInit.boolValue = EditorGUI.Toggle(position, m_isInit.boolValue);
            position.y += EditorGUIUtility.singleLineHeight;
            position.height -= EditorGUIUtility.singleLineHeight;
            */

            m_utility.DrawFingerControls(position, presets);
        }
    }
}