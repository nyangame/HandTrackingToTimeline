using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Vox.Hands
{
    [CustomPropertyDrawer(typeof(HandTrackedPlayableBehaviour))]
    public class HandTrackedPlayableDrawer : PropertyDrawer
    {
        //private HandPoseDataEditorUtility m_utility;
        //private SerializedProperty m_prop_presets;
        private SerializedProperty m_isRecord;
        private SerializedProperty m_TrackedFile;

        string[] _paths = null;
        int _currentIndex;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int fieldCount = 36;
            return fieldCount * EditorGUIUtility.singleLineHeight + 200f;
        }

        void AssetSelect(Rect position, string label, SerializedProperty property)
        {
            if (_paths == null)
            {
                var list = Directory.GetFiles(Directory.GetCurrentDirectory() + "/TrackingSave").Select(f => Path.GetFileName(f)).ToList();
                _currentIndex = list.IndexOf(property.stringValue);
                    if (_currentIndex == -1)
                    {
                        _currentIndex = 0;
                    }
                _paths = list.ToArray();
            }

            _currentIndex = EditorGUI.Popup(position, label, _currentIndex, _paths);
            property.stringValue = _paths[_currentIndex];
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            /*
            if (m_utility == null)
            {
                m_utility = new HandPoseDataEditorUtility(property, "handPose.");
            }
            */

            if (m_isRecord == null)
            {
                m_isRecord = property.FindPropertyRelative("isRecording");
            }

            if (m_TrackedFile == null)
            {
                m_TrackedFile = property.FindPropertyRelative("trackingFile");
            }

            var singleFieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            m_isRecord.boolValue = EditorGUI.Toggle(singleFieldRect, "録画モード？", m_isRecord.boolValue);

            singleFieldRect.y += EditorGUIUtility.singleLineHeight;
            AssetSelect(singleFieldRect, "録画ファイル", m_TrackedFile);

            /*
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

            m_utility.DrawFingerControls(position, presets);
            */
        }
    }
}