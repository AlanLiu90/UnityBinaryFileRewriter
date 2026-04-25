using UnityEditor;
using UnityEngine;

namespace EngineBinaryFileRewriter
{
    [CustomPropertyDrawer(typeof(CodeRewriterRule))]
    public class CodeRewriterRuleDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // BeginProperty is vital for proper array item handling and prefab overrides
            label = EditorGUI.BeginProperty(position, label, property);

            // 1. Get relative properties for this specific array element
            SerializedProperty buildTargetProp = property.FindPropertyRelative("BuildTarget");
            SerializedProperty ruleProp = property.FindPropertyRelative("Rule");

            // 2. Calculate the Rect for the BuildTarget enum
            Rect targetRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            if (ruleProp.managedReferenceValue == null)
                UpdateRuleType(buildTargetProp, ruleProp);

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(targetRect, buildTargetProp);
            if (EditorGUI.EndChangeCheck())
            {
                UpdateRuleType(buildTargetProp, ruleProp);
            }

            // 3. Draw the polymorphic Rule field if it exists
            if (ruleProp.managedReferenceValue != null)
            {
                // Shift down by one line height to draw the rule's own fields
                Rect ruleRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, position.width, position.height);

                // Draw with includeChildren = true to show all fields of the specific subclass
                EditorGUI.PropertyField(ruleRect, ruleProp, true);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty ruleProp = property.FindPropertyRelative("Rule");

            // Base height for the BuildTarget enum line
            float height = EditorGUIUtility.singleLineHeight + 2;

            // includeChildren: true is required to get the height of all nested fields (Symbols, etc.)
            height += EditorGUI.GetPropertyHeight(ruleProp, true) + 2;

            return height;
        }

        private void UpdateRuleType(SerializedProperty buildTargetProp, SerializedProperty ruleProp)
        {
            BuildTarget target = (BuildTarget)buildTargetProp.intValue;

            object newRule = target switch
            {
                BuildTarget.Android => new PlatformCodeRewriterRuleAndroid(),
                BuildTarget.iOS => new PlatformCodeRewriterRuleIOS(),
                BuildTarget.OpenHarmony => new PlatformCodeRewriterRuleOpenHarmony(),
                BuildTarget.WebGL => new PlatformCodeRewriterRuleWebGL(),
                _ => null
            };

            ruleProp.managedReferenceValue = newRule;

            // FORCE the property to be expanded so fields are visible immediately
            ruleProp.isExpanded = true;

            // Apply changes and tell the inspector to redraw next frame
            ruleProp.serializedObject.ApplyModifiedProperties();
        }
    }
}
