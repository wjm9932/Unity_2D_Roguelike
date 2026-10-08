using CutomAttributes.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CutomAttributes.Editor
{
    [CustomPropertyDrawer(typeof(LayerAttribute))]
    public sealed class LayerPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var field = new LayerField(property.displayName)
            {
                bindingPath = property.propertyPath,
                tooltip = property.tooltip
            };
            field.AddToClassList(BaseField<int>.alignedFieldUssClassName);
            return field;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var previousMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;

            EditorGUI.BeginChangeCheck();
            var layer = EditorGUI.LayerField(position, label, property.intValue);
            if (EditorGUI.EndChangeCheck())
            {
                property.intValue = layer;
            }

            EditorGUI.showMixedValue = previousMixedValue;
            EditorGUI.EndProperty();
        }
    }
}
