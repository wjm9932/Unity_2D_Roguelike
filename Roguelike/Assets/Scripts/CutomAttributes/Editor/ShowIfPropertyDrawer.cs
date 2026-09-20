using CustomAttribute.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace CutomAttributes.Editor
{
    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    public class ShowIfPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var attr = (ShowIfAttribute)attribute;

            var root = new VisualElement();

            var field = new PropertyField(property);
            root.Add(field);

            var path = property.propertyPath;
            var lastDot = path.LastIndexOf('.');
            var parentPath = lastDot >= 0 ? path[..lastDot] : string.Empty;
            var enumPath = string.IsNullOrEmpty(parentPath) ? attr.FlagField : $"{parentPath}.{attr.FlagField}";
            var enumProp = property.serializedObject.FindProperty(enumPath);

            if (enumProp == null)
            {
                return root;
            }

            UpdateVisibility(enumProp);

            root.TrackPropertyValue(enumProp, UpdateVisibility);

            return root;

            void UpdateVisibility(SerializedProperty p)
            {
                var flag = p.intValue;
                root.style.display = flag == attr.RequiredFlag ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}