using UnityEditor;
using UnityEngine;

namespace VSplatter.StateMachine.Editor
{
    [CustomPropertyDrawer(typeof(InitOnlyAttribute))]
    public class InitOnlyAttributeDrawer : PropertyDrawer
    {
        private static readonly string _text = "Changes to this parameter during Play mode won't be reflected on existing StateMachines";

        private static GUIStyle _style;
        private static GUIContent _content;

        private static GUIStyle Style
        {
            get
            {
                if (_style == null)
                {
                    _style = new GUIStyle(EditorStyles.helpBox)
                    {
                        padding = new RectOffset(5, 5, 5, 5)
                    };
                }

                return _style;
            }
        }

        private static GUIContent Content
        {
            get
            {
                if (_content == null)
                    _content = new GUIContent(_text);

                return _content;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (EditorApplication.isPlaying)
            {
                position.height = Style.CalcHeight(Content, EditorGUIUtility.currentViewWidth);
                EditorGUI.HelpBox(position, _text, MessageType.Info);
                position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
                position.height = EditorGUI.GetPropertyHeight(property, label);
            }

            EditorGUI.PropertyField(position, property, label);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUI.GetPropertyHeight(property, label);

            if (EditorApplication.isPlaying)
            {
                height += Style.CalcHeight(Content, EditorGUIUtility.currentViewWidth)
                    + EditorGUIUtility.standardVerticalSpacing * 4;
            }

            return height;
        }
    }
}
