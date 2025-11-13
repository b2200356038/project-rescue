using Physics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Editor
{
    [CustomEditor(typeof(PhysicsObjectMotion))]
    class PhysicsObjectMotionEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            serializedObject.Update();
            SerializedProperty property = serializedObject.GetIterator();
            property.NextVisible(true); 
            while (property.NextVisible(false))
            {
                var propertyField = new PropertyField(property);
                root.Add(propertyField);
            }

            serializedObject.ApplyModifiedProperties();

            return root;
        }
    }
}
